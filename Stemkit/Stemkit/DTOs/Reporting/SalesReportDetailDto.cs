namespace Stemkit.DTOs.Reporting
{
    public class SalesReportDetailDto
    {
        public DateOnly OrderDate { get; set; }
        public int OrderID { get; set; }
        public string CustomerUsername { get; set; }
        public decimal OrderTotal { get; set; }
    }
}
