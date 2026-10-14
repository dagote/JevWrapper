using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Xunit;

namespace JevWrapper.Tests
{
    public class JevProtocolTests
    {
        // Real response captured from the dagote endpoint.
        private const string ChoiceResponse =
            @"{""model"":""jev-latest"",""answers"":{""action"":{""type"":""choice"",""choice"":""Flee"",""probabilities"":{""Attack"":0.0013,""Flee"":0.9987,""Heal"":0},""confidence"":0.9987}},""usage"":{""input_tokens"":19,""output_tokens"":1},""ms"":43,""authenticated"":false}";

        [Fact]
        public void ParseAnswer_ChoiceResponse_FillsItem()
        {
            var answer = new JevAnswer();
            JevProtocol.ParseAnswer(ChoiceResponse, answer);

            Assert.True(answer.Success);
            var item = answer.Get("action");
            Assert.NotNull(item);
            Assert.Equal("Flee", item.Choice);
            Assert.Equal(QuestionType.Choice, item.Type);
            Assert.Equal(0.9987f, item.Confidence, 4);
            Assert.Equal(0.0013f, item.Probabilities["Attack"], 4);
        }

        [Fact]
        public void ParseAnswer_NoAnswers_FailsWithError()
        {
            var answer = new JevAnswer();
            JevProtocol.ParseAnswer(@"{""model"":""jev-latest""}", answer);

            Assert.False(answer.Success);
            Assert.NotEmpty(answer.Error);
        }

        [Fact]
        public void ParseAnswer_InvalidJson_FailsWithError()
        {
            var answer = new JevAnswer();
            JevProtocol.ParseAnswer("not json", answer);

            Assert.False(answer.Success);
            Assert.NotEmpty(answer.Error);
        }

        [Fact]
        public void BuildStateText_UsesInvariantCulture()
        {
            var previous = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            try
            {
                string text = JevProtocol.BuildStateText(new Dictionary<string, float> { { "health", 0.5f } });
                Assert.Equal("health is 0.5.", text);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Fact]
        public void BuildRequestJson_IsValidAndKeepsOptionOrder()
        {
            var questions = new JevQuestions()
                .AddChoice("act", "Say \"go\" or\nstay", "Shoot", "Run", "Heal")
                .Describe("Run", "Back away");

            string json = JevProtocol.BuildRequestJson("jev-3b", "Health is \"low\".", questions);
            var root = JObject.Parse(json);

            Assert.Equal("jev-3b", (string)root["model"]);
            Assert.Equal("Health is \"low\".", (string)root["state"]);
            Assert.Equal("Say \"go\" or\nstay", (string)root["questions"]["act"]["instructions"]);

            var criteria = (JObject)root["questions"]["act"]["criteria"];
            Assert.Collection(
                criteria.Properties(),
                p => Assert.Equal("Shoot", p.Name),
                p => Assert.Equal("Run", p.Name),
                p => Assert.Equal("Heal", p.Name)
            );
            Assert.Equal("Back away", (string)criteria["Run"]);
            Assert.Equal("Shoot", (string)criteria["Shoot"]); // no description: falls back to the option name
        }
    }
}
