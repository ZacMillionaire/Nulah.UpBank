using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nulah.UpApi.Lib.Models.Converters;

internal class ResponseEnumConverter<T> : JsonConverter<T> where T : Enum
{
	public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var a = (T)Enum.Parse(typeof(T), reader.GetString()!);
		return a;
	}

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		JsonSerializer.Serialize(writer, value, typeof(T), options);
	}
}