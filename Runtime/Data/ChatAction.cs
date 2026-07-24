using System;

namespace Neocortex.Data
{
    /// <summary>
    ///     One action the character triggered, in the order it intends to perform it.
    ///     <see cref="targetId"/> is the id of the perceived entity the action applies to — the
    ///     <see cref="NeocortexInteractable"/> it means — or empty for actions that need no object.
    ///
    ///     Because each action carries its own target, a single reply can stack several actions
    ///     pointing at different things ("go to the blue cube, then the red one").
    /// </summary>
    [Serializable]
    public class ChatAction
    {
        public string name;
        public string targetId;
    }
}
