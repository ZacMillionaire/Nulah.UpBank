using Nulah.UpApi.Domain.Api.Shared;

namespace Nulah.UpApi.Domain.Api.Accounts;

public class Account
{
	/// <summary>
	/// Will always be the string "accounts" in v1 of the API
	/// </summary>
	public string Type { get; set; }

	/// <summary>
	/// The unique identifier of the resource within its type.
	/// </summary>
	public string Id { get; set; }

	public AccountAttributes Attributes { get; set; }
	public Relationship Relationships { get; set; }
	public SelfLink Links { get; set; }

	public override string ToString()
	{
		return Attributes.DisplayName;
	}
}