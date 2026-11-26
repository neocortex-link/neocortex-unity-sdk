using System;

namespace Neocortex.Data
{
    /// <summary>
    ///     A failed request, classified by the API's error contract, so a game can branch on what happened.
    /// </summary>
    /// <remarks>
    ///     401 invalid key; 402 insufficient_credits; 403 inactive key or plan required;
    ///     429 rate_limit_exceeded, character_credit_limit, player_credit_limit;
    ///     502 speech_failed, transcription_failed; 503 database_unavailable; 504 upstream_timeout.
    /// </remarks>
    [Serializable]
    public class RequestError
    {
        public const string InsufficientCredits = "insufficient_credits";
        public const string CharacterCreditLimit = "character_credit_limit";
        public const string PlayerCreditLimit = "player_credit_limit";
        public const string RateLimitExceeded = "rate_limit_exceeded";
        public const string SpeechFailed = "speech_failed";
        public const string TranscriptionFailed = "transcription_failed";
        public const string DatabaseUnavailable = "database_unavailable";
        public const string UpstreamTimeout = "upstream_timeout";

        /// <summary>HTTP status, or 0 when the request never got an answer.</summary>
        public long status;

        /// <summary>The API's machine readable code, or null when it sent none.</summary>
        public string code;

        /// <summary>A short, readable message, safe to show in a log or a debug label.</summary>
        public string message;

        /// <summary>This side gave up waiting.</summary>
        public bool timedOut;

        /// <summary>The URL that failed, for logs.</summary>
        public string url;

        /// <summary>True when the same request a moment later can succeed.</summary>
        public bool IsRetryable() =>
            timedOut || code == DatabaseUnavailable || code == UpstreamTimeout || status == 503 || status == 504;

        public bool IsUnauthorized() => status == 401;

        public bool IsForbidden() => status == 403;

        /// <summary>True when the team, the character or the player is out of credit.</summary>
        public bool IsOutOfCredits() => status == 402 || IsCreditCode(code);

        /// <summary>True for a 429 that is a rate limit rather than a credit cap.</summary>
        public bool IsRateLimited() => status == 429 && !IsCreditCode(code);

        public bool IsSpeechFailure() => code == SpeechFailed;

        public bool IsTranscriptionFailure() => code == TranscriptionFailed;

        public override string ToString() => message;

        private static bool IsCreditCode(string value) =>
            value == InsufficientCredits || value == CharacterCreditLimit || value == PlayerCreditLimit;
    }
}
