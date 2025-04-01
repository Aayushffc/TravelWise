namespace Backend.DTOs
{
    public class DealCreateDto
    {
        public string? Title { get; set; }
        public int LocationId { get; set; }
        public string? UserId { get; set; }
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
    }

    public class DealUpdateDto : DealCreateDto
    {
        public int? ViewCount { get; set; }
        public DateTime? LastViewed { get; set; }
        public int? ClickCount { get; set; }
        public DateTime? LastClicked { get; set; }
        public decimal? RelevanceScore { get; set; }
    }

    public class DealResponseDto : DealCreateDto
    {
        public int Id { get; set; }
        public LocationResponseDto? Location { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Status { get; set; }
        public string? ApprovalStatus { get; set; }
        public string? SearchKeywords { get; set; }
        public int ClickCount { get; set; }
        public int ViewCount { get; set; }
        public DateTime LastClicked { get; set; }
        public DateTime LastViewed { get; set; }
        public decimal RelevanceScore { get; set; }
        public decimal? AverageRating { get; set; }
        public string? AgencyId { get; set; }
        public string? Tags { get; set; }
        public string? Categories { get; set; }
        public string? Seasons { get; set; }
        public string? DifficultyLevel { get; set; }
        public bool IsInstantBooking { get; set; }
        public bool IsLastMinuteDeal { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
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
