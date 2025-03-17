namespace Backend.DTOs
{
    public class LocationCreateDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsPopular { get; set; }
        public bool IsActive { get; set; }
    }

    public class LocationUpdateDto : LocationCreateDto { }

    public class LocationResponseDto : LocationCreateDto
    {
        public int Id { get; set; }
        public int ClickCount { get; set; }
        public int RequestCallCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
