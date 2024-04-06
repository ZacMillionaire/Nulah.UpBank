namespace Nulah.UpApi.Lib.Models.Shared;

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