using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace JevWrapper
{
    /// <summary>
    /// One connection to Jev with its own settings and signal context.
    /// Give each system its own client so they don't share a model, key or groups.
    /// </summary>
    public class JevClient
    {
        public JevSettings Settings { get; }
        public JevContext Context { get; } = new JevContext();

        private readonly HashSet<string> _inFlight = new HashSet<string>();

        public JevClient(JevSettings settings = null)
        {
            Settings = settings ?? new JevSettings();
        }

        public void Update(string name, string group, float value)
        {
            Context.Update(name, group, value);
        }

        /// <param name="stateText">
        /// Optional prose describing the situation. The model reads state as text, so a sentence
        /// ("Health is 10 out of 100.") works better than bare numbers. If null, the group's
        /// signals are rendered as "name is value" sentences.
        /// </param>
        /// <param name="owner">
        /// Optional. If this object is destroyed before the answer arrives, the request is aborted
        /// and the callback is not called.
        /// </param>
        /// <returns>
        /// False, with no callback, if the arguments are invalid or a request for this group is
        /// already in flight.
        /// </returns>
        public bool Question(string group, JevQuestions questions, Action<JevAnswer> onAnswer,
                             string stateText = null, UnityEngine.Object owner = null)
        {
            if (string.IsNullOrEmpty(group) || questions == null || onAnswer == null) return false;
            if (!_inFlight.Add(group)) return false;
            JevRunner.Instance.StartCoroutine(DoQuestion(group, questions, onAnswer, stateText, owner));
            return true;
        }

        private IEnumerator DoQuestion(string group, JevQuestions questions, Action<JevAnswer> onAnswer,
                                       string stateText, UnityEngine.Object owner)
        {
            bool hasOwner = owner != null;
            try
            {
                if (stateText == null) stateText = JevProtocol.BuildStateText(Context.Snapshot(group));
                string json = JevProtocol.BuildRequestJson(Settings.Model, stateText, questions);

                using (var req = new UnityWebRequest(Settings.Endpoint, "POST"))
                {
                    byte[] body = Encoding.UTF8.GetBytes(json);
                    req.uploadHandler = new UploadHandlerRaw(body);
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.timeout = Settings.TimeoutSeconds;
                    req.SetRequestHeader("Content-Type", "application/json");
                    if (!string.IsNullOrEmpty(Settings.ApiKey))
                    {
                        req.SetRequestHeader("x-api-key", Settings.ApiKey);
                        req.SetRequestHeader("Authorization", "Bearer " + Settings.ApiKey);
                    }

                    var op = req.SendWebRequest();
                    while (!op.isDone)
                    {
                        if (hasOwner && owner == null)
                        {
                            req.Abort();
                            yield break;
                        }
                        yield return null;
                    }
                    if (hasOwner && owner == null) yield break;

                    var answer = new JevAnswer { Group = group, RawJson = req.downloadHandler.text };

                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        JevProtocol.ParseAnswer(req.downloadHandler.text, answer);
                    }
                    else
                    {
                        answer.Error = req.error;
                        Debug.LogWarning($"Jev request failed: {req.error}\n{req.downloadHandler.text}");
                    }

                    // callback on main thread (coroutine already is)
                    onAnswer(answer);
                }
            }
            finally
            {
                _inFlight.Remove(group);
            }
        }
    }

    /// <summary>
    /// Hidden runner so we can start coroutines from non-MonoBehaviour code.
    /// </summary>
    internal class JevRunner : MonoBehaviour
    {
        private static JevRunner _instance;
        public static JevRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("JevRunner");
                    _instance = go.AddComponent<JevRunner>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        internal static void Reset()
        {
            _instance = null;
        }
    }
}
