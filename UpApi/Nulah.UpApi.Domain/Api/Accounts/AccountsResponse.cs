using Nulah.UpApi.Domain.Api.Shared;
using Nulah.UpApi.Lib.ApiModels.Accounts;

namespace Nulah.UpApi.Domain.Api.Accounts;

public class AccountsResponse
{
	public List<Account> Data { get; set; }
	public PaginationLinks Links { get; set; }
}