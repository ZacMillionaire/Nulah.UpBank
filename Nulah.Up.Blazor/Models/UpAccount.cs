using Marten.Metadata;
using Nulah.UpApi.Lib.Models.Accounts;
using Nulah.UpApi.Lib.Models.Shared;

namespace Nulah.Up.Blazor.Models;

public class UpAccount
{
	/// <summary>
	/// Will always be the string "accounts" in v1 of the API
	/// </summary>
	public string Type { get; set; } = null!;

	/// <summary>
	/// The unique identifier of the resource within its type.
	/// </summary>
	public string Id { get; set; } = null!;

	/// <summary>
	/// User defined account name
	/// </summary>
	public string DisplayName { get; set; } = null!;

	public AccountType AccountType { get; set; }
	public AccountOwnershipType OwnershipType { get; set; }
	public MoneyObject Balance { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTimeOffset ModifiedAt { get; set; }
}