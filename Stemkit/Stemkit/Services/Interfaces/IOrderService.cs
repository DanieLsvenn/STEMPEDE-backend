using Stemkit.DTOs;
using Stemkit.DTOs.Cart;
using Stemkit.DTOs.Order;
using Stemkit.DTOs.Reporting;
using Stemkit.Utils.Implementation;

namespace Stemkit.Services.Interfaces
{
    public interface IOrderService
    {
        Task<ApiResponse<PaginatedList<OrderDto>>> GetAllOrdersAsync(QueryParameters queryParameters);
        Task<ApiResponse<OrderDto>> GetOrderByIdAsync(int orderId, string currentUsername, string userRole);
        Task<ApiResponse<string>> UpdateDeliveryStatusAsync(int orderId, int deliveryId, UpdateDeliveryStatusDto updateDto, string userRole);
        //Task<ApiResponse<SalesReportDto>> GetSalesReportAsync(DateOnly fromDate, DateOnly toDate);
    }
}
