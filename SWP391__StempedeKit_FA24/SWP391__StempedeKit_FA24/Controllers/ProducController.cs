using Microsoft.AspNetCore.Mvc;

namespace SWP391__StempedeKit_FA24.Controllers{
    [ApiController]
    [Route("[controller]")]
    public class ProductController: ControllerBase
    private 

     [HttpGet(Name = "GetProductDetails")]
        public IEnumerable<Product> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
}
