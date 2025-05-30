public class StripeConnectAccountDTO
{
    public string AccountId { get; set; }
    public string AccountLink { get; set; }
    public string Status { get; set; }
    public bool IsEnabled { get; set; }
    public bool PayoutsEnabled { get; set; }
    public bool ChargesEnabled { get; set; }
    public bool DetailsSubmitted { get; set; }
    public string Requirements { get; set; }
    public string Capabilities { get; set; }
    public string BusinessType { get; set; }
    public string BusinessProfile { get; set; }
    public string ExternalAccounts { get; set; }
    public string VerificationStatus { get; set; }
}