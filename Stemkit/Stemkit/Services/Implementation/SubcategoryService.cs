using AutoMapper;
using Stemkit.Data;
using Stemkit.DTOs.Subcategory;
using Stemkit.Models;
using Stemkit.Services.Interfaces;

namespace Stemkit.Services.Implementation
{
    public class SubcategoryService : ISubcategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SubcategoryService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        /// <summary>
        /// Retrieves all subcategories from the database.
        /// </summary>
        /// <returns>List of subcategories.</returns>
        public async Task<IEnumerable<ReadSubcategoryDto>> GetAllSubcategoriesAsync()
        {
            var subcategories = await _unitOfWork.GetRepository<Subcategory>().GetAllAsync();
            return _mapper.Map<IEnumerable<ReadSubcategoryDto>>(subcategories);
        }
    }
}
