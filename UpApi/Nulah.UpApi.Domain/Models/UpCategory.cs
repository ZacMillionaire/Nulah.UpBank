using System.ComponentModel.DataAnnotations.Schema;

namespace Nulah.UpApi.Domain.Models;

public class UpCategory
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Type { get; set; }

	public string? ParentCategoryId { get; set; }
	
	[NotMapped]
	public UpCategory? Parent { get; set; }
}