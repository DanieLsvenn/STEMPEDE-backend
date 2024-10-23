using System.ComponentModel.DataAnnotations;

namespace Stemkit.DTOs.Product
{
    public class ProductQueryParameters
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 10;

        /// <summary>
        /// The page number to retrieve. Must be greater than 0.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0.")]
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// The number of items per page. Maximum allowed is 100.
        /// </summary>
        [Range(1, MaxPageSize, ErrorMessage = "PageSize must be between 1 and 100.")]
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }

        ///// <summary>
        ///// The field by which to sort the products (e.g., "price", "name").
        ///// </summary>
        //public string? SortBy { get; set; }

        ///// <summary>
        ///// The direction of sorting: "asc" for ascending or "desc" for descending.
        ///// </summary>
        //public string? SortDirection { get; set; } // "asc" or "desc"

        //// Filtering parameters
        ///// <summary>
        ///// The minimum price to filter products.
        ///// </summary>
        //public decimal? MinPrice { get; set; }

        ///// <summary>
        ///// The maximum price to filter products.
        ///// </summary>
        //public decimal? MaxPrice { get; set; }

        ///// <summary>
        ///// The name of the product to filter by. Supports partial matches.
        ///// </summary>
        //public string? ProductName { get; set; }
        //public string? SubcategoryName { get; set; }
        //public string? LabName { get; set; }
        //public string? Ages { get; set; }
    }
}
