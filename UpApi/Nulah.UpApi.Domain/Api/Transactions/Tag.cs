namespace Nulah.UpApi.Domain.Api.Transactions;

[ApiModel]
public class Tag
{
	/// <summary>
	/// Will always be the string "tags" in v1 of the API
	/// </summary>
	public string Type { get; set; }

	/// <summary>
	/// The label of the tag, which also acts as the tag’s unique identifier.
	/// </summary>
	public string Id { get; set; }
}