using Application.DTOs;
using Application.DTOs.Order;
using Application.Utils.Implementation;

namespace Application.Services.Interfaces
{
    public interface IOrderService
    {
        Task<ApiResponse<PaginatedList<OrderDto>>> GetAllOrdersAsync(QueryParameters queryParameters);
        Task<ApiResponse<OrderDto>> GetOrderByIdAsync(int orderId, string currentUsername, string userRole);
        Task<ApiResponse<string>> UpdateDeliveryStatusAsync(int orderId, int deliveryId, UpdateDeliveryStatusDto updateDto, string userRole);
        //Task<ApiResponse<SalesReportDto>> GetSalesReportAsync(DateOnly fromDate, DateOnly toDate);
    }
}
