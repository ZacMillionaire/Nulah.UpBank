namespace Nulah.UpApi.Lib.Models;

public class PingResponse
{
	public Meta Meta { get; set; }
}

public class Meta
{
	public string Id { get; set; }
	public string StatusEmoji { get; set; }
}