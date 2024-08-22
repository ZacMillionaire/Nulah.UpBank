using Marten;
using Microsoft.Extensions.Logging;
using Nulah.UpApi.Domain.Api.Categories;
using Nulah.UpApi.Domain.Interfaces;
using Nulah.UpApi.Domain.Models;

namespace Nulah.UpApi.Lib.Controllers;

public class CategoryController
{
	private readonly IUpBankApi _upBankApi;
	private readonly IUpStorage _upStorage;
	private readonly ILogger<CategoryController> _logger;


	public Action<CategoryController, EventArgs>? CategoriesUpdating;
	public Action<CategoryController, IReadOnlyList<UpCategory>>? CategoriesUpdated;

	public CategoryController(IUpBankApi upBankApi, IUpStorage upStorage, ILogger<CategoryController> logger)
	{
		_upBankApi = upBankApi;
		_upStorage = upStorage;
		// TODO: implement logging when I get around to updating and setting categories
		_logger = logger;
	}

	public async Task<IReadOnlyList<UpCategory>> GetCategories(bool bypassCache = false)
	{
		CategoriesUpdating?.Invoke(this, EventArgs.Empty);


		if (!bypassCache)
		{
			var existingCategories = await _upStorage.LoadCategoriesFromCacheAsync();

			// We duplicate code slightly here to retrieve categories from the Api if _no_ categories
			// exist in the database.
			if (existingCategories.Count != 0)
			{
				CategoriesUpdated?.Invoke(this, existingCategories);
				return existingCategories;
			}
		}

		var categories = await GetCategoriesFromApi();

		foreach (var category in categories.Where(x => x.ParentCategoryId != null))
		{
			category.Parent = categories.FirstOrDefault(x => x.Id == category.ParentCategoryId);
		}

		await _upStorage.SaveCategoriesToCacheAsync(categories);

		CategoriesUpdated?.Invoke(this, categories);
		return categories;
	}


	private async Task<List<UpCategory>> GetCategoriesFromApi(string? nextPage = null)
	{
		var categories = new List<UpCategory>();
		var apiResponse = await _upBankApi.GetCategories(nextPage);

		if (apiResponse is { Success: true, Response: not null })
		{
			categories.AddRange(apiResponse.Response.Data
				.Select(x => new UpCategory()
				{
					Id = x.Id,
					Name = x.Attributes.Name,
					Type = x.Type,
					ParentCategoryId = x.Relationships?.parent.data?.id
				}));
		}

		return categories;
	}


	/// <summary>
	/// <para>
	/// Returns a category for transactions when caching. If <paramref name="rawCategory"/> is null, null is returned.
	/// </para>
	/// <para>
	///	If <paramref name="categoryLookup"/> is null or contains no elements, or the id from <paramref name="rawCategory"/> cannot be found
	/// as a key value, a category is returned with the id and type from <paramref name="rawCategory"/>.
	/// </para>
	/// <para>
	///	Otherwise the matched category by id is returned from <paramref name="categoryLookup"/>
	/// </para>
	/// </summary>
	/// <param name="rawCategory"></param>
	/// <param name="categoryLookup"></param>
	/// <returns></returns>
	internal UpCategory? LookupCategory(Category? rawCategory = null, Dictionary<string, UpCategory>? categoryLookup = null)
	{
		// If we have no category return no category
		if (rawCategory == null)
		{
			return null;
		}

		// If we have no lookup dictionary, guess a category from the raw category given. We'll be missing some information
		// but that's still adequate
		if (categoryLookup == null || categoryLookup.Count == 0)
		{
			return new UpCategory()
			{
				Id = rawCategory.Id,
				Type = rawCategory.Type
			};
		}

		// Try find the category by id and return the populated and previously cached (hopefully) category with full details
		if (categoryLookup.TryGetValue(rawCategory.Id, out var categoryMatch))
		{
			return categoryMatch;
		}

		// otherwise our fallback is to create a category from the raw category given which contains the Id that we'll be indexing
		// and querying off anyway. Name is a nice to have essentially
		return new UpCategory()
		{
			Id = rawCategory.Id,
			Type = rawCategory.Type
		};
	}
}