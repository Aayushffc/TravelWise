using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.DTOs;
using Backend.Helper;
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
        private readonly IDBHelper _dbHelper;
        private readonly string _stripeSecretKey;
        private readonly decimal _defaultCommissionPercentage = 10; // 10% default commission

        public PaymentService(
            IConfiguration configuration,
            ILogger<PaymentService> logger,
            IDBHelper dbHelper
        )
        {
            _configuration = configuration;
            _logger = logger;
            _dbHelper = dbHelper;
            _stripeSecretKey = _configuration["Stripe:SecretKey"] ?? string.Empty;
            StripeConfiguration.ApiKey = _stripeSecretKey;
        }

        public async Task<PaymentIntentResponseDTO> CreatePaymentIntent(CreatePaymentIntentDTO dto)
        {
            try
            {
                // Get booking details
                var booking = await _dbHelper.GetBookingById(dto.BookingId, dto.AgencyId);
                if (booking == null)
                {
                    throw new Exception("Booking not found");
                }

                // Get agency's Stripe account ID
                var agencyProfile = await _dbHelper.GetAgencyProfileByUserId(dto.AgencyId);
                if (agencyProfile == null || string.IsNullOrEmpty(agencyProfile.StripeAccountId))
                {
                    throw new Exception("Agency not found or not connected to Stripe");
                }

                // Calculate commission amount
                var commissionPercentage = dto.CommissionPercentage ?? _defaultCommissionPercentage;
                var commissionAmount = (dto.Amount * commissionPercentage) / 100;
                var transferAmount = dto.Amount - commissionAmount;

                // Create or get customer
                var customerId = await GetOrCreateCustomer(dto.CustomerEmail, dto.CustomerName);

                // Create payment intent
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(dto.Amount * 100), // Convert to cents
                    Currency = dto.Currency.ToLower(),
                    Customer = customerId,
                    PaymentMethodTypes = new List<string> { "card" },
                    Description = dto.Description,
                    TransferData = new PaymentIntentTransferDataOptions
                    {
                        Destination = agencyProfile.StripeAccountId,
                        Amount = (long)(transferAmount * 100), // Convert to cents
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        { "bookingId", dto.BookingId.ToString() },
                        { "agencyId", dto.AgencyId },
                        { "commissionPercentage", commissionPercentage.ToString() },
                        { "commissionAmount", commissionAmount.ToString() },
                    },
                };

                var service = new PaymentIntentService();
                var intent = await service.CreateAsync(options);

                // Create payment record
                var payment = await _dbHelper.CreatePayment(
                    intent.Id,
                    dto.BookingId,
                    dto.Amount,
                    dto.Currency,
                    intent.Status,
                    dto.PaymentMethod ?? string.Empty,
                    customerId ?? string.Empty,
                    dto.CustomerEmail ?? string.Empty,
                    dto.CustomerName ?? string.Empty,
                    agencyProfile.StripeAccountId ?? string.Empty,
                    commissionPercentage,
                    dto.Description ?? string.Empty,
                    dto.PaymentDeadline
                );

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
                var service = new PaymentIntentService();
                var intent = await service.GetAsync(paymentIntentId);

                var payment = await _dbHelper.GetPaymentByStripeId(paymentIntentId);
                if (payment == null)
                {
                    throw new Exception("Payment not found");
                }

                // Update payment status
                await _dbHelper.UpdatePaymentStatus(payment.Id, intent.Status);

                // If payment succeeded, update booking status
                if (intent.Status == "succeeded")
                {
                    await _dbHelper.UpdateBookingStatus(
                        payment.BookingId,
                        "Paid",
                        null,
                        payment.AgencyId.ToString()
                    );
                }

                return await _dbHelper.GetPaymentByStripeId(paymentIntentId);
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
                return await _dbHelper.GetPaymentByStripeId(paymentIntentId);
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
                    throw new Exception("Payment not found");
                }

                // Create refund in Stripe
                var refundOptions = new RefundCreateOptions
                {
                    PaymentIntent = payment.StripePaymentId,
                    Reason = reason,
                };

                var service = new RefundService();
                await service.CreateAsync(refundOptions);

                // Update payment status in database
                await _dbHelper.RefundPayment(paymentId, reason);

                // Update booking status
                await _dbHelper.UpdateBookingStatus(
                    payment.BookingId,
                    "Refunded",
                    reason,
                    payment.AgencyId.ToString()
                );

                return true;
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
                var service = new CustomerService();
                var options = new CustomerListOptions { Email = email, Limit = 1 };

                var customers = await service.ListAsync(options);
                if (customers.Data.Count > 0)
                {
                    return customers.Data[0].Id;
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
