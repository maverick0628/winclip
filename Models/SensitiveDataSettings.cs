using System;

namespace WinClip.Models
{
    /// <summary>
    /// Represents configurable settings for sensitive data handling and auto-expiry
    /// </summary>
    public class SensitiveDataSettings
    {
        /// <summary>
        /// The duration in seconds after which sensitive clipboard entries should automatically expire and be deleted
        /// </summary>
        public int ExpiryDurationSeconds { get; set; }

        /// <summary>
        /// Indicates whether automatic expiry of sensitive clipboard entries is enabled
        /// </summary>
        public bool AutoExpiryEnabled { get; set; }

        public SensitiveDataSettings()
        {
            ExpiryDurationSeconds = 60;
            AutoExpiryEnabled = true;
        }
    }
}
