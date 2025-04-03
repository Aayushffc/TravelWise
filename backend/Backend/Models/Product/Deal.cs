using Backend.Models.Auth;

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
        public string? Description { get; set; }
        public string? Photos { get; set; } // Store as JSON array

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
        public string? PackageOptions { get; set; }
        public string? MapUrl { get; set; }
        public string? Policies { get; set; }
        public string? PackageType { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Search and relevance fields
        public int ClickCount { get; set; } = 0;
        public int ViewCount { get; set; } = 0;
        public int BookingCount { get; set; } = 0;
        public DateTime LastClicked { get; set; }
        public DateTime LastViewed { get; set; }
        public DateTime LastBooked { get; set; }
        public decimal RelevanceScore { get; set; } = 0;
        public string? SearchKeywords { get; set; }
        public bool IsFeatured { get; set; } = false;
        public DateTime FeaturedUntil { get; set; }
        public int Priority { get; set; } = 0;
        public string? Headlines { get; set; } // Store as JSON array
        public string? Tags { get; set; } // Store as JSON array
        public string? Seasons { get; set; } // Store as JSON array
        public string? DifficultyLevel { get; set; }
        public int? MaxGroupSize { get; set; }
        public int? MinGroupSize { get; set; }
        public bool IsInstantBooking { get; set; }
        public bool IsLastMinuteDeal { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string? CancellationPolicy { get; set; }
        public string? RefundPolicy { get; set; }
        public string? Availability { get; set; } // Store as JSON
        public string? Languages { get; set; } // Store as JSON array
        public string? Requirements { get; set; } // Store as JSON array
        public string? Restrictions { get; set; } // Store as JSON array
        public string? Version { get; set; }
        public string? Metadata { get; set; } // Store as JSON
    }
}
