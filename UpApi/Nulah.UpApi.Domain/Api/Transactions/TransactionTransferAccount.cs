using Nulah.UpApi.Domain.Api.Accounts;
using Nulah.UpApi.Domain.Api.Shared;
using Nulah.UpApi.Lib.ApiModels.Accounts;

namespace Nulah.UpApi.Domain.Api.Transactions;

[ApiModel]
public class TransactionTransferAccount
{
	public Account? Data { get; set; }
	public RelatedLink? Links { get; set; }
}