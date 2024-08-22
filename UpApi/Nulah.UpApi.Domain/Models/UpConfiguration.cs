namespace Nulah.UpApi.Domain.Models;

public class UpConfiguration
{
	public string? AccessToken { get; set; }
	public string ApiBaseAddress { get; set; } = "https://api.up.com.au/api/v1";
}