using System.Security.Claims;
using Backend.DTOs;
using Backend.Helper;
using Backend.Models.Auth;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Agency")]
    public class StripeConnectController : ControllerBase
    {
        private readonly IStripeConnectService _stripeConnectService;
        private readonly IDBHelper _dbHelper;
        private readonly ILogger<StripeConnectController> _logger;
        private readonly IConfiguration _configuration;

        public StripeConnectController(
            IStripeConnectService stripeConnectService,
            IDBHelper dbHelper,
            ILogger<StripeConnectController> logger,
            IConfiguration configuration
        )
        {
            _stripeConnectService = stripeConnectService;
            _dbHelper = dbHelper;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet("status")]
        public async Task<ActionResult<StripeConnectStatusDTO>> GetConnectStatus()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var status = await _stripeConnectService.GetConnectStatus(userId);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Stripe Connect status");
                return StatusCode(
                    500,
                    new { message = "Error getting Stripe Connect status", error = ex.Message }
                );
            }
        }

        [HttpPost("create-account")]
        public async Task<ActionResult<StripeConnectAccountDTO>> CreateConnectAccount()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var account = await _stripeConnectService.CreateConnectAccount(userId);
                return Ok(account);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe Connect account");
                return StatusCode(
                    500,
                    new { message = "Error creating Stripe Connect account", error = ex.Message }
                );
            }
        }

        [HttpPost("create-account-link")]
        public async Task<ActionResult<StripeAccountLinkDTO>> CreateAccountLink()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var accountLink = await _stripeConnectService.CreateAccountLink(userId);
                return Ok(accountLink);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe account link");
                return StatusCode(
                    500,
                    new { message = "Error creating Stripe account link", error = ex.Message }
                );
            }
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<ActionResult> HandleWebhook()
        {
            try
            {
                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                var signature = Request.Headers["Stripe-Signature"];

                var webhookSecret = _configuration["Stripe:ConnectWebhookSecret"];
                var stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);

                switch (stripeEvent.Type)
                {
                    case "account.updated":
                        var account = stripeEvent.Data.Object as Account;
                        if (account != null)
                        {
                            await _stripeConnectService.UpdateConnectAccountStatus(account.Id);
                        }
                        break;
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Stripe Connect webhook");
                return StatusCode(500, "Error handling webhook");
            }
        }
    }
}
