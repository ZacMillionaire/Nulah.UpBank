using Nulah.UpApi.Domain.Api.Enums;
using Nulah.UpApi.Domain.Api.Shared;

namespace Nulah.UpApi.Domain.Api.Accounts;

public class AccountAttributes
{
	public string DisplayName { get; set; }

	public AccountType AccountType { get; set; }

	public AccountOwnershipType OwnershipType { get; set; }

	public MoneyObject Balance { get; set; }
	public DateTime CreatedAt { get; set; }
}