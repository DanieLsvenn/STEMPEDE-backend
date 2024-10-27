using Stemkit.DTOs.Lab;
using Stemkit.Utils.Implementation;

namespace Stemkit.Services.Interfaces
{
    public interface ILabService
    {
        Task<IEnumerable<ReadLabSimpleDto>> GetAllLabsAsync();
        Task<PaginatedList<ReadLabSimpleDto>> GetLabsAsync(LabQueryParameters queryParameters);
        Task<ReadLabSimpleDto> GetLabByIdAsync(int labId);
        Task<ReadLabSimpleDto> CreateLabAsync(CreateLabDto createLabDto);
        Task<bool> UpdateLabAsync(int labId, UpdateLabDto updateLabDto);
        Task<bool> DeleteLabAsync(int labId);
    }
}
