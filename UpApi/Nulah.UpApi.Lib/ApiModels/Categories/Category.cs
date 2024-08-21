using Nulah.UpApi.Lib.ApiModels.Categories;

namespace Nulah.UpApi.Lib.ApiModels.Categories;

public class Category
{
	/// <summary>
	/// Will always be the string "categories" in v1 of the API
	/// </summary>
	public string Type { get; set; }

	/// <summary>
	/// The unique identifier of the resource within its type.
	/// </summary>
	public string Id { get; set; }

	public CategoryAttribute Attributes { get; set; } = null!;
	public CategoryRelationship? Relationships { get; set; }
}