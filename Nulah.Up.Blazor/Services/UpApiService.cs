using Marten;
using Nulah.UpApi.Lib;
using Nulah.UpApi.Lib.Controllers;
using Nulah.UpApi.Lib.Models;

namespace Nulah.Up.Blazor.Services;

public class UpApiService
{
	internal readonly AccountController Accounts;
	internal readonly TransactionController Transactions;
	internal readonly CategoryController Categories;

	/// <summary>
	/// Called whenever accounts have started to be loaded. The sender will be the current <see cref="AccountController"/>.
	/// </summary>
	public event EventHandler? AccountsUpdating;

	/// <summary>
	/// Called when accounts have been loaded, containing the list of all accounts available. The sender will be the current <see cref="AccountController"/>.
	/// </summary>
	public event EventHandler<IReadOnlyList<UpAccount>>? AccountsUpdated;

	/// <summary>
	/// Called whenever transactions have started to be cached. The sender will be the current <see cref="TransactionController"/>.
	/// <para>
	/// Once this event has been raised, <see cref="TransactionCacheMessageHandler"/> will start emitting messages.
	/// </para>
	/// </summary>
	public event EventHandler? TransactionCacheStarted;

	/// <summary>
	/// Called when transactions have finished being cached. The sender will be the current <see cref="TransactionController"/>.
	/// </summary>
	public event EventHandler? TransactionCacheFinished;

	/// <summary>
	/// A message returned during a transaction cache. This is an html string and should be rendered. The sender will be the current <see cref="TransactionController"/>.
	/// </summary>
	public event EventHandler<string>? TransactionCacheMessage;

	/// <summary>
	/// Not yet used - will be called when categories are being cached. The sender will be the current <see cref="CategoryController"/>.
	/// </summary>
	public event EventHandler? CategoriesUpdating;

	/// <summary>
	/// Not yet used - will be called when categories have been cached, with a list of all categories available. The sender will be the current <see cref="CategoryController"/>.
	/// </summary>
	public event EventHandler<IReadOnlyList<UpCategory>>? CategoriesUpdated;

	public UpApiService(AccountController accountController,
		TransactionController transactionController,
		CategoryController categoryController)
	{
		Accounts = accountController;
		Transactions = transactionController;
		Categories = categoryController;

		accountController.AccountsUpdating = AccountsUpdatingHandler;
		accountController.AccountsUpdated = AccountsUpdatedHandler;

		transactionController.TransactionCacheStarted = TransactionCacheStartedHandler;
		transactionController.TransactionCacheFinished = TransactionCacheFinishedHandler;
		transactionController.TransactionCacheMessage = TransactionCacheMessageHandler;

		categoryController.CategoriesUpdating = CategoriesUpdatingHandler;
		categoryController.CategoriesUpdated = CategoriesUpdatedHandler;
	}

	private void AccountsUpdatedHandler(AccountController controller, IReadOnlyList<UpAccount> accounts)
	{
		AccountsUpdated?.Invoke(controller, accounts);
	}

	private void AccountsUpdatingHandler(AccountController controller, EventArgs eventArgs)
	{
		AccountsUpdating?.Invoke(controller, eventArgs);
	}

	private void TransactionCacheStartedHandler(TransactionController transactionController, EventArgs eventArgs)
	{
		TransactionCacheStarted?.Invoke(transactionController, eventArgs);
	}

	private void TransactionCacheFinishedHandler(TransactionController transactionController, EventArgs eventArgs)
	{
		TransactionCacheFinished?.Invoke(transactionController, eventArgs);
	}

	private void TransactionCacheMessageHandler(TransactionController transactionController, string cacheMessage)
	{
		TransactionCacheMessage?.Invoke(transactionController, cacheMessage);
	}

	private void CategoriesUpdatingHandler(CategoryController categroyController, EventArgs eventArgs)
	{
		CategoriesUpdating?.Invoke(categroyController, eventArgs);
	}

	private void CategoriesUpdatedHandler(CategoryController categroyController, IReadOnlyList<UpCategory> categories)
	{
		CategoriesUpdated?.Invoke(categroyController, categories);
	}
}