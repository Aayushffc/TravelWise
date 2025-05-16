using System;
using System.Threading.Tasks;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
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
    }
}
