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
        Task<PaymentResponseDTO> ConfirmPayment(string paymentIntentId, string paymentMethodId = null);
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
                var agencyStripeConnect = await _dbHelper.GetAgencyStripeConnect(booking.AgencyId);
                if (agencyStripeConnect == null || !agencyStripeConnect.IsEnabled)
                {
                    throw new Exception("Agency not found or not connected to Stripe");
                }

                // Calculate commission amount
                var commissionPercentage = dto.CommissionPercentage ?? _defaultCommissionPercentage;
                var commissionAmount = (dto.Amount * commissionPercentage) / 100;
                var transferAmount = dto.Amount - commissionAmount;

                // Create or get customer
                var stripeCustomerId = await GetOrCreateCustomer(dto.CustomerEmail, dto.CustomerName);

                // Check if payment intent already exists for this booking
                var existingPayment = await _dbHelper.GetPaymentByBookingId(dto.BookingId);
                if (existingPayment != null)
                {
                    // If payment exists but failed, create a new one
                    if (existingPayment.Status == "failed" || existingPayment.Status == "canceled")
                    {
                        // Create new payment intent
                        var newOptions = new PaymentIntentCreateOptions
                        {
                            Amount = (long)(dto.Amount * 100), // Convert to cents
                            Currency = dto.Currency.ToLower(),
                            Customer = stripeCustomerId,
                            PaymentMethodTypes = new List<string> { "card" },
                            Description = dto.Description,
                            TransferData = new PaymentIntentTransferDataOptions
                            {
                                Destination = agencyStripeConnect.StripeAccountId,
                                Amount = (long)(transferAmount * 100), // Convert to cents
                            },
                            Metadata = new Dictionary<string, string>
                            {
                                { "bookingId", dto.BookingId.ToString() },
                                { "agencyId", booking.AgencyId },
                                { "commissionPercentage", commissionPercentage.ToString() },
                                { "commissionAmount", commissionAmount.ToString() },
                            },
                        };

                        var newService = new PaymentIntentService();
                        var newIntent = await newService.CreateAsync(newOptions);

                        // Update existing payment record with new intent
                        await _dbHelper.UpdatePaymentWithNewIntent(
                            existingPayment.Id,
                            newIntent.Id,
                            newIntent.Status,
                            stripeCustomerId
                        );

                        return new PaymentIntentResponseDTO
                        {
                            ClientSecret = newIntent.ClientSecret,
                            PaymentIntentId = newIntent.Id,
                            Amount = dto.Amount,
                            Currency = dto.Currency,
                            Status = newIntent.Status,
                        };
                    }
                    else
                    {
                        // Return existing payment intent
                        var existingService = new PaymentIntentService();
                        var existingIntent = await existingService.GetAsync(existingPayment.StripePaymentId);

                        return new PaymentIntentResponseDTO
                        {
                            ClientSecret = existingIntent.ClientSecret,
                            PaymentIntentId = existingPayment.StripePaymentId,
                            Amount = existingPayment.Amount,
                            Currency = existingPayment.Currency,
                            Status = existingPayment.Status,
                        };
                    }
                }

                // Create payment intent
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(dto.Amount * 100), // Convert to cents
                    Currency = dto.Currency.ToLower(),
                    Customer = stripeCustomerId,
                    Description = dto.Description,
                    TransferData = new PaymentIntentTransferDataOptions
                    {
                        Destination = agencyStripeConnect.StripeAccountId,
                        Amount = (long)(transferAmount * 100), // Convert to cents
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        { "bookingId", dto.BookingId.ToString() },
                        { "agencyId", booking.AgencyId },
                        { "commissionPercentage", commissionPercentage.ToString() },
                        { "commissionAmount", commissionAmount.ToString() },
                    },
                    SetupFutureUsage = "off_session", // Allow future payments without requiring customer action
                    Confirm = false, // Don't confirm immediately
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                        AllowRedirects = "never"
                    }
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
                    booking.UserId,  // Application user ID
                    stripeCustomerId,  // Stripe customer ID
                    dto.CustomerEmail ?? string.Empty,
                    dto.CustomerName ?? string.Empty,
                    agencyStripeConnect.StripeAccountId ?? string.Empty,
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

        public async Task<PaymentResponseDTO> ConfirmPayment(string paymentIntentId, string paymentMethodId = null)
        {
            try
            {
                var service = new PaymentIntentService();
                var intent = await service.GetAsync(paymentIntentId);

                // Get the payment record from our database
                var paymentRecord = await _dbHelper.GetPaymentByStripeId(paymentIntentId);
                if (paymentRecord == null)
                {
                    throw new Exception("Payment record not found");
                }

                // If the payment is already succeeded, update our database
                if (intent.Status == "succeeded")
                {
                    await _dbHelper.UpdatePaymentStatus(paymentRecord.Id, intent.Status);

                    // Update the payment method if we have one
                    if (!string.IsNullOrEmpty(paymentMethodId))
                    {
                        await _dbHelper.UpdatePaymentWithNewIntent(
                            paymentRecord.Id,
                            paymentIntentId,
                            intent.Status,
                            paymentMethodId
                        );
                    }

                    // Update booking status to Paid
                    await _dbHelper.UpdateBookingStatus(
                        paymentRecord.BookingId,
                        "Paid",
                        "Payment completed successfully",
                        paymentRecord.CustomerId  // Use CustomerId instead of AgencyId
                    );

                    return await _dbHelper.GetPaymentByStripeId(paymentIntentId);
                }

                // If we have a payment method ID and the intent is not succeeded, attach it
                if (!string.IsNullOrEmpty(paymentMethodId) &&
                    (intent.Status == "requires_payment_method" ||
                     intent.Status == "requires_confirmation" ||
                     intent.Status == "requires_action"))
                {
                    var options = new PaymentIntentUpdateOptions
                    {
                        PaymentMethod = paymentMethodId
                    };
                    intent = await service.UpdateAsync(paymentIntentId, options);
                }

                // Confirm the payment intent if it's not already confirmed
                if (intent.Status != "succeeded")
                {
                    var confirmOptions = new PaymentIntentConfirmOptions();
                    intent = await service.ConfirmAsync(paymentIntentId, confirmOptions);
                }

                // Update payment status and method in database
                await _dbHelper.UpdatePaymentStatus(paymentRecord.Id, intent.Status);
                if (!string.IsNullOrEmpty(paymentMethodId))
                {
                    await _dbHelper.UpdatePaymentWithNewIntent(
                        paymentRecord.Id,
                        paymentIntentId,
                        intent.Status,
                        paymentMethodId
                    );
                }

                // If payment succeeded, update booking status
                if (intent.Status == "succeeded")
                {
                    await _dbHelper.UpdateBookingStatus(
                        paymentRecord.BookingId,
                        "Paid",
                        "Payment completed successfully",
                        paymentRecord.CustomerId  // Use CustomerId instead of AgencyId
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
