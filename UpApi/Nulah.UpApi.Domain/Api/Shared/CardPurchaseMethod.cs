using Nulah.UpApi.Domain.Api.Enums;

namespace Nulah.UpApi.Domain.Api.Shared;

[ApiModel]
public class CardPurchaseMethod
{
	/// <summary>
	/// The type of card purchase.
	/// </summary>
	public CardPurchaseMethodType MethodType { get; set; }

	/// <summary>
	/// The last four digits of the card used for the purchase, if applicable.
	/// </summary>
	public string? CardNumberSuffix { get; set; }
}