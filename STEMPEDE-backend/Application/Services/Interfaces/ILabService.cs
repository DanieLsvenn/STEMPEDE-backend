using Application.DTOs;
using Application.DTOs.Lab;
using Application.Utils.Implementation;

namespace Application.Services.Interfaces
{
    public interface ILabService
    {
        Task<IEnumerable<ReadLabSimpleDto>> GetAllLabsAsync();
        Task<PaginatedList<ReadLabSimpleDto>> GetLabsAsync(QueryParameters queryParameters);
        Task<ReadLabSimpleDto> GetLabByIdAsync(int labId);
        Task<ReadLabSimpleDto> CreateLabAsync(CreateLabDto createLabDto);
        Task<bool> UpdateLabAsync(int labId, UpdateLabDto updateLabDto);
        Task<bool> DeleteLabAsync(int labId);
    }
}
