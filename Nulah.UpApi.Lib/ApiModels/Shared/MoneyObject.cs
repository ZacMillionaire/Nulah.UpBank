namespace Nulah.UpApi.Lib.ApiModels.Shared;

public class MoneyObject
{
	/// <summary>
	/// ISO 4217 currency code describing how <see cref="Value"/> and <see cref="ValueInBaseUnits"/> are displayed
	/// </summary>
	public string CurrencyCode { get; set; }

	/// <summary>
	/// Display value of the currency, based on the ISO 4217 currency code
	/// </summary>
	public string Value { get; set; }

	/// <summary>
	/// Value in the lowest base unit from the currency code.
	/// <para>
	/// eg for AUD and USD, this would be the value represented in cents
	/// </para>
	/// </summary>
	public long ValueInBaseUnits { get; set; }
}