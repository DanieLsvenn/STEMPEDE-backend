using System.ComponentModel.DataAnnotations;

namespace Stemkit.DTOs.Lab
{
    public class LabQueryParameters
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
    }
}
