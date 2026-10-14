using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace JevWrapper
{
    /// <summary>
    /// Request building and response parsing, kept free of networking so it can be tested.
    /// </summary>
    internal static class JevProtocol
    {
        public static string BuildStateText(Dictionary<string, float> state)
        {
            var sb = new StringBuilder();
            foreach (var kv in state)
                sb.Append(kv.Key).Append(" is ").Append(kv.Value.ToString("G", CultureInfo.InvariantCulture)).Append(". ");
            return sb.ToString().TrimEnd();
        }

        public static string BuildRequestJson(string model, string stateText, JevQuestions questions)
        {
            var questionsJson = new JObject();
            foreach (var q in questions.Items)
            {
                var question = new JObject
                {
                    ["type"] = q.Type.ToString().ToLower(),
                    ["instructions"] = q.Instructions ?? ""
                };

                if (q.Type == QuestionType.Choice || q.Type == QuestionType.Score)
                {
                    var criteria = new JObject();
                    if (q.Options != null)
                    {
                        foreach (string opt in q.Options)
                            criteria[opt] = (q.Criteria != null && q.Criteria.TryGetValue(opt, out var d)) ? d : opt;
                    }
                    question["criteria"] = criteria;
                }
                questionsJson[q.Name] = question;
            }

            var root = new JObject
            {
                ["model"] = model,
                ["state"] = stateText,
                ["questions"] = questionsJson
            };
            return root.ToString(Formatting.None);
        }

        public static void ParseAnswer(string json, JevAnswer answer)
        {
            answer.RawJson = json;
            try
            {
                var answers = JObject.Parse(json)["answers"] as JObject;
                if (answers == null)
                {
                    answer.Error = "response has no 'answers'";
                    return;
                }

                foreach (var prop in answers.Properties())
                {
                    var a = prop.Value;
                    var item = new JevAnswerItem
                    {
                        Name = prop.Name,
                        Choice = (string)a["choice"],
                        Confidence = (float?)a["confidence"] ?? 0f,
                        Probabilities = a["probabilities"]?.ToObject<Dictionary<string, float>>()
                    };
                    if (Enum.TryParse((string)a["type"], true, out QuestionType type)) item.Type = type;
                    answer.Items.Add(item);
                }
                answer.Success = true;
            }
            catch (Exception e)
            {
                answer.Error = e.Message;
                Debug.LogWarning($"Jev answer parse failed: {e.Message}");
            }
        }
    }
}
