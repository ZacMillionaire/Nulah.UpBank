using Nulah.UpApi.Domain.Api.Shared;

namespace Nulah.UpApi.Domain.Api.Transactions;

[ApiModel]
public class TransactionResponse
{
	public List<Transaction> Data { get; set; }
	public PaginationLinks Links { get; set; }
}