using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JevWrapper
{
    /// <summary>
    /// One connection to Jev with its own settings and signal context.
    /// Give each system its own client so they don't share a model, key or groups.
    /// </summary>
    public class JevClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly HashSet<string> _inFlight = new HashSet<string>();

        public JevSettings Settings { get; }
        public JevContext Context { get; } = new JevContext();

        public JevClient(JevSettings settings = null)
        {
            Settings = settings ?? new JevSettings();
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(Settings.TimeoutSeconds);
        }

        public void Update(string name, string group, float value)
        {
            Context.Update(name, group, value);
        }

        /// <summary>
        /// Ask a question and get an answer asynchronously.
        /// </summary>
        /// <param name="group">
        /// Group name (required, used to track signals). A request for this group cannot be in flight already.
        /// </param>
        /// <param name="questions">Questions to ask (required).</param>
        /// <param name="stateText">
        /// Optional prose describing the situation. The model reads state as text, so a sentence
        /// ("Health is 10 out of 100.") works better than bare numbers. If null, the group's
        /// signals are rendered as "name is value" sentences.
        /// </param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>
        /// The answer from the model, or an answer with Success=false and an Error message if something goes wrong.
        /// </returns>
        public async Task<JevAnswer> QuestionAsync(
            string group,
            JevQuestions questions,
            string stateText = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(group) || questions == null)
                throw new ArgumentException("group and questions are required");

            if (!_inFlight.Add(group))
                throw new InvalidOperationException($"A request for group '{group}' is already in flight");

            try
            {
                if (stateText == null)
                    stateText = JevProtocol.BuildStateText(Context.Snapshot(group));

                string json = JevProtocol.BuildRequestJson(Settings.Model, stateText, questions);
                byte[] body = Encoding.UTF8.GetBytes(json);

                var req = new HttpRequestMessage(HttpMethod.Post, Settings.Endpoint)
                {
                    Content = new ByteArrayContent(body),
                };
                req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                if (!string.IsNullOrEmpty(Settings.ApiKey))
                {
                    req.Headers.Add("x-api-key", Settings.ApiKey);
                }

                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    cts.Timeout = TimeSpan.FromSeconds(Settings.TimeoutSeconds);
                    var response = await _httpClient.SendAsync(req, cts.Token);
                    string responseText = await response.Content.ReadAsStringAsync();

                    var answer = new JevAnswer { Group = group, RawJson = responseText };

                    if (response.IsSuccessStatusCode)
                    {
                        JevProtocol.ParseAnswer(responseText, answer);
                    }
                    else
                    {
                        answer.Error = $"HTTP {(int)response.StatusCode}: {responseText}";
                    }

                    return answer;
                }
            }
            finally
            {
                _inFlight.Remove(group);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
