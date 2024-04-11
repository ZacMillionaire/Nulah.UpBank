namespace Nulah.Up.Blazor.Models.Criteria;

public class TransactionQueryCriteria
{
	public string? AccountId { get; set; }
	public DateTimeOffset? Since { get; set; }
	public DateTimeOffset? Until { get; set; }
	public bool ExcludeUncategorisableTransactions { get; set; }
	public List<TransactionType> TransactionTypes { get; set; } = new();
}