using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Neocortex.API
{
    /// <summary>
    /// Fallback enum converter that returns the default enum value (e.g. Emotions.Neutral)
    /// instead of throwing an exception when encountering empty strings ("") or unknown enum names.
    /// </summary>
    public class SafeStringEnumConverter : StringEnumConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            Type underlyingType = Nullable.GetUnderlyingType(objectType) ?? objectType;

            if (reader.TokenType == JsonToken.Null)
            {
                return Nullable.GetUnderlyingType(objectType) != null ? null : Activator.CreateInstance(underlyingType);
            }

            if (reader.TokenType == JsonToken.String)
            {
                string enumText = reader.Value?.ToString();
                if (string.IsNullOrWhiteSpace(enumText))
                {
                    return Nullable.GetUnderlyingType(objectType) != null ? null : Activator.CreateInstance(underlyingType);
                }

                if (Enum.TryParse(underlyingType, enumText, true, out object result))
                {
                    return result;
                }

                return Nullable.GetUnderlyingType(objectType) != null ? null : Activator.CreateInstance(underlyingType);
            }

            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
    }
}
