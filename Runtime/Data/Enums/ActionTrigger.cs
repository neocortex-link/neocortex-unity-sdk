namespace Neocortex.Data
{
    /// <summary>When a reply's actions are run.</summary>
    public enum ActionTrigger
    {
        /// <summary>The instant the reply arrives, the character may act before it speaks.</summary>
        WhenResponseReceived,

        /// <summary>As the character starts speaking, action and voice line begin together.</summary>
        WhenSpeechStarts,

        /// <summary>Once the character has finished speaking the reply.</summary>
        AfterReplySpoken,
    }
}
