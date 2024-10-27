using System.ComponentModel.DataAnnotations;

namespace Stemkit.DTOs.User
{
    public class UserQueryParameters
    {
        private const int maxPageSize = 100;
        private int _pageSize = 10;

        /// <summary>
        /// The page number to retrieve.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Page number must be at least 1.")]
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// The number of items per page.
        /// </summary>
        [Range(1, maxPageSize, ErrorMessage = "Page size must be between 1 and 100.")]
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > maxPageSize) ? maxPageSize : value;
        }
    }
}
