using Nulah.UpApi.Lib.ApiModels.Categories;

namespace Nulah.UpApi.Lib.ApiModels.Categories;

public class CategoryRelationship
{
	public CategoryParent parent { get; set; }
	public CategoryChild children { get; set; }
}