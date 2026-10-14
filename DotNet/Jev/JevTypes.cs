using System;
using System.Collections.Generic;

namespace JevWrapper
{
    public enum QuestionType { Choice, Score, Noul }

    [Serializable]
    public class JevQuestionDef
    {
        public string Name;
        public QuestionType Type;
        public string Instructions;
        public List<string> Options;          // for Choice or Score
        public Dictionary<string, string> Criteria; // optional descriptions for Choice
    }

    [Serializable]
    public class JevQuestions
    {
        public List<JevQuestionDef> Items = new List<JevQuestionDef>();

        public JevQuestions AddChoice(string name, string instructions, params string[] options)
        {
            Items.Add(new JevQuestionDef { Name = name, Type = QuestionType.Choice, Instructions = instructions, Options = new List<string>(options) });
            return this;
        }

        /// <summary>Describes what an option means; applies to the most recently added question.</summary>
        public JevQuestions Describe(string option, string meaning)
        {
            var last = Items[Items.Count - 1];
            if (last.Criteria == null) last.Criteria = new Dictionary<string, string>();
            last.Criteria[option] = meaning;
            return this;
        }

        public JevQuestions AddScore(string name, string instructions, params string[] levels)
        {
            Items.Add(new JevQuestionDef { Name = name, Type = QuestionType.Score, Instructions = instructions, Options = new List<string>(levels) });
            return this;
        }

        public JevQuestions AddNoul(string name, string instructions)
        {
            Items.Add(new JevQuestionDef { Name = name, Type = QuestionType.Noul, Instructions = instructions });
            return this;
        }
    }

    [Serializable]
    public class JevAnswerItem
    {
        public string Name;
        public QuestionType Type;
        public string Choice;                 // for Choice
        public float Score;                   // for Score (index or value)
        public float Noul;                    // for Noul (0-1)
        public float Confidence;
        public Dictionary<string, float> Probabilities;
    }

    [Serializable]
    public class JevAnswer
    {
        public string Group;
        public bool Success;                  // false on HTTP or parse failure; see Error
        public string Error;
        public List<JevAnswerItem> Items = new List<JevAnswerItem>();
        public string RawJson;                // for debugging

        public JevAnswerItem Get(string name)
        {
            return Items.Find(i => i.Name == name);
        }
    }
}
