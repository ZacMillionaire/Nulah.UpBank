﻿using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nulah.UpApi.Domain.Api.Converters;

public class HttpStatusCodeConverter : JsonConverter<HttpStatusCode>
{
	public override HttpStatusCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> Enum.Parse<HttpStatusCode>(reader.GetString());

	public override void Write(Utf8JsonWriter writer, HttpStatusCode value, JsonSerializerOptions options)
	{
		JsonSerializer.Serialize(writer, value, typeof(HttpStatusCode), options);
	}
}