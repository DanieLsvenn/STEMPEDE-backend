using Stem.Business.Base;
using Stem.Common;
using Stem.Data;
using Stem.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stem.Business.Business
{
    public interface IProductBusiness
    {
        Task<IBusinessResult> GetAll();
        Task<IBusinessResult> Create(Product product);
        Task<IBusinessResult> Created(
           string productName,
           decimal price,
           int stockQuantity,
           string description,
           string ages,
           int supportInstances,
           string labName,
           string? labSchedule,
           string labUrl);
        Task<IBusinessResult> GetById(int code);
        Task<IBusinessResult> Save(Product product);

        Task<IBusinessResult> Update(Product product);
        Task<IBusinessResult> DeleteById(int code);
    }
    public class ProductBusiness : IProductBusiness
    {
        private readonly UnitOfWork _unitOfWork;
        public ProductBusiness()
        {
            //_DAO = new ExtraDiamondDAO();
            _unitOfWork ??= new UnitOfWork();
        }

        public async Task<IBusinessResult> Created(
           string productName,
           decimal price,
           int stockQuantity,
           string description,
           string ages,
           int supportInstances,
           string labName,
           string? labSchedule,
           string labUrl)
        {
            try
            {
                // Creating the Product entity directly from the passed parameters.
                var product = new Product
                {
                    ProductName = productName,
                    Price = price,
                    StockQuantity = stockQuantity,
                    Description = description,
                    Ages = ages,
                    SupportInstances = supportInstances,
                    LabName = labName,
                    LabSchedule = labSchedule,
                    LabUrl = labUrl
                };

                // Using Generic Repository's CreateAsync method to add the product.
                int result = await _unitOfWork.ProductRepository.CreateAsync(product);

                if (result > 0)
                {
                    return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG);
                }
                else
                {
                    return new BusinessResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
                }
            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }
        public async Task<IBusinessResult> Create(Product product)
        {
            try
            {
                int result = await _unitOfWork.ProductRepository.CreateAsync(product);
                if (result > 0)
                {
                    return new BusinessResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG);
                }
                else
                {
                    return new BusinessResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG);
                }
            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }

        public async Task<IBusinessResult> DeleteById(int code)
        {
            try
            {
                var product = await _unitOfWork.ProductRepository.GetByIdAsync(code);
                if (product != null)
                {
                    var result = await _unitOfWork.ProductRepository.RemoveAsync(product);
                    if (result)
                    {
                        return new BusinessResult(Const.SUCCESS_DELETE_CODE, Const.SUCCESS_DELETE_MSG);
                    }
                    else
                    {
                        return new BusinessResult(Const.FAIL_DELETE_CODE, Const.FAIL_DELETE_MSG);
                    }
                }
                else
                {
                    return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA__MSG);
                }
            }
            catch (Exception ex)
            {
                return new BusinessResult(-4, ex.ToString());
            }
        }

        public async Task<IBusinessResult> GetAll()
        {
            try
            {
                #region Business rule
                #endregion

                //var currencies = _DAO.GetAll();
                var products = await _unitOfWork.ProductRepository.GetAllAsync();

                if (products == null)
                {
                    return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA__MSG);
                }
                else
                {
                    return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, products);
                }
            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }
        public async Task<int> SaveAll()
        {
            return await _unitOfWork.ProductRepository.SaveAsync();
        }
        public async Task<IBusinessResult> GetById(int code)
        {
            try
            {
                #region Business rule
                #endregion

                var product = await _unitOfWork.ProductRepository.GetByIdAsync(code);

                if (product == null)
                {
                    return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA__MSG);
                }
                else
                {
                    return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, product);
                }
            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

        public async Task<IBusinessResult> Save(Product product)
        {
            try
            {
                //int result = await _unitOfWork.ExtraDiamondRepository.CreateAsync(extraDiamond);
                _unitOfWork.ProductRepository.PrepareCreate(product);
                int result = await _unitOfWork.ProductRepository.SaveAsync();
                if (result > 0)
                {
                    return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG);
                }
                else
                {
                    return new BusinessResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
                }
            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }

        public async Task<IBusinessResult> Update(Product product)
        {
            try
            {
                int result = await _unitOfWork.ProductRepository.UpdateAsync(product);
                if (result > 0)
                {
                    return new BusinessResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG);
                }
                else
                {
                    return new BusinessResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG);
                }
            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }
    }
}

