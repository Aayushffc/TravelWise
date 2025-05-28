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

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;
        private readonly DBHelper _dbHelper;
        private readonly IConfiguration _configuration;

        public PaymentController(
            IPaymentService paymentService,
            ILogger<PaymentController> logger,
            DBHelper dbHelper,
            IConfiguration configuration
        )
        {
            _paymentService = paymentService;
            _logger = logger;
            _dbHelper = dbHelper;
            _configuration = configuration;
        }

        [HttpPost("create-intent")]
        public async Task<ActionResult<PaymentIntentResponseDTO>> CreatePaymentIntent(
            [FromBody] CreatePaymentIntentDTO dto
        )
        {
            try
            {
                var response = await _paymentService.CreatePaymentIntent(dto);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment intent");
                return StatusCode(500, "Error creating payment intent");
            }
        }

        [HttpPost("confirm/{paymentIntentId}")]
        public async Task<ActionResult<PaymentResponseDTO>> ConfirmPayment(string paymentIntentId)
        {
            try
            {
                var payment = await _paymentService.ConfirmPayment(paymentIntentId);
                return Ok(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment");
                return StatusCode(500, "Error confirming payment");
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
                    return NotFound();
                }
                return Ok(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment");
                return StatusCode(500, "Error getting payment");
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
                    return NotFound();
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding payment");
                return StatusCode(500, "Error refunding payment");
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
                    return Unauthorized();
                }

                // Get booking details
                var booking = await _dbHelper.GetBookingById(dto.BookingId, userId);
                if (booking == null)
                {
                    return NotFound("Booking not found");
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
                return StatusCode(500, "Error creating payment request");
            }
        }

        [HttpGet("requests")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<PaymentRequestDTO>>> GetPaymentRequests()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // TODO: Get payment requests from database
                // Filter based on user role (agency sees their requests, users see requests for them)

                return Ok(new List<PaymentRequestDTO>());
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
