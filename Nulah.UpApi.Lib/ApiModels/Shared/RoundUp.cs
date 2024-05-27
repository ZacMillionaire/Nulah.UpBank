namespace Nulah.UpApi.Lib.ApiModels.Shared;

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