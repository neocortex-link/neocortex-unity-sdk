namespace Neocortex.Data
{
    public enum ApiResponseType
    {
        Text,
        Audio,

        /// <summary>Incremental body read by a custom handler on <see cref="Neocortex.API.ApiPayload.downloadHandler"/>.</summary>
        Stream
    }
}
