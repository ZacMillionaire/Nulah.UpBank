using System.ComponentModel;
using System.Text.Json.Serialization;
using Nulah.UpApi.Lib.Models.Accounts;
using Nulah.UpApi.Lib.Models.Shared;

namespace Nulah.UpApi.Lib.Models.Transactions;

public class TransactionResponse
{
	public List<Transaction> Data { get; set; }
	public PaginationLinks Links { get; set; }
}

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

public class TransactionRelationships
{
	/// <summary>
	/// If this transaction is a transfer between accounts, this relationship will contain the account the transaction went to/came from.
	/// The amount field can be used to determine the direction of the transfer.
	/// </summary>
	public TransactionTransferAccount TransferAccount { get; set; }

	public AccountResponse Account { get; set; }

	public TransactionCategory Category { get; set; }
	public TransactionParentCategory ParentCategory { get; set; }
	public TransactionTags Tags { get; set; }
}

public class TransactionTransferAccount
{
	public Account? Data { get; set; }
	public RelatedLink? Links { get; set; }
}

public class TransactionCategory
{
	public Category? Data { get; set; }
	public TransactionLinks? Links { get; set; }
}

public class TransactionLinks
{
	/// <summary>
	/// The link to retrieve or modify linkage between this resources and the related resource(s) in this relationship.
	/// </summary>
	public string Self { get; set; }

	/// <summary>
	/// The link to retrieve the related resource(s) in this relationship.
	/// </summary>
	public string? Related { get; set; }
}

public class TransactionParentCategory
{
	public Category? Data { get; set; }
	public RelatedLink? Links { get; set; }
}

public class Category
{
	/// <summary>
	/// Will always be the string "categories" in v1 of the API
	/// </summary>
	public string Type { get; set; }

	/// <summary>
	/// The unique identifier of the resource within its type.
	/// </summary>
	public string Id { get; set; }

	public CategoryAttribute? Attributes { get; set; }
	public CategoryRelationship? Relationships { get; set; }
}

// TODO: move these category classes to their own thing when I implement them
public class CategoryRelationship
{
	public CategoryParent parent { get; set; }
	public CategoryChild children { get; set; }
}

public class CategoryParent
{
	public CategoryData data { get; set; }
}

public class CategoryChild
{
	public List<CategoryData> data { get; set; }
}

public class CategoryData
{
	public string type { get; set; }
	public string id { get; set; }
}

public class TransactionTags
{
	public List<Tag> Data { get; set; }
	public SelfLink? Links { get; set; }
}

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

public class TransactionAttributes
{
	/// <summary>
	/// The current processing status of this transaction, according to whether or not this transaction has settled or is still held.
	/// </summary>
	[JsonConverter(typeof(ResponseEnumConverter<TransactionStatusEnum>))]
	public TransactionStatusEnum Status { get; set; }

	/// <summary>
	/// The original, unprocessed text of the transaction. This is often not a perfect indicator of the actual merchant,
	/// but it is useful for reconciliation purposes in some cases.
	/// </summary>
	public string RawText { get; set; }

	/// <summary>
	/// A short description for this transaction. Usually the merchant name for purchases.
	/// </summary>
	public string Description { get; set; }

	/// <summary>
	/// Attached message for this transaction, such as a payment message, or a transfer note.
	/// </summary>
	public string? Message { get; set; }

	/// <summary>
	/// Boolean flag set to true on transactions that support the use of categories.
	/// </summary>
	public bool IsCategorizable { get; set; }

	/// <summary>
	/// If this transaction is currently in the <see cref="TransactionStatusEnum.HELD"/> status, or was ever in the <see cref="TransactionStatusEnum.HELD"/> status,
	/// the <see cref="HoldInfo.Amount"/> and <see cref="HoldInfo.ForeignAmount"/> of the transaction while <see cref="TransactionStatusEnum.HELD"/>.
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
	public Cashback Cashback { get; set; }

	/// <summary>
	/// The amount of this transaction in Australian dollars. 
	/// For transactions that were once <see cref="TransactionStatusEnum.HELD"/> but are now <see cref="TransactionStatusEnum.SETTLED"/>,
	/// refer to the <see cref="HoldInfo"/> field for the original <see cref="HoldInfo.Amount"/> the transaction was <see cref="TransactionStatusEnum.HELD"/> at.
	/// </summary>
	public MoneyObject Amount { get; set; }

	/// <summary>
	/// The foreign currency amount of this transaction. This field will be null for domestic transactions. 
	/// The amount was converted to the AUD amount reflected in the <see cref="Amount"/> of this transaction. 
	/// Refer to the <see cref="HoldInfo"/> field for the original <see cref="HoldInfo.ForeignAmount"/> the transaction was <see cref="TransactionStatusEnum.HELD"/> at.
	/// </summary>
	public MoneyObject? ForeignAmount { get; set; }

	/// <summary>
	/// Information about the card used for this transaction, if applicable.
	/// </summary>
	public CardPurchaseMethod CardPurchaseMethod { get; set; }

	/// <summary>
	/// The date-time at which this transaction settled. This field will be null for transactions
	/// that are currently in the <see cref="TransactionStatusEnum.HELD"/> status.
	/// </summary>
	public DateTime? SettledAt { get; set; }

	/// <summary>
	/// The date-time at which this transaction was first encountered.
	/// </summary>
	public DateTime CreatedAt { get; set; }
}

public class HoldInfo
{
	/// <summary>
	/// The amount of this transaction while in the <see cref="TransactionStatusEnum.HELD"/> status, in Australian dollars.
	/// </summary>
	public MoneyObject Amount { get; set; }

	/// <summary>
	/// The foreign currency amount of this transaction while in the <see cref="TransactionStatusEnum.HELD"/> status.
	/// This field will be null for domestic transactions. The amount was converted to the <see cref="TransactionStatusEnum.HELD"/> amount 
	/// reflected in the <see cref="Amount"/> field.
	/// </summary>
	public MoneyObject? ForeignAmount { get; set; }
}

public class RoundUp
{
	/// <summary>
	/// The total amount of this Round Up, including any boosts, represented as a negative value.
	/// </summary>
	public MoneyObject Amount { get; set; }

	/// <summary>
	/// The portion of the Round Up <see cref="Amount"/> owing to boosted Round Ups, 
	/// represented as a negative value. If no boost was added to the Round Up this field will be null.
	/// </summary>
	public MoneyObject? BoostPortion { get; set; }
}

public class Cashback
{
	/// <summary>
	/// A brief description of why this cashback was paid.
	/// </summary>
	public string Description { get; set; }

	/// <summary>
	/// The total amount of cashback paid, represented as a positive value.
	/// </summary>
	public MoneyObject Amount { get; set; }
}

public class CardPurchaseMethod
{
	/// <summary>
	/// The type of card purchase.
	/// </summary>
	[JsonConverter(typeof(ResponseEnumConverter<CardPurchaseMethodEnum>))]
	public CardPurchaseMethodEnum Method { get; set; }

	/// <summary>
	/// The last four digits of the card used for the purchase, if applicable.
	/// </summary>
	public string? CardNumberSuffix { get; set; }
}

public enum TransactionStatusEnum
{
	/// <summary>
	/// Pending (maybe?)
	/// </summary>
	HELD,
	SETTLED
}

public enum CardPurchaseMethodEnum
{
	BAR_CODE,
	OCR,
	CARD_PIN,
	CARD_DETAILS,
	CARD_ON_FILE,
	ECOMMERCE,
	MAGNETIC_STRIPE,
	CONTACTLESS
}