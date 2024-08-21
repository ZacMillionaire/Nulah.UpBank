namespace Nulah.UpApi.Lib.ApiModels.Shared;

public class PaginationLinks
{
    /// <summary>
    /// The link to the previous page in the results. If this value is null there is no previous page.
    /// </summary>
    public string? Prev { get; set; }
    /// <summary>
    /// The link to the next page in the results. If this value is null there is no next page.
    /// </summary>
    public string? Next { get; set; }
}