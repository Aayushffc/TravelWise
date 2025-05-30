using System;
using System.Text.Json;
using Backend.DTOs;
using Backend.Helper;
using Backend.Models.Auth;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace Backend.Services
{
    public class StripeConnectService : IStripeConnectService
    {
        private readonly IDBHelper _dbHelper;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripeConnectService> _logger;
        private readonly string _stripeSecretKey;
        private readonly string _stripeClientId;
        private readonly string _stripeRedirectUri;

        public StripeConnectService(
            IDBHelper dbHelper,
            IConfiguration configuration,
            ILogger<StripeConnectService> logger
        )
        {
            _dbHelper = dbHelper;
            _configuration = configuration;
            _logger = logger;
            _stripeSecretKey = _configuration["Stripe:SecretKey"];
            _stripeClientId = _configuration["Stripe:ClientId"];
            _stripeRedirectUri = _configuration["Stripe:ConnectRedirectUri"];
            StripeConfiguration.ApiKey = _stripeSecretKey;
        }

        public async Task<StripeConnectStatusDTO> GetConnectStatus(string userId)
        {
            try
            {
                var agencyStripe = await _dbHelper.GetAgencyStripeConnect(userId);
                if (agencyStripe == null)
                {
                    return new StripeConnectStatusDTO { IsConnected = false };
                }

                var accountService = new AccountService();
                var account = await accountService.GetAsync(agencyStripe.StripeAccountId);

                return new StripeConnectStatusDTO
                {
                    IsConnected = true,
                    StripeAccountId = account.Id,
                    AccountStatus = account.BusinessProfile?.Name ?? "Unknown",
                    IsEnabled = account.ChargesEnabled && account.PayoutsEnabled,
                    PayoutsEnabled = account.PayoutsEnabled.ToString(),
                    ChargesEnabled = account.ChargesEnabled.ToString(),
                    DetailsSubmitted = account.DetailsSubmitted.ToString(),
                    Requirements = JsonSerializer.Serialize(account.Requirements),
                    VerificationStatus =
                        account.Requirements?.CurrentlyDue?.FirstOrDefault() ?? "None",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Stripe Connect status");
                return new StripeConnectStatusDTO
                {
                    IsConnected = false,
                    ErrorMessage = ex.Message,
                };
            }
        }

        public async Task<StripeConnectAccountDTO> CreateConnectAccount(string userId)
        {
            try
            {
                var options = new AccountCreateOptions
                {
                    Type = "express",
                    BusinessType = "company",
                    BusinessProfile = new AccountBusinessProfileOptions
                    {
                        Mcc = "4722", // Travel agencies
                        Url = "https://travelwise.mechlintech.com", // Replace with your actual URL
                    },
                    Capabilities = new AccountCapabilitiesOptions
                    {
                        CardPayments = new AccountCapabilitiesCardPaymentsOptions
                        {
                            Requested = true,
                        },
                        Transfers = new AccountCapabilitiesTransfersOptions { Requested = true },
                    },
                    Settings = new AccountSettingsOptions
                    {
                        Payouts = new AccountSettingsPayoutsOptions
                        {
                            Schedule = new AccountSettingsPayoutsScheduleOptions
                            {
                                Interval = "manual", // You can change this to "daily", "weekly", or "monthly"
                            },
                        },
                    },
                };

                var service = new AccountService();
                var account = await service.CreateAsync(options);

                // Fetch the complete account information
                var accountDetails = await service.GetAsync(account.Id);

                // Update the database with complete account information
                await _dbHelper.CreateAgencyStripeConnect(
                    userId,
                    account.Id,
                    account.BusinessProfile?.Name ?? "Unknown",
                    account.ChargesEnabled && account.PayoutsEnabled,
                    account.PayoutsEnabled.ToString(),
                    account.ChargesEnabled.ToString(),
                    account.DetailsSubmitted.ToString(),
                    JsonSerializer.Serialize(account.Requirements),
                    JsonSerializer.Serialize(account.Capabilities),
                    account.BusinessType,
                    JsonSerializer.Serialize(account.BusinessProfile),
                    JsonSerializer.Serialize(account.ExternalAccounts),
                    account.Requirements?.CurrentlyDue?.FirstOrDefault() ?? "None"
                );

                // Create an account link for onboarding after saving to database
                var accountLink = await CreateAccountLink(userId);

                return new StripeConnectAccountDTO
                {
                    AccountId = account.Id,
                    AccountLink = accountLink.Url,
                    Status = account.BusinessProfile?.Name ?? "Unknown",
                    IsEnabled = account.ChargesEnabled && account.PayoutsEnabled,
                    PayoutsEnabled = account.PayoutsEnabled,
                    ChargesEnabled = account.ChargesEnabled,
                    DetailsSubmitted = account.DetailsSubmitted,
                    Requirements = JsonSerializer.Serialize(account.Requirements),
                    Capabilities = JsonSerializer.Serialize(account.Capabilities),
                    BusinessType = account.BusinessType,
                    BusinessProfile = JsonSerializer.Serialize(account.BusinessProfile),
                    ExternalAccounts = JsonSerializer.Serialize(account.ExternalAccounts),
                    VerificationStatus =
                        account.Requirements?.CurrentlyDue?.FirstOrDefault() ?? "None",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe Connect account");
                throw;
            }
        }

        public async Task<StripeAccountLinkDTO> CreateAccountLink(string userId)
        {
            try
            {
                var agencyStripe = await _dbHelper.GetAgencyStripeConnect(userId);
                if (agencyStripe == null)
                {
                    throw new Exception("Agency not connected to Stripe");
                }

                var options = new AccountLinkCreateOptions
                {
                    Account = agencyStripe.StripeAccountId,
                    RefreshUrl = _stripeRedirectUri,
                    ReturnUrl = _stripeRedirectUri,
                    Type = "account_onboarding",
                    Collect = "eventually_due",
                };

                var service = new AccountLinkService();
                var accountLink = await service.CreateAsync(options);

                return new StripeAccountLinkDTO
                {
                    Url = accountLink.Url,
                    ExpiresAt = ((DateTimeOffset)accountLink.ExpiresAt).ToUnixTimeSeconds(),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe account link");
                throw;
            }
        }

        public async Task UpdateConnectAccountStatus(string stripeAccountId)
        {
            try
            {
                var service = new AccountService();
                var account = await service.GetAsync(stripeAccountId);

                await _dbHelper.UpdateAgencyStripeConnect(
                    stripeAccountId,
                    account.BusinessProfile?.Name ?? "Unknown",
                    account.ChargesEnabled && account.PayoutsEnabled,
                    account.PayoutsEnabled.ToString(),
                    account.ChargesEnabled.ToString(),
                    account.DetailsSubmitted.ToString(),
                    JsonSerializer.Serialize(account.Requirements),
                    account.Requirements?.CurrentlyDue?.FirstOrDefault() ?? "None"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Stripe Connect account status");
                throw;
            }
        }
    }
}
