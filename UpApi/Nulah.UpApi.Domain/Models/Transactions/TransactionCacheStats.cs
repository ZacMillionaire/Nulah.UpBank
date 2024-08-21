namespace Nulah.UpApi.Domain.Models.Transactions;

public class TransactionCacheStats
{
	public long Count { get; set; }
	public DateTimeOffset? MostRecentTransactionDate { get; set; }
	public DateTimeOffset? FirstTransactionDate { get; set; }
	public IReadOnlyList<CategoryStat> CategoryStats { get; set; } = [];
}

public class CategoryStat
{
	public string CategoryId { get; set; } = null!;
	public int Count { get; set; }
	public string Name { get; set; } = null!;
}