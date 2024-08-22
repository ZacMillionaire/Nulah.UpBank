namespace Nulah.UpApi.Domain.Api.Categories;

public class CategoryRelationship
{
	public CategoryParent parent { get; set; }
	public CategoryChild children { get; set; }
}