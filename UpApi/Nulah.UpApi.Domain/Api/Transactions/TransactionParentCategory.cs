using Nulah.UpApi.Domain.Api.Categories;
using Nulah.UpApi.Domain.Api.Shared;

namespace Nulah.UpApi.Domain.Api.Transactions;

[ApiModel]
public class TransactionParentCategory
{
	public Category? Data { get; set; }
	public RelatedLink? Links { get; set; }
}