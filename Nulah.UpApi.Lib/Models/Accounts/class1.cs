using System.Text.Json.Serialization;
using Nulah.UpApi.Lib.Models.Shared;

namespace Nulah.UpApi.Lib.Models.Accounts;

public class AccountsResponse
{
	public List<Account> Data { get; set; }
	public PaginationLinks Links { get; set; }
}

public class AccountResponse
{
	public Account Data { get; set; }
	// public TransactionLink Transactions { get; set; }
	// public SelfLink Links { get; set; }
}

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

public class AccountAttributes
{
	public string DisplayName { get; set; }

	[JsonConverter(typeof(ResponseEnumConverter<AccountType>))]
	public AccountType AccountType { get; set; }

	[JsonConverter(typeof(ResponseEnumConverter<AccountOwnershipType>))]
	public AccountOwnershipType OwnershipType { get; set; }

	public MoneyObject Balance { get; set; }
	public DateTime CreatedAt { get; set; }
}

public enum AccountType
{
	SAVER,
	TRANSACTIONAL
}

public enum AccountOwnershipType
{
	INDIVIDUAL,
	JOINT
}

//
public class Relationship
{
	public TransactionLink Transactions { get; set; }
}

public class TransactionLink
{
	public RelatedLink Links { get; set; }
}