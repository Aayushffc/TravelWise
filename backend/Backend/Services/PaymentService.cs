using System;
using System.Threading.Tasks;
using Backend.DTOs;
using Backend.Helper;
using Backend.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Backend.Services
{
    public interface IPaymentService
    {
        Task<PaymentIntentResponseDTO> CreatePaymentIntent(CreatePaymentIntentDTO dto);
        Task<PaymentResponseDTO> ConfirmPayment(string paymentIntentId);
        Task<PaymentResponseDTO> GetPayment(string paymentIntentId);
        Task<bool> RefundPayment(int paymentId, string reason);
    }

    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentService> _logger;
        private readonly DBHelper _dbHelper;
        private readonly StripeClient _stripeClient;

        public PaymentService(
            IConfiguration configuration,
            ILogger<PaymentService> logger,
            DBHelper dbHelper
        )
        {
            _configuration = configuration;
            _logger = logger;
            _dbHelper = dbHelper;

            var stripeSecretKey = _configuration["Stripe:SecretKey"];
            if (string.IsNullOrEmpty(stripeSecretKey))
            {
                throw new ArgumentException("Stripe secret key is not configured");
            }

            _stripeClient = new StripeClient(stripeSecretKey);
        }

        public async Task<PaymentIntentResponseDTO> CreatePaymentIntent(CreatePaymentIntentDTO dto)
        {
            try
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(dto.Amount * 100), // Convert to cents
                    Currency = dto.Currency.ToLower(),
                    PaymentMethodTypes = new List<string> { "card" },
                    Customer =
                        dto.CustomerEmail != null
                            ? await GetOrCreateCustomer(dto.CustomerEmail, dto.CustomerName)
                            : null,
                    Metadata = new Dictionary<string, string>
                    {
                        { "bookingId", dto.BookingId.ToString() },
                    },
                };

                var service = new PaymentIntentService(_stripeClient);
                var intent = await service.CreateAsync(options);

                return new PaymentIntentResponseDTO
                {
                    ClientSecret = intent.ClientSecret,
                    PaymentIntentId = intent.Id,
                    Amount = dto.Amount,
                    Currency = dto.Currency,
                    Status = intent.Status,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment intent");
                throw;
            }
        }

        public async Task<PaymentResponseDTO> ConfirmPayment(string paymentIntentId)
        {
            try
            {
                var service = new PaymentIntentService(_stripeClient);
                var intent = await service.GetAsync(paymentIntentId);

                if (intent.Status != "succeeded")
                {
                    throw new Exception(
                        $"Payment intent status is {intent.Status}, expected succeeded"
                    );
                }

                var bookingId = int.Parse(intent.Metadata["bookingId"]);
                var payment = await _dbHelper.GetPaymentByStripeId(paymentIntentId);

                if (payment == null)
                {
                    // Create new payment record
                    payment = await _dbHelper.CreatePayment(
                        paymentIntentId,
                        bookingId,
                        (decimal)intent.Amount / 100, // Convert from cents
                        intent.Currency.ToUpper(),
                        intent.Status,
                        intent.PaymentMethodTypes.FirstOrDefault(),
                        intent.CustomerId,
                        intent.Customer?.Email,
                        intent.Customer?.Name
                    );
                }
                else
                {
                    // Update existing payment record
                    await _dbHelper.UpdatePaymentStatus(payment.Id, intent.Status);
                }

                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment");
                throw;
            }
        }

        public async Task<PaymentResponseDTO> GetPayment(string paymentIntentId)
        {
            try
            {
                var payment = await _dbHelper.GetPaymentByStripeId(paymentIntentId);
                if (payment != null)
                {
                    return payment;
                }

                // If not found in database, fetch from Stripe
                var service = new PaymentIntentService(_stripeClient);
                var intent = await service.GetAsync(paymentIntentId);

                var bookingId = int.Parse(intent.Metadata["bookingId"]);
                return await _dbHelper.CreatePayment(
                    paymentIntentId,
                    bookingId,
                    (decimal)intent.Amount / 100,
                    intent.Currency.ToUpper(),
                    intent.Status,
                    intent.PaymentMethodTypes.FirstOrDefault(),
                    intent.CustomerId,
                    intent.Customer?.Email,
                    intent.Customer?.Name
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment");
                throw;
            }
        }

        public async Task<bool> RefundPayment(int paymentId, string reason)
        {
            try
            {
                var payment = await _dbHelper.GetPaymentById(paymentId);
                if (payment == null)
                {
                    throw new Exception($"Payment with ID {paymentId} not found");
                }

                var service = new RefundService(_stripeClient);
                var options = new RefundCreateOptions
                {
                    PaymentIntent = payment.StripePaymentId,
                    Reason = reason != null ? RefundReasons.RequestedByCustomer : null,
                };

                await service.CreateAsync(options);
                return await _dbHelper.RefundPayment(paymentId, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding payment");
                throw;
            }
        }

        private async Task<string> GetOrCreateCustomer(string email, string name)
        {
            try
            {
                var service = new CustomerService(_stripeClient);
                var options = new CustomerListOptions { Email = email, Limit = 1 };

                var customers = await service.ListAsync(options);
                if (customers.Data.Any())
                {
                    return customers.Data.First().Id;
                }

                var createOptions = new CustomerCreateOptions { Email = email, Name = name };

                var customer = await service.CreateAsync(createOptions);
                return customer.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating customer");
                throw;
            }
        }
    }
}
