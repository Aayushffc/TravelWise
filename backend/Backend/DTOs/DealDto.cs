namespace Backend.DTOs
{
    public class DealCreateDto
    {
        public string? Title { get; set; }
        public int LocationId { get; set; }
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

    public class DealUpdateDto : DealCreateDto { }

    public class DealResponseDto : DealCreateDto
    {
        public int Id { get; set; }
        public LocationResponseDto? Location { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
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
