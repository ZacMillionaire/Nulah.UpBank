using Nulah.UpApi.Domain.Api.Enums;
using Nulah.UpApi.Domain.Api.Shared;

namespace Nulah.UpApi.Domain.Models;

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