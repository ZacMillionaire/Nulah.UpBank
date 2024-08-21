using Nulah.UpApi.Domain.Api.Categories;

namespace Nulah.UpApi.Domain.Api.Transactions;

[ApiModel]
public class TransactionCategory
{
	public Category? Data { get; set; }
	public TransactionLinks? Links { get; set; }
}