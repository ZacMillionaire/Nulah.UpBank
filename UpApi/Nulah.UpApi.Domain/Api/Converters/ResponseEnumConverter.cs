using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nulah.UpApi.Domain.Api.Converters;

public class ResponseEnumConverter<T> : JsonConverter<T> where T : Enum
{
	public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.String:
			{
				var enumFromString = (T)Enum.Parse(typeof(T), reader.GetString()!);
				return enumFromString;
			}
			case JsonTokenType.Number:
			{
				var enumFromNumber = (T)Enum.ToObject(typeof(T), reader.GetInt32()!);
				return enumFromNumber;
			}
			default:
				return default!;
		}
	}

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		JsonSerializer.Serialize(writer, value, typeof(T), options);
	}
}