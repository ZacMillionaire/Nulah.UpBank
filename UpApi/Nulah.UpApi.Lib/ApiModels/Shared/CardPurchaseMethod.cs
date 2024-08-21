using System.Text.Json.Serialization;
using Nulah.UpApi.Lib.ApiModels.Enums;

namespace Nulah.UpApi.Lib.ApiModels.Shared;

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