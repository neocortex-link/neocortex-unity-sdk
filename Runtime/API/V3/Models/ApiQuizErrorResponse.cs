using System;
using Neocortex.Data;

namespace Neocortex.API
{
    /// <summary>
    ///     A quiz failure that carries a turn with it.
    ///
    ///     Sending to a run that has already finished answers 409 with the closing turn it gave
    ///     last time, so a client whose response was lost in transit can still speak the sign off
    ///     rather than ending the game on silence.
    /// </summary>
    [Serializable]
    public class ApiQuizErrorResponse : ApiErrorResponse
    {
        public QuizTurnResponse turn;
    }
}
