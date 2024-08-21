using Nulah.UpApi.Domain.Api.Shared;

namespace Nulah.UpApi.Domain.Api.Transactions;

[ApiModel]
public class Transaction
{
	/// <summary>
	/// Will always be the string "transactions" in v1 of the API
	/// </summary>
	public string Type { get; set; }

	/// <summary>
	/// The unique identifier for this transaction.
	/// </summary>
	public string Id { get; set; }

	public TransactionAttributes Attributes { get; set; }
	public TransactionRelationships Relationships { get; set; }
	public SelfLink Links { get; set; }
}