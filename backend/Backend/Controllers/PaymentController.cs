using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Backend.DTOs;
using Backend.Helper;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Stripe;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Backend.Models.Auth;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;
        private readonly IDBHelper _dbHelper;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentController(
            IPaymentService paymentService,
            ILogger<PaymentController> logger,
            IDBHelper dbHelper,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager
        )
        {
            _paymentService = paymentService;
            _logger = logger;
            _dbHelper = dbHelper;
            _configuration = configuration;
            _userManager = userManager;
        }

        [HttpPost("create-intent")]
        public async Task<ActionResult<PaymentIntentResponseDTO>> CreatePaymentIntent(
            [FromBody] CreatePaymentIntentDTO dto
        )
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Set the agency ID from the authenticated user
                dto.AgencyId = userId;

                var response = await _paymentService.CreatePaymentIntent(dto);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment intent");
                return StatusCode(
                    500,
                    new { message = "Error creating payment intent", error = ex.Message }
                );
            }
        }

        [HttpPost("confirm/{paymentIntentId}")]
        public async Task<ActionResult<PaymentResponseDTO>> ConfirmPayment(
            string paymentIntentId,
            [FromBody] ConfirmPaymentDTO dto
        )
        {
            try
            {
                if (string.IsNullOrEmpty(dto.PaymentMethodId))
                {
                    return BadRequest(new { message = "Payment method ID is required" });
                }

                var payment = await _paymentService.ConfirmPayment(paymentIntentId, dto.PaymentMethodId);
                if (payment == null)
                {
                    return NotFound(new { message = "Payment not found" });
                }
                return Ok(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment");
                return StatusCode(
                    500,
                    new { message = "Error confirming payment", error = ex.Message }
                );
            }
        }

        [HttpGet("{paymentIntentId}")]
        public async Task<ActionResult<PaymentResponseDTO>> GetPayment(string paymentIntentId)
        {
            try
            {
                var payment = await _paymentService.GetPayment(paymentIntentId);
                if (payment == null)
                {
                    return NotFound(new { message = "Payment not found" });
                }
                return Ok(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment");
                return StatusCode(
                    500,
                    new { message = "Error getting payment", error = ex.Message }
                );
            }
        }

        [HttpPost("refund/{paymentId}")]
        [Authorize(Roles = "Admin,Agency")]
        public async Task<ActionResult> RefundPayment(
            int paymentId,
            [FromBody] RefundPaymentDTO dto
        )
        {
            try
            {
                var success = await _paymentService.RefundPayment(paymentId, dto.Reason);
                if (!success)
                {
                    return NotFound(new { message = "Payment not found" });
                }
                return Ok(new { message = "Payment refunded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding payment");
                return StatusCode(
                    500,
                    new { message = "Error refunding payment", error = ex.Message }
                );
            }
        }

        [HttpPost("request")]
        [Authorize(Roles = "Agency")]
        public async Task<ActionResult<PaymentRequestDTO>> CreatePaymentRequest(
            [FromBody] CreatePaymentRequestDTO dto
        )
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Get booking details
                var booking = await _dbHelper.GetBookingById(dto.BookingId, userId);
                if (booking == null)
                {
                    return NotFound(
                        new { message = "Booking not found or you don't have access to it" }
                    );
                }

                // Create payment request
                var paymentRequest = new PaymentRequestDTO
                {
                    BookingId = dto.BookingId,
                    AgencyId = userId,
                    UserId = booking.UserId,
                    Amount = dto.Amount,
                    Currency = dto.Currency,
                    Status = "Pending",
                    Description = dto.Description,
                    PaymentDeadline = dto.PaymentDeadline,
                    CommissionPercentage = dto.CommissionPercentage,
                    CreatedAt = DateTime.UtcNow,
                };

                // TODO: Save payment request to database
                // TODO: Send notification to user

                return Ok(paymentRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment request");
                return StatusCode(
                    500,
                    new { message = "Error creating payment request", error = ex.Message }
                );
            }
        }

        [HttpGet("requests")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<PaymentResponseDTO>>> GetPaymentRequests()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Get user's role
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Unauthorized();
                }

                var isAgency = await _userManager.IsInRoleAsync(user, "Agency");
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

                // Get payments based on user role
                if (isAgency)
                {
                    // For agencies, get payments where they are the agency
                    var payments = await _dbHelper.GetPaymentsByAgencyId(userId);
                    return Ok(payments);
                }
                else
                {
                    // For regular users, get payments where they are the customer
                    var payments = await _dbHelper.GetPaymentsByCustomerId(userId);
                    return Ok(payments);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment requests");
                return StatusCode(500, "Error getting payment requests");
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

                var webhookSecret = _configuration["Stripe:WebhookSecret"];
                var stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);

                switch (stripeEvent.Type)
                {
                    case "payment_intent.succeeded":
                        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                        await _paymentService.ConfirmPayment(paymentIntent.Id);
                        break;

                    case "payment_intent.payment_failed":
                        var failedPaymentIntent = stripeEvent.Data.Object as PaymentIntent;
                        var payment = await _dbHelper.GetPaymentByStripeId(failedPaymentIntent.Id);
                        if (payment != null)
                        {
                            await _dbHelper.UpdatePaymentStatus(
                                payment.Id,
                                "failed",
                                failedPaymentIntent.LastPaymentError?.Message
                            );
                        }
                        break;

                    case "transfer.created":
                        var transfer = stripeEvent.Data.Object as Transfer;
                        if (transfer != null && transfer.SourceTransaction != null)
                        {
                            // Update payment with transfer details
                            var paymentWithTransfer = await _dbHelper.GetPaymentByStripeId(
                                transfer.SourceTransaction.ToString()
                            );
                            if (paymentWithTransfer != null)
                            {
                                // TODO: Update payment with transfer details
                            }
                        }
                        break;
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling webhook");
                return StatusCode(500, "Error handling webhook");
            }
        }
    }
}
