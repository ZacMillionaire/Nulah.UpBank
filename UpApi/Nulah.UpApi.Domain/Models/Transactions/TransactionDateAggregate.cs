namespace Nulah.UpApi.Domain.Models.Transactions;

/// <summary>
/// Represents an aggregate of transactions for the given date
/// </summary>
public class TransactionDateAggregate
{
	public DateTime Date { get; set; }
	public double Total { get; set; }
}