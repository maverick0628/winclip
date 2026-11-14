namespace WinClip.Models
{
    /// <summary>
    /// Enum for categorizing types of sensitive data detected in clipboard entries.
    /// </summary>
    public enum SensitiveDataType
    {
        /// <summary>
        /// No sensitive data detected.
        /// </summary>
        None = 0,

        /// <summary>
        /// Password or password-related content detected.
        /// </summary>
        Password = 1,

        /// <summary>
        /// Credit card number detected.
        /// </summary>
        CreditCard = 2,

        /// <summary>
        /// Social Security Number (SSN) detected.
        /// </summary>
        SSN = 3,

        /// <summary>
        /// API key detected.
        /// </summary>
        APIKey = 4,

        /// <summary>
        /// JSON Web Token (JWT) detected.
        /// </summary>
        JWT = 5,

        /// <summary>
        /// AWS access key detected.
        /// </summary>
        AWSKey = 6,

        /// <summary>
        /// Private key (RSA, EC, DSA, etc.) detected.
        /// </summary>
        PrivateKey = 7,

        /// <summary>
        /// Generic sensitive data detected that doesn't fit other categories.
        /// </summary>
        Generic = 8
    }
}
