using Nulah.UpApi.Domain.Api.Accounts;
using Nulah.UpApi.Lib.ApiModels.Accounts;

namespace Nulah.UpApi.Domain.Api.Transactions;

[ApiModel]
public class TransactionRelationships
{
	/// <summary>
	/// If this transaction is a transfer between accounts, this relationship will contain the account the transaction went to/came from.
	/// The amount field can be used to determine the direction of the transfer.
	/// </summary>
	public AccountResponse? TransferAccount { get; set; }

	public AccountResponse? Account { get; set; }

	public TransactionCategory? Category { get; set; }
	public TransactionParentCategory? ParentCategory { get; set; }
	public TransactionTags? Tags { get; set; }
}