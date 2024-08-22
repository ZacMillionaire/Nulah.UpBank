using Nulah.UpApi.Domain.Api.Shared;

namespace Nulah.UpApi.Domain.Api.Transactions;

[ApiModel]
public class TransactionTags
{
	public List<Tag> Data { get; set; }
	public SelfLink? Links { get; set; }
}