using Nulah.UpApi.Domain.Api.Shared;
using Nulah.UpApi.Lib.ApiModels.Accounts;

namespace Nulah.UpApi.Domain.Api.Accounts;

public class AccountResponse
{
	public Account? Data { get; set; }

	public RelatedLink? Links { get; set; }
	// public TransactionLink Transactions { get; set; }
	// public SelfLink Links { get; set; }
}