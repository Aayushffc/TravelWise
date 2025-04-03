namespace Backend.DTOs
{
    public class LocationCreateDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? Currency { get; set; }
        public bool IsPopular { get; set; }
        public bool IsActive { get; set; }
    }

    public class LocationUpdateDto : LocationCreateDto { }

    public class LocationResponseDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? Country { get; set; }
        public string? Continent { get; set; }
        public string? Currency { get; set; }
        public bool IsPopular { get; set; }
        public bool IsActive { get; set; }
        public int ClickCount { get; set; }
        public int RequestCallCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
