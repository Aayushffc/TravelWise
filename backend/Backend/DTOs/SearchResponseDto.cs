using Backend.Models.Product;

namespace Backend.DTOs
{
    public class SearchResponseDto
    {
        public List<DealResponseDto> Deals { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}
