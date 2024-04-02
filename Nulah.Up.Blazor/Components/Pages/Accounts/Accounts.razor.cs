using System.Text.Json;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Nulah.Up.Blazor.Components.Dialogs;
using Nulah.Up.Blazor.Components.Pages.Accounts.Dialogs;
using Nulah.Up.Blazor.Services;
using Nulah.UpApi.Lib.Models.Accounts;

namespace Nulah.Up.Blazor.Components.Pages.Accounts;

public partial class Accounts
{
	[Inject]
	private UpApiService _upBankApi { get; set; }

	[Inject]
	private IDialogService DialogService { get; set; }

	private IReadOnlyList<Account> LoadedAccounts { get; set; } = new List<Account>();
	private bool IsLoading = true;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await LoadAccounts();
		}

		await base.OnAfterRenderAsync(firstRender);
	}

	private async void RefreshAccounts() => await LoadAccounts();
	private void DisplayRawData(Account account) => DisplayAccountRaw(account);

	private async Task LoadAccounts()
	{
		IsLoading = true;
		try
		{
			LoadedAccounts = await _upBankApi.GetAccounts();
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
			IsLoading = false;
			StateHasChanged();
		}
	}

	private void DisplayAccountRaw(Account account)
	{
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