using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stem.Data.DTO
{
    public class CreateProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Description { get; set; }
        public string Ages { get; set; }
        public int SupportInstances { get; set; }
        public string LabName { get; set; }
        public DateOnly? LabSchedule { get; set; }
        public string LabUrl { get; set; }
    }
}
