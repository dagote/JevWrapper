using System;

namespace JevWrapper
{
    /// <summary>
    /// Connection settings for one JevClient.
    /// </summary>
    [Serializable]
    public class JevSettings
    {
        public string Endpoint = "https://api.typesafe.ai/v1/systemone";
        public string ApiKey = "";          // set this
        public string Model = "jev-latest";
        public int TimeoutSeconds = 30;

        public JevSettings Clone()
        {
            return (JevSettings)MemberwiseClone();
        }
    }
}
