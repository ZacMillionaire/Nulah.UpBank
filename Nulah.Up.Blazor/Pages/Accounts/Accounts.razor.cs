using System.Text.Json;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Nulah.Up.Blazor.Components.Dialogs;
using Nulah.Up.Blazor.Pages.Accounts.Dialogs;
using Nulah.Up.Blazor.Services;
using Nulah.UpApi.Domain.Models;

namespace Nulah.Up.Blazor.Pages.Accounts;

public partial class Accounts : IDisposable
{
	[Inject]
	private UpApiService UpBankApi { get; set; } = null!;

	[Inject]
	private IDialogService DialogService { get; set; } = null!;

	private IReadOnlyList<UpAccount> LoadedAccounts { get; set; } = new List<UpAccount>();
	private bool _isLoading = true;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			// This page _always_ loads accounts explicitly, so does not use the events exposed by the service.
			// You can think of this page as the source of any events that would trigger accounts to be loaded/cache updated
			await LoadAccounts();
		}

		await base.OnAfterRenderAsync(firstRender);
	}

	private async void RecacheAccounts() => await LoadAccounts(true);
	private void DisplayRawData(UpAccount account) => DisplayAccountRaw(account);

	private async Task LoadAccounts(bool updateCache = false)
	{
		_isLoading = true;
		try
		{
			LoadedAccounts = await UpBankApi.Accounts.GetAccounts(updateCache);
		}
		catch (Exception ex)
		{
			var options = new DialogOptions { CloseOnEscapeKey = true };

			var parameters = new DialogParameters<ErrorDialog>();
			parameters.Add(x => x.ContentText, ex.Message);

			await DialogService.ShowAsync<ErrorDialog>("Error when loading accounts", parameters, options);
		}
		finally
		{
			_isLoading = false;
			StateHasChanged();
		}
	}

	private void DisplayAccountRaw(UpAccount account)
	{
		// TODO: update this to pull the raw response from the UpApi
		// TODO: change the parameter to the accountId
		// TODO: expose the api to get the raw account in the upbankservice
		var options = new DialogOptions { CloseOnEscapeKey = true };

		var parameters = new DialogParameters<ErrorDialog>();
		parameters.Add(x => x.ContentText, JsonSerializer.Serialize(account, new JsonSerializerOptions()
		{
			WriteIndented = true
		}));

		DialogService.Show<DebugDialog>("Account raw data", parameters, options);
	}

	public void Dispose()
	{
	}
}