namespace Backend.DTOs
{
    public class DealCreateDto
    {
        public string? Title { get; set; }
        public int LocationId { get; set; }
        public decimal? Price { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public int? DiscountPercentage { get; set; }
        public decimal? Rating { get; set; }
        public int? DaysCount { get; set; }
        public int? NightsCount { get; set; }
        public string? Description { get; set; }
        public List<string>? Photos { get; set; }
        public bool? ElderlyFriendly { get; set; }
        public bool? InternetIncluded { get; set; }
        public bool? TravelIncluded { get; set; }
        public bool? MealsIncluded { get; set; }
        public bool? SightseeingIncluded { get; set; }
        public bool? StayIncluded { get; set; }
        public bool? AirTransfer { get; set; }
        public bool? RoadTransfer { get; set; }
        public bool? TrainTransfer { get; set; }
        public bool? TravelCostIncluded { get; set; }
        public bool? GuideIncluded { get; set; }
        public bool? PhotographyIncluded { get; set; }
        public bool? InsuranceIncluded { get; set; }
        public bool? VisaIncluded { get; set; }
        public List<ItineraryDay>? Itinerary { get; set; }
        public List<PackageOption>? PackageOptions { get; set; }
        public string? MapUrl { get; set; }
        public List<Policy>? Policies { get; set; }
        public string? PackageType { get; set; }
        public bool IsActive { get; set; }
        public List<string>? Headlines { get; set; }
        public List<string>? Tags { get; set; }
        public List<string>? Seasons { get; set; }
        public string? DifficultyLevel { get; set; }
        public int? MaxGroupSize { get; set; }
        public int? MinGroupSize { get; set; }
        public bool? IsInstantBooking { get; set; }
        public bool? IsLastMinuteDeal { get; set; }
        public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
        public DateTime ValidUntil { get; set; } = DateTime.UtcNow.AddYears(1);
        public string? CancellationPolicy { get; set; }
        public string? RefundPolicy { get; set; }
        public List<string>? Languages { get; set; }
        public List<string>? Requirements { get; set; }
        public List<string>? Restrictions { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? UserId { get; set; }
    }

    public class DealUpdateDto
    {
        public string? Title { get; set; }
        public int? LocationId { get; set; }
        public decimal? Price { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public int? DiscountPercentage { get; set; }
        public decimal? Rating { get; set; }
        public int? DaysCount { get; set; }
        public int? NightsCount { get; set; }
        public string? Description { get; set; }
        public List<string>? Photos { get; set; }

        // Facilities
        public bool? ElderlyFriendly { get; set; }
        public bool? InternetIncluded { get; set; }
        public bool? TravelIncluded { get; set; }
        public bool? MealsIncluded { get; set; }
        public bool? SightseeingIncluded { get; set; }
        public bool? StayIncluded { get; set; }
        public bool? AirTransfer { get; set; }
        public bool? RoadTransfer { get; set; }
        public bool? TrainTransfer { get; set; }
        public bool? TravelCostIncluded { get; set; }
        public bool? GuideIncluded { get; set; }
        public bool? PhotographyIncluded { get; set; }
        public bool? InsuranceIncluded { get; set; }
        public bool? VisaIncluded { get; set; }

        public List<ItineraryDay>? Itinerary { get; set; }
        public List<PackageOption>? PackageOptions { get; set; }
        public string? MapUrl { get; set; }
        public List<Policy>? Policies { get; set; }
        public string? PackageType { get; set; }
        public bool? IsActive { get; set; }
        public List<string>? Headlines { get; set; }
        public List<string>? Tags { get; set; }
        public List<string>? Seasons { get; set; }
        public string? DifficultyLevel { get; set; }
        public int? MaxGroupSize { get; set; }
        public int? MinGroupSize { get; set; }
        public bool? IsInstantBooking { get; set; }
        public bool? IsLastMinuteDeal { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string? CancellationPolicy { get; set; }
        public string? RefundPolicy { get; set; }
        public List<string>? Languages { get; set; }
        public List<string>? Requirements { get; set; }
        public List<string>? Restrictions { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? ClickCount { get; set; }
        public DateTime? LastClicked { get; set; }
        public decimal? RelevanceScore { get; set; }
    }

    public class DealToggleStatusDto
    {
        public bool IsActive { get; set; }
    }

    public class DealResponseDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public int LocationId { get; set; }
        public virtual LocationResponseDto? Location { get; set; }
        public string? UserId { get; set; }
        public decimal Price { get; set; }
        public decimal DiscountedPrice { get; set; }
        public int DiscountPercentage { get; set; }
        public decimal Rating { get; set; }
        public int DaysCount { get; set; }
        public int NightsCount { get; set; }
        public string? Description { get; set; }
        public List<string>? Photos { get; set; }

        // Facilities
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

        public List<ItineraryDay>? Itinerary { get; set; }
        public List<PackageOption>? PackageOptions { get; set; }
        public string? MapUrl { get; set; }
        public List<Policy>? Policies { get; set; }
        public string? PackageType { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int ClickCount { get; set; }
        public int ViewCount { get; set; }
        public int BookingCount { get; set; }
        public DateTime LastClicked { get; set; }
        public DateTime LastViewed { get; set; }
        public DateTime LastBooked { get; set; }
        public decimal RelevanceScore { get; set; }
        public string? SearchKeywords { get; set; }
        public bool IsFeatured { get; set; }
        public DateTime FeaturedUntil { get; set; }
        public int Priority { get; set; }
        public List<string>? Headlines { get; set; }
        public List<string>? Tags { get; set; }
        public List<string>? Seasons { get; set; }
        public string? DifficultyLevel { get; set; }
        public int? MaxGroupSize { get; set; }
        public int? MinGroupSize { get; set; }
        public bool IsInstantBooking { get; set; }
        public bool IsLastMinuteDeal { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string? CancellationPolicy { get; set; }
        public string? RefundPolicy { get; set; }
        public List<string>? Languages { get; set; }
        public List<string>? Requirements { get; set; }
        public List<string>? Restrictions { get; set; }
    }

    public class ItineraryDay
    {
        public int DayNumber { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public List<string>? Activities { get; set; }
    }

    public class PackageOption
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public List<string>? Inclusions { get; set; }
    }

    public class Policy
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
    }
}
