namespace Backend.DTOs
{
    public class StripeConnectStatusDTO
    {
        public bool IsConnected { get; set; }
        public string? StripeAccountId { get; set; }
        public string? AccountStatus { get; set; }
        public bool IsEnabled { get; set; }
        public string? PayoutsEnabled { get; set; }
        public string? ChargesEnabled { get; set; }
        public string? DetailsSubmitted { get; set; }
        public string? Requirements { get; set; }
        public string? VerificationStatus { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class StripeConnectAccountDTO
    {
        public string AccountId { get; set; }
        public string AccountLink { get; set; }
    }

    public class StripeAccountLinkDTO
    {
        public string Url { get; set; }
        public long ExpiresAt { get; set; }
    }
}
