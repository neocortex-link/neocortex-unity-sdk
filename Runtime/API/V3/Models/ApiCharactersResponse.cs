using System;

namespace Neocortex.API
{
    [Serializable]
    public class ApiCharactersResponse
    {
        public CharacterSummary[] characters;
    }

    [Serializable]
    public class CharacterSummary
    {
        public string id;
        public string name;
    }
}
