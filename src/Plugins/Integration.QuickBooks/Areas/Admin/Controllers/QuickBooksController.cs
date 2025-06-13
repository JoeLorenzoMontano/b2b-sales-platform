using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Integration.QuickBooks.Configuration;
using Integration.QuickBooks.Models;
using Integration.QuickBooks.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Integration.QuickBooks.Areas.Admin.Controllers
{
    [AuthorizeAdmin]
    [Area("Admin")]
    [PermissionAuthorize(PermissionSystemName.PaymentMethods)]
    public class QuickBooksController : BasePluginController
    {
        private readonly IQuickBooksService _quickBooksService;
        private readonly IQuickBooksCustomerService _quickBooksCustomerService;
        private readonly IQuickBooksInvoiceService _quickBooksInvoiceService;
        private readonly ITranslationService _translationService;
        private readonly IPermissionService _permissionService;

        public QuickBooksController(
            IQuickBooksService quickBooksService,
            IQuickBooksCustomerService quickBooksCustomerService,
            IQuickBooksInvoiceService quickBooksInvoiceService,
            ITranslationService translationService,
            IPermissionService permissionService)
        {
            _quickBooksService = quickBooksService;
            _quickBooksCustomerService = quickBooksCustomerService;
            _quickBooksInvoiceService = quickBooksInvoiceService;
            _translationService = translationService;
            _permissionService = permissionService;
        }

        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.Authorize(PermissionSystemName.PaymentMethods))
                return AccessDeniedView();

            var settings = _quickBooksService.GetSettings();

            var model = new QuickBooksConfigurationModel
            {
                ClientId = settings.ClientId,
                ClientSecret = settings.ClientSecret,
                RedirectUri = settings.RedirectUri,
                Environment = settings.Environment,
                Enabled = settings.Enabled,
                WebhookToken = settings.WebhookToken,
                IsConnected = _quickBooksService.IsConnected()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(QuickBooksConfigurationModel model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.PaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return View(model);

            var settings = _quickBooksService.GetSettings();

            settings.ClientId = model.ClientId;
            settings.ClientSecret = model.ClientSecret;
            settings.RedirectUri = model.RedirectUri;
            settings.Environment = model.Environment;
            settings.Enabled = model.Enabled;
            settings.WebhookToken = model.WebhookToken;

            await _quickBooksService.SaveSettingsAsync(settings);

            Success(_translationService.GetResource("Admin.Plugins.Saved"));

            return RedirectToAction("Configure");
        }

        public async Task<IActionResult> ConnectToQuickBooks()
        {
            if (!await _permissionService.Authorize(PermissionSystemName.PaymentMethods))
                return AccessDeniedView();

            var authUrl = _quickBooksService.GetAuthorizationUrl();
            return Redirect(authUrl);
        }

        public async Task<IActionResult> Callback(string code, string realmId, string state)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.PaymentMethods))
                return AccessDeniedView();

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(realmId))
            {
                Error("Error connecting to QuickBooks: Invalid response");
                return RedirectToAction("Configure");
            }

            try
            {
                await _quickBooksService.ExchangeAuthorizationCodeAsync(code, realmId);
                Success(_translationService.GetResource("Plugins.Integration.QuickBooks.Connected"));
            }
            catch (Exception ex)
            {
                Error("Error connecting to QuickBooks: " + ex.Message);
            }

            return RedirectToAction("Configure");
        }

        [HttpPost]
        public async Task<IActionResult> Disconnect()
        {
            if (!await _permissionService.Authorize(PermissionSystemName.PaymentMethods))
                return AccessDeniedView();

            try
            {
                await _quickBooksService.DisconnectAsync();
                Success(_translationService.GetResource("Plugins.Integration.QuickBooks.Disconnected"));
            }
            catch (Exception ex)
            {
                Error("Error disconnecting from QuickBooks: " + ex.Message);
            }

            return RedirectToAction("Configure");
        }

        [HttpPost]
        public async Task<IActionResult> SyncCustomers()
        {
            if (!await _permissionService.Authorize(PermissionSystemName.PaymentMethods))
                return AccessDeniedView();

            try
            {
                var count = await _quickBooksCustomerService.SyncCustomersAsync();
                Success($"Successfully synced {count} customers to QuickBooks");
            }
            catch (Exception ex)
            {
                Error("Error syncing customers to QuickBooks: " + ex.Message);
            }

            return RedirectToAction("Configure");
        }

        [HttpPost]
        public async Task<IActionResult> SyncInvoices()
        {
            if (!await _permissionService.Authorize(PermissionSystemName.PaymentMethods))
                return AccessDeniedView();

            try
            {
                var count = await _quickBooksInvoiceService.SyncInvoicesAsync();
                Success($"Successfully synced {count} invoices to QuickBooks");
            }
            catch (Exception ex)
            {
                Error("Error syncing invoices to QuickBooks: " + ex.Message);
            }

            return RedirectToAction("Configure");
        }
    }
}