using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JevWrapper.Tests
{
    public class JevProtocolTests
    {
        // Real response captured from the dagote endpoint.
        private const string ChoiceResponse =
            @"{""model"":""jev-latest"",""answers"":{""action"":{""type"":""choice"",""choice"":""Flee"",""probabilities"":{""Attack"":0.0013,""Flee"":0.9987,""Heal"":0},""confidence"":0.9987}},""usage"":{""input_tokens"":19,""output_tokens"":1},""ms"":43,""authenticated"":false}";

        [Test]
        public void ParseAnswer_ChoiceResponse_FillsItem()
        {
            var answer = new JevAnswer();
            JevProtocol.ParseAnswer(ChoiceResponse, answer);

            Assert.IsTrue(answer.Success);
            var item = answer.Get("action");
            Assert.AreEqual("Flee", item.Choice);
            Assert.AreEqual(QuestionType.Choice, item.Type);
            Assert.AreEqual(0.9987f, item.Confidence, 1e-4f);
            Assert.AreEqual(0.0013f, item.Probabilities["Attack"], 1e-4f);
        }

        [Test]
        public void ParseAnswer_NoAnswers_FailsWithError()
        {
            var answer = new JevAnswer();
            JevProtocol.ParseAnswer(@"{""model"":""jev-latest""}", answer);

            Assert.IsFalse(answer.Success);
            Assert.IsNotEmpty(answer.Error);
        }

        [Test]
        public void ParseAnswer_InvalidJson_FailsWithError()
        {
            LogAssert.Expect(LogType.Warning, new Regex("Jev answer parse failed"));
            var answer = new JevAnswer();
            JevProtocol.ParseAnswer("not json", answer);

            Assert.IsFalse(answer.Success);
            Assert.IsNotEmpty(answer.Error);
        }

        [Test]
        public void BuildStateText_UsesInvariantCulture()
        {
            var previous = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            try
            {
                string text = JevProtocol.BuildStateText(new Dictionary<string, float> { { "health", 0.5f } });
                Assert.AreEqual("health is 0.5.", text);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void BuildRequestJson_IsValidAndKeepsOptionOrder()
        {
            var questions = new JevQuestions()
                .AddChoice("act", "Say \"go\" or\nstay", "Shoot", "Run", "Heal")
                .Describe("Run", "Back away");

            string json = JevProtocol.BuildRequestJson("jev-3b", "Health is \"low\".", questions);
            var root = JObject.Parse(json);

            Assert.AreEqual("jev-3b", (string)root["model"]);
            Assert.AreEqual("Health is \"low\".", (string)root["state"]);
            Assert.AreEqual("Say \"go\" or\nstay", (string)root["questions"]["act"]["instructions"]);

            var criteria = (JObject)root["questions"]["act"]["criteria"];
            CollectionAssert.AreEqual(new[] { "Shoot", "Run", "Heal" }, criteria.Properties().Select(p => p.Name));
            Assert.AreEqual("Back away", (string)criteria["Run"]);
            Assert.AreEqual("Shoot", (string)criteria["Shoot"]); // no description: falls back to the option name
        }
    }
}
