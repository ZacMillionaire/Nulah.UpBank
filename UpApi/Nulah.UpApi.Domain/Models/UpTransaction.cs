using Nulah.UpApi.Domain.Api.Enums;
using Nulah.UpApi.Domain.Api.Shared;
using Nulah.UpApi.Domain.Api.Transactions;
using Nulah.UpApi.Domain.Models.Transactions;
namespace Nulah.UpApi.Domain.Models;

public class UpTransaction
{
	public string Id { get; set; } = null!;
	public string? AccountId { get; set; }

	/// <summary>
	/// The current processing status of this transaction, according to whether or not this transaction has settled or is still held.
	/// </summary>
	public TransactionStatus Status { get; set; }

	/// <summary>
	/// The original, unprocessed text of the transaction. This is often not a perfect indicator of the actual merchant,
	/// but it is useful for reconciliation purposes in some cases.
	/// </summary>
	public string? RawText { get; set; }

	/// <summary>
	/// A short description for this transaction. Usually the merchant name for purchases.
	/// </summary>
	public string Description { get; set; } = null!;

	/// <summary>
	/// Attached message for this transaction, such as a payment message, or a transfer note.
	/// </summary>
	public string? Message { get; set; }

	/// <summary>
	/// Boolean flag set to true on transactions that support the use of categories.
	/// </summary>
	public bool IsCategorizable { get; set; }

	/// <summary>
	/// If this transaction is currently in the <see cref="TransactionStatus.HELD"/> status, or was ever in the <see cref="TransactionStatus.HELD"/> status,
	/// the <see cref="HoldInfo.Amount"/> and <see cref="HoldInfo.ForeignAmount"/> of the transaction while <see cref="TransactionStatus.HELD"/>.
	/// </summary>
	// what even is this sentence...It's straight from the offical v1 documentation though
	public HoldInfo? HoldInfo { get; set; }

	/// <summary>
	/// Details of how this transaction was rounded-up. If no Round Up was applied this field will be null.
	/// </summary>
	public RoundUp? RoundUp { get; set; }

	/// <summary>
	/// If all or part of this transaction was instantly reimbursed in the form of cashback, details of the reimbursement.
	/// </summary>
	public Cashback? Cashback { get; set; }

	/// <summary>
	/// The amount of this transaction in Australian dollars. 
	/// For transactions that were once <see cref="TransactionStatus.HELD"/> but are now <see cref="TransactionStatus.SETTLED"/>,
	/// refer to the <see cref="HoldInfo"/> field for the original <see cref="HoldInfo.Amount"/> the transaction was <see cref="TransactionStatus.HELD"/> at.
	/// </summary>
	public MoneyObject? Amount { get; set; }

	/// <summary>
	/// The foreign currency amount of this transaction. This field will be null for domestic transactions. 
	/// The amount was converted to the AUD amount reflected in the <see cref="Amount"/> of this transaction. 
	/// Refer to the <see cref="HoldInfo"/> field for the original <see cref="HoldInfo.ForeignAmount"/> the transaction was <see cref="TransactionStatus.HELD"/> at.
	/// </summary>
	public MoneyObject? ForeignAmount { get; set; }

	/// <summary>
	/// Information about the card used for this transaction, if applicable.
	/// </summary>
	public CardPurchaseMethod? CardPurchaseMethod { get; set; }

	/// <summary>
	/// The date-time at which this transaction settled. This field will be null for transactions
	/// that are currently in the <see cref="TransactionStatus.HELD"/> status.
	/// </summary>
	public DateTime? SettledAt { get; set; }

	/// <summary>
	/// The date-time at which this transaction was first encountered.
	/// </summary>
	public DateTime CreatedAt { get; set; }

	public UpCategory? Category { get; set; }
	public UpCategory? CategoryParent { get; set; }
	public List<Tag> Tags { get; set; } = new();
	public string? TransferAccountId { get; set; }
	public TransactionType InferredType { get; set; }
}