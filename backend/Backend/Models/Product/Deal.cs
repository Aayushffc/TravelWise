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

        // Search and relevance fields
        public int ClickCount { get; set; } = 0;
        public int ViewCount { get; set; } = 0;
        public int BookingCount { get; set; } = 0;
        public DateTime LastClicked { get; set; }
        public DateTime LastViewed { get; set; }
        public DateTime LastBooked { get; set; }
        public decimal RelevanceScore { get; set; } = 0;
        public string? SearchKeywords { get; set; } // Store processed keywords for faster search
        public bool IsFeatured { get; set; } = false;
        public DateTime FeaturedUntil { get; set; }
        public int Priority { get; set; } = 0; // Higher priority deals appear first
        public string? AgencyId { get; set; }
        public virtual ApplicationUser? Agency { get; set; }
        public string? AgencyName { get; set; } // Denormalized for faster search
        public string? LocationName { get; set; } // Denormalized for faster search
        public string? Region { get; set; }
        public string? Country { get; set; }
        public string? Continent { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Tags { get; set; } // Store as JSON array
        public string? Categories { get; set; } // Store as JSON array
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
        public string? Currency { get; set; } = "INR";
        public decimal? TaxAmount { get; set; }
        public decimal? ServiceCharge { get; set; }
        public string? IncludedServices { get; set; } // Store as JSON array
        public string? ExcludedServices { get; set; } // Store as JSON array
        public string? Requirements { get; set; } // Store as JSON array
        public string? Restrictions { get; set; } // Store as JSON array
        public string? Highlights { get; set; } // Store as JSON array
        public string? Reviews { get; set; } // Store as JSON array
        public int? ReviewCount { get; set; } = 0;
        public decimal? AverageRating { get; set; } = 0;
        public string? Status { get; set; } = "Active"; // Active, Draft, Pending, Suspended, Expired
        public string? ApprovalStatus { get; set; } = "Pending"; // Pending, Approved, Rejected
        public string? RejectionReason { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? SuspendedAt { get; set; }
        public string? SuspendedBy { get; set; }
        public string? SuspensionReason { get; set; }
        public DateTime? ExpiredAt { get; set; }
        public string? ExpiredBy { get; set; }
        public string? ExpirationReason { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string? LastUpdatedBy { get; set; }
        public string? Version { get; set; }
        public string? Metadata { get; set; } // Store as JSON
    }
}
