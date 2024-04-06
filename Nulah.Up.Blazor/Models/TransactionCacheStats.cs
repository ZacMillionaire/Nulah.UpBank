namespace Nulah.Up.Blazor.Models;

public class TransactionCacheStats
{
	public long Count { get; set; }
	public DateTimeOffset? MostRecentTransactionDate { get; set; }
	public DateTimeOffset? FirstTransactionDate { get; set; }
}