namespace Stemkit.DTOs.Product
{
    public class ReadProductDto
    {
        public int ProductID { get; set; }

        public string ProductName { get; set; } = null!;

        public string Description { get; set; } = null!;

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public string? Ages { get; set; }

        public int SupportInstances { get; set; }

        public int LabID { get; set; }

        public string LabName { get; set; } = null!;

        public int SubcategoryID { get; set; }

        public string SubcategoryName { get; set; } = null!;
    }
}
