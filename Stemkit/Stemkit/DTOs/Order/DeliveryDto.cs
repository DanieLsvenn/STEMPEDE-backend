namespace Stemkit.DTOs.Order
{
    public class DeliveryDto
    {
        public int DeliveryID { get; set; }
        public string DeliveryStatus { get; set; }
        public DateOnly? DeliveryDate { get; set; }
    }
}
