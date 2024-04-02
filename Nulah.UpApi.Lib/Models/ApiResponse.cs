using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nulah.UpApi.Lib.Models;

public class ApiResponse<T> where T : class
{
	public bool Success => Errors == null && Response != default(T);
	public T? Response { get; set; }
	public List<Error>? Errors { get; set; }
}

public static class ApiResponse
{
	public static ApiResponse<T> FromSuccess<T>(T? res) where T : class
	{
		return new ApiResponse<T>
		{
			Response = res
		};
	}

	public static ApiResponse<T> FromError<T>(ErrorResponse? errorResponse) where T : class
	{
		return new ApiResponse<T>
		{
			Errors = errorResponse?.Errors
		};
	}
}

public class ErrorResponse
{
	/// <summary>
	/// The list of errors returned in this response.
	/// </summary>
	public List<Error> Errors { get; set; }
}

public class Error
{
	/// <summary>
	/// The HTTP status code associated with this error. This can also be obtained from the response headers. The status indicates the broad type of error according to HTTP semantics.
	/// </summary>
	[JsonConverter(typeof(HttpStatusCodeConverter))]
	public HttpStatusCode Status { get; set; }

	/// <summary>
	/// A short description of this error. This should be stable across multiple occurrences of this type of error and typically expands on the reason for the status code.
	/// </summary>
	public string? Title { get; set; }

	/// <summary>
	/// A detailed description of this error. This should be considered unique to individual occurrences of an error and subject to change. It is useful for debugging purposes.
	/// </summary>
	public string? Detail { get; set; }

	/// <summary>
	/// If applicable, location in the request that this error relates to. This may be a parameter in the query string, or a an attribute in the request body.
	/// </summary>
	public ErrorSource? Source { get; set; }
}

public class ErrorSource
{
	/// <summary>
	/// If this error relates to a query parameter, the name of the parameter.
	/// </summary>
	public string? Parameter { get; set; }

	/// <summary>
	/// If this error relates to an attribute in the request body, a rfc-6901 JSON pointer to the attribute.
	/// </summary>
	public string? Pointer { get; set; }
}

public class HttpStatusCodeConverter : JsonConverter<HttpStatusCode>
{
	public override HttpStatusCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> Enum.Parse<HttpStatusCode>(reader.GetString());

	public override void Write(Utf8JsonWriter writer, HttpStatusCode value, JsonSerializerOptions options)
	{
		JsonSerializer.Serialize(writer, value, typeof(HttpStatusCode), options);
	}
}

public class ResponseEnumConverter<T> : JsonConverter<T> where T : Enum
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