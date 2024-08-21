namespace Nulah.UpApi.Domain.Api.Transactions;

[ApiModel]
public class TransactionLinks
{
	/// <summary>
	/// The link to retrieve or modify linkage between this resources and the related resource(s) in this relationship.
	/// </summary>
	public string Self { get; set; }

	/// <summary>
	/// The link to retrieve the related resource(s) in this relationship.
	/// </summary>
	public string? Related { get; set; }
}