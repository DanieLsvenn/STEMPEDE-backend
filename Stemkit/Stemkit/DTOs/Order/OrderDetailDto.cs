namespace Stemkit.DTOs.Order
{
    public class OrderDetailDto
    {
        public int OrderDetailID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
