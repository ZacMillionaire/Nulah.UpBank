using Marten;
using MudBlazor.Services;
using Nulah.Up.Blazor.Pages;
using Nulah.Up.Blazor.Services;
using Nulah.UpApi.Domain.Models;
using Nulah.UpApi.Lib;
using Nulah.UpApi.Lib.Controllers;
using Weasel.Core;

namespace Nulah.Up.Blazor;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		builder.Configuration.AddEnvironmentVariables(prefix: "NulahUpBank_");

		// Add services to the container.
		builder.Services.AddRazorComponents()
			.AddInteractiveServerComponents();

		builder.Services.AddMudServices();
		// Ensure we use httpclient correctly by leveraging what the framework can provide us.
		// Theoretically this _should_ handle the correct usage of httpclient and avoid any common pitfalls with httpclient
		// lifetime.
		builder.Services.AddHttpClient();
		// TODO: look at rolling configuration into something from configuration using an options builder pattern
		builder.Services.AddSingleton(x => new UpConfiguration()
		{
			AccessToken = builder.Configuration["Api:UpBank"]
		});
		builder.Services.AddScoped<IUpBankApi, UpBankApi>();

		builder.Services.AddScoped<AccountController>();
		builder.Services.AddScoped<TransactionController>();
		builder.Services.AddScoped<CategoryController>();

		AddPageServices(builder);

		builder.Services.AddMarten(options =>
			{
				// Establish the connection string to your Marten database
				options.Connection(builder.Configuration.GetConnectionString("Postgres"));
				options.DisableNpgsqlLogging = true;

				options.UseSystemTextJsonForSerialization();

				// If we're running in development mode, let Marten just take care
				// of all necessary schema building and patching behind the scenes
				if (builder.Environment.IsDevelopment())
				{
					options.AutoCreateSchemaObjects = AutoCreate.All;
				}

				options.Schema.For<UpAccount>().Metadata(x =>
				{
					x.LastModified.MapTo(y => y.ModifiedAt);
				});

				options.Schema.For<UpTransaction>()
					.Index(x => x.AccountId, x => x.Name = "account_id")
					.Index(x => x.Category.Id, x => x.Name = "category_id");

				options.Schema.For<UpCategory>()
					.Index(x => x.ParentCategoryId, x => x.Name = "parent_category_id");
			})
			.ApplyAllDatabaseChangesOnStartup()
			.OptimizeArtifactWorkflow()
			.UseLightweightSessions();
		// enable once we're adding a dbcontext
		//.UseNpgsqlDataSource();

		builder.Services.AddRazorComponents(options =>
			options.DetailedErrors = builder.Environment.IsDevelopment());

		var app = builder.Build();

		// Configure the HTTP request pipeline.
		if (!app.Environment.IsDevelopment())
		{
			app.UseExceptionHandler("/Error");
			// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
			app.UseHsts();
		}

		app.UseHttpsRedirection();
		app.UseStatusCodePagesWithRedirects("/{0}");

		app.UseStaticFiles();
		app.UseAntiforgery();

		app.MapRazorComponents<App>()
			.AddInteractiveServerRenderMode();

		app.Run();
	}

	/// <summary>
	/// Add any required services for UI events. 
	/// </summary>
	/// <param name="builder"></param>
	private static void AddPageServices(WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<UpApiService>();
		builder.Services.AddScoped<FrontPageService>();
	}
}