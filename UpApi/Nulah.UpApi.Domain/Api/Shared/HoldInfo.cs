using Nulah.UpApi.Domain.Api.Enums;

namespace Nulah.UpApi.Domain.Api.Shared;

[ApiModel]
public class HoldInfo
{
	/// <summary>
	/// The amount of this transaction while in the <see cref="TransactionStatus.HELD"/> status, in Australian dollars.
	/// </summary>
	public MoneyObject Amount { get; set; }

	/// <summary>
	/// The foreign currency amount of this transaction while in the <see cref="TransactionStatus.HELD"/> status.
	/// This field will be null for domestic transactions. The amount was converted to the <see cref="TransactionStatus.HELD"/> amount 
	/// reflected in the <see cref="Amount"/> field.
	/// </summary>
	public MoneyObject? ForeignAmount { get; set; }
}