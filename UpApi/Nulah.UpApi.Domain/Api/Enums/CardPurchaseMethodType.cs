namespace Nulah.UpApi.Domain.Api.Enums;

[ApiModel]
public enum CardPurchaseMethodType
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