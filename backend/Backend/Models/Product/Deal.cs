namespace Backend.Models.Product
{
    public class Deal
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public int LocationId { get; set; }
        public virtual Location? Location { get; set; }
        public string UserId { get; set; }
        public decimal Price { get; set; }
        public decimal DiscountedPrice { get; set; }
        public int DiscountPercentage { get; set; }
        public decimal Rating { get; set; }
        public int DaysCount { get; set; }
        public int NightsCount { get; set; }
        public string? StartPoint { get; set; }
        public string? EndPoint { get; set; }
        public string? Duration { get; set; }
        public string? Description { get; set; }

        // Store as JSON array
        public string? Photos { get; set; }

        // Facilities (stored as bit)
        public bool ElderlyFriendly { get; set; }
        public bool InternetIncluded { get; set; }
        public bool TravelIncluded { get; set; }
        public bool MealsIncluded { get; set; }
        public bool SightseeingIncluded { get; set; }
        public bool StayIncluded { get; set; }
        public bool AirTransfer { get; set; }
        public bool RoadTransfer { get; set; }
        public bool TrainTransfer { get; set; }
        public bool TravelCostIncluded { get; set; }
        public bool GuideIncluded { get; set; }
        public bool PhotographyIncluded { get; set; }
        public bool InsuranceIncluded { get; set; }
        public bool VisaIncluded { get; set; }

        // Store as JSON
        public string? Itinerary { get; set; }

        // Store as JSON
        public string? PackageOptions { get; set; }
        public string? MapUrl { get; set; }

        // Store as JSON
        public string? Policies { get; set; }
        public string? PackageType { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
