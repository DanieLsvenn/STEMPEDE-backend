using System.ComponentModel.DataAnnotations;

namespace Stemkit.DTOs.Product
{
    public class ProductQueryParameters
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 10;

        [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0.")]
        public int PageNumber { get; set; } = 1;

        [Range(1, MaxPageSize, ErrorMessage = "PageSize must be between 1 and 100.")]
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }

        public string? SortBy { get; set; }
        public string? SortDirection { get; set; } // "asc" or "desc"

        // Filtering parameters
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? ProductName { get; set; }
        public string? SubcategoryName { get; set; }
        public string? LabName { get; set; }
        public string? Ages { get; set; }
    }
}
