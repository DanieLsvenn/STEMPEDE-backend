using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Stemkit.Configurations;
using Stemkit.DTOs.Cart;
using Stemkit.DTOs.Order;
using Stemkit.Services.Interfaces;

namespace Stemkit.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : Controller
    {
        private readonly IProductService _productService;

        public CartController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet("index")] // Explicitly declare as GET method
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("checkout")] // Explicitly declare as GET method
        public IActionResult Checkout()
        {
            return View(GetCheckoutViewModel());
        }

        [HttpPost("checkout")] // Explicitly declare as POST method
        public IActionResult Checkout(CheckOutViewModel request)
        {
            var model = GetCheckoutViewModel();
            var orderDetails = new List<OrderDetailViewModel>();
            foreach (var item in model.CartItems)
            {
                orderDetails.Add(new OrderDetailViewModel()
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                });
            }
            var checkoutRequest = new CheckoutRequest()
            {
                Address = request.CheckoutModel.Address,
                Name = request.CheckoutModel.Name,
                Email = request.CheckoutModel.Email,
                PhoneNumber = request.CheckoutModel.PhoneNumber,
                OrderDetails = orderDetails
            };

            TempData["SuccessMsg"] = "Order purchased successfully";
            return View(model);
        }

        [HttpGet("list-items")] // Explicitly declare as GET method
        public IActionResult GetListItems()
        {
            var session = HttpContext.Session.GetString(SystemConstant.CartSession);
            List<CartItemViewModel> currentCart = new List<CartItemViewModel>();
            if (session != null)
                currentCart = JsonConvert.DeserializeObject<List<CartItemViewModel>>(session);
            return Ok(currentCart);
        }

        [HttpPost("add-to-cart/{productId}")] // Explicitly declare as POST method
        public async Task<IActionResult> AddToCart(int productId)
        {
            var product = await _productService.GetProductByIdAsync(productId);

            var session = HttpContext.Session.GetString(SystemConstant.CartSession);
            List<CartItemViewModel> currentCart = new List<CartItemViewModel>();
            if (session != null)
                currentCart = JsonConvert.DeserializeObject<List<CartItemViewModel>>(session);

            int quantity = 1;
            if (currentCart.Any(x => x.ProductId == productId))
            {
                quantity = currentCart.First(x => x.ProductId == productId).Quantity + 1;
            }

            var cartItem = new CartItemViewModel()
            {
                ProductId = product.ProductID,
                Price = product.Price,
                Quantity = quantity
            };

            currentCart.Add(cartItem);

            HttpContext.Session.SetString(SystemConstant.CartSession, JsonConvert.SerializeObject(currentCart));
            return Ok(currentCart);
        }

        [HttpPost("update-cart/{id}")] // Explicitly declare as POST method
        public IActionResult UpdateCart(int id, int quantity)
        {
            var session = HttpContext.Session.GetString(SystemConstant.CartSession);
            List<CartItemViewModel> currentCart = new List<CartItemViewModel>();
            if (session != null)
                currentCart = JsonConvert.DeserializeObject<List<CartItemViewModel>>(session);

            foreach (var item in currentCart)
            {
                if (item.ProductId == id)
                {
                    if (quantity == 0)
                    {
                        currentCart.Remove(item);
                        break;
                    }
                    item.Quantity = quantity;
                }
            }

            HttpContext.Session.SetString(SystemConstant.CartSession, JsonConvert.SerializeObject(currentCart));
            return Ok(currentCart);
        }

        private CheckOutViewModel GetCheckoutViewModel()
        {
            var session = HttpContext.Session.GetString(SystemConstant.CartSession);
            List<CartItemViewModel> currentCart = new List<CartItemViewModel>();
            if (session != null)
                currentCart = JsonConvert.DeserializeObject<List<CartItemViewModel>>(session);
            var checkoutVm = new CheckOutViewModel()
            {
                CartItems = currentCart,
                CheckoutModel = new CheckoutRequest()
            };
            return checkoutVm;
        }
    }
}
