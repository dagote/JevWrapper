using System;
using UnityEngine;

namespace JevWrapper
{
    /// <summary>
    /// Minimal public API for the Jev wrapper, backed by a shared default client.
    /// Groups are required. Context is automatic.
    /// For independent settings or groups, create your own JevClient instead.
    /// </summary>
    public static class Jev
    {
        public static JevClient Default { get; private set; } = new JevClient();

        public static string Endpoint { get => Default.Settings.Endpoint; set => Default.Settings.Endpoint = value; }
        public static string ApiKey { get => Default.Settings.ApiKey; set => Default.Settings.ApiKey = value; }
        public static string Model { get => Default.Settings.Model; set => Default.Settings.Model = value; }

        public static void Update(string name, string group, float value)
        {
            Default.Update(name, group, value);
        }

        public static bool Question(string group, JevQuestions questions, Action<JevAnswer> onAnswer,
                                    string stateText = null, UnityEngine.Object owner = null)
        {
            return Default.Question(group, questions, onAnswer, stateText, owner);
        }

        // Statics survive Play Mode when "Reload Domain" is off; start every play session clean.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession()
        {
            Default = new JevClient();
            JevRunner.Reset();
        }
    }
}
