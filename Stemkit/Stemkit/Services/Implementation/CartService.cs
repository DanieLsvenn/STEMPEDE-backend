using AutoMapper;
using Stemkit.Data;
using Stemkit.DTOs.Cart;
using Stemkit.Models;
using Stemkit.Services.Interfaces;

namespace Stemkit.Services.Implementation
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CartService> _logger;
        private readonly IMapper _mapper;

        public CartService(IUnitOfWork unitOfWork, ILogger<CartService> logger, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<CartDto?> GetCartAsync(string userName)
        {
            _logger.LogInformation("Retrieving cart for user: {UserName}", userName);

            var userRepository = _unitOfWork.GetRepository<User>();
            var user = await userRepository.FindAsync(u => u.Username == userName);
            var userEntity = user.FirstOrDefault();

            if (userEntity == null)
            {
                _logger.LogWarning("User {UserName} not found.", userName);
                throw new ArgumentException("User not found.");
            }

            var cartRepository = _unitOfWork.GetRepository<Cart>();
            var cart = await cartRepository.FindAsync(c => c.UserId == userEntity.UserId && c.Status == "Active");
            var activeCart = cart.FirstOrDefault();

            if (activeCart == null)
            {
                _logger.LogInformation("No active cart found for user: {UserName}", userName);
                return null; // Controller will handle this case
            }

            // Load cart items with products
            var cartItemRepository = _unitOfWork.GetRepository<CartItem>();
            var cartItems = await cartItemRepository.FindAsync(ci => ci.CartId == activeCart.CartId, includeProperties: "Product");

            var cartItemDtos = cartItems.Select(ci => new CartItemDto
            {
                CartItemId = ci.CartItemId,
                ProductId = ci.ProductId,
                ProductName = ci.Product.ProductName,
                Quantity = ci.Quantity,
                Price = ci.Price,
                TotalPrice = ci.Price * ci.Quantity
            }).ToList();

            var cartDto = new CartDto
            {
                CartId = activeCart.CartId,
                UserId = activeCart.UserId,
                CreatedDate = activeCart.CreatedDate,
                Status = activeCart.Status,
                Items = cartItemDtos,
                TotalAmount = cartItemDtos.Sum(ci => ci.TotalPrice)
            };

            _logger.LogInformation("Cart retrieved successfully for user: {UserName}", userName);
            return cartDto;
        }

        public async Task<string> AddItemToCartAsync(string userName, int productId, int quantity)
        {
            _logger.LogInformation("Adding ProductID: {ProductId} to cart for User: {UserName}", productId, userName);

            var userRepository = _unitOfWork.GetRepository<User>();
            var user = await userRepository.FindAsync(u => u.Username == userName);
            var userEntity = user.FirstOrDefault();

            if (userEntity == null)
            {
                _logger.LogWarning("User {UserName} not found.", userName);
                throw new ArgumentException("User not found.");
            }

            var productRepository = _unitOfWork.GetRepository<Product>();
            var product = await productRepository.GetByIdAsync(productId);

            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found.", productId);
                throw new ArgumentException("Product not found.");
            }

            if (product.StockQuantity < quantity)
            {
                _logger.LogWarning("Insufficient stock for ProductID: {ProductId}. Requested: {Quantity}, Available: {Stock}", productId, quantity, product.StockQuantity);
                throw new ArgumentException("Insufficient stock for the requested product.");
            }

            // Retrieve or create active cart
            var cartRepository = _unitOfWork.GetRepository<Cart>();
            var cart = await cartRepository.FindAsync(c => c.UserId == userEntity.UserId && c.Status == "Active");
            var activeCart = cart.FirstOrDefault();

            if (activeCart == null)
            {
                activeCart = new Cart
                {
                    UserId = userEntity.UserId,
                    CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Status = "Active"
                };
                await cartRepository.AddAsync(activeCart);
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Created new cart for UserID: {UserId}", userEntity.UserId);
            }

            // Check if product already in cart
            var cartItemRepository = _unitOfWork.GetRepository<CartItem>();
            var existingCartItem = await cartItemRepository.FindAsync(ci => ci.CartId == activeCart.CartId && ci.ProductId == productId);
            var cartItem = existingCartItem.FirstOrDefault();

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
                cartItem.Price = product.Price; // Update price in case it has changed
                cartItemRepository.Update(cartItem);
                _logger.LogInformation("Updated quantity for CartItemID: {CartItemId}", cartItem.CartItemId);
            }
            else
            {
                cartItem = new CartItem
                {
                    CartId = activeCart.CartId,
                    ProductId = productId,
                    Quantity = quantity,
                    Price = product.Price
                };
                await cartItemRepository.AddAsync(cartItem);
                _logger.LogInformation("Added new CartItemID: {CartItemId} to CartID: {CartId}", cartItem.CartItemId, activeCart.CartId);
            }

            // Deduct stock
            product.StockQuantity -= quantity;
            productRepository.Update(product);

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Item added to cart successfully for UserID: {UserId}", userEntity.UserId);
            return "Item added to cart successfully.";
        }

        public async Task<string> UpdateCartItemAsync(string userName, int cartItemId, int quantity)
        {
            _logger.LogInformation("Updating CartItemID: {CartItemId} for User: {UserName} to Quantity: {Quantity}", cartItemId, userName, quantity);

            var userRepository = _unitOfWork.GetRepository<User>();
            var user = await userRepository.FindAsync(u => u.Username == userName);
            var userEntity = user.FirstOrDefault();

            if (userEntity == null)
            {
                _logger.LogWarning("User {UserName} not found.", userName);
                throw new ArgumentException("User not found.");
            }

            var cartItemRepository = _unitOfWork.GetRepository<CartItem>();
            var cartItem = await cartItemRepository.GetByIdAsync(cartItemId);

            if (cartItem == null || cartItem.Cart.UserId != userEntity.UserId || cartItem.Cart.Status != "Active")
            {
                _logger.LogWarning("CartItemID: {CartItemId} not found or does not belong to user: {UserName}.", cartItemId, userName);
                throw new ArgumentException("Cart item not found.");
            }

            var productRepository = _unitOfWork.GetRepository<Product>();
            var product = await productRepository.GetByIdAsync(cartItem.ProductId);

            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found for CartItemID: {CartItemId}.", cartItem.ProductId, cartItemId);
                throw new ArgumentException("Product not found.");
            }

            if (quantity > cartItem.Quantity)
            {
                int additionalQuantity = quantity - cartItem.Quantity;
                if (product.StockQuantity < additionalQuantity)
                {
                    _logger.LogWarning("Insufficient stock for ProductID: {ProductId}. Requested Additional: {AdditionalQuantity}, Available: {Stock}", product.ProductId, additionalQuantity, product.StockQuantity);
                    throw new ArgumentException("Insufficient stock for the requested quantity.");
                }
                product.StockQuantity -= additionalQuantity;
            }
            else if (quantity < cartItem.Quantity)
            {
                int reducedQuantity = cartItem.Quantity - quantity;
                product.StockQuantity += reducedQuantity;
            }

            cartItem.Quantity = quantity;
            cartItem.Price = product.Price; // Update price if it has changed
            cartItemRepository.Update(cartItem);
            productRepository.Update(product);

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("CartItemID: {CartItemId} updated successfully.", cartItemId);
            return "Cart item updated successfully.";
        }

        public async Task<string> RemoveItemFromCartAsync(string userName, int cartItemId)
        {
            _logger.LogInformation("Removing CartItemID: {CartItemId} from cart for User: {UserName}", cartItemId, userName);

            var userRepository = _unitOfWork.GetRepository<User>();
            var user = await userRepository.FindAsync(u => u.Username == userName);
            var userEntity = user.FirstOrDefault();

            if (userEntity == null)
            {
                _logger.LogWarning("User {UserName} not found.", userName);
                throw new ArgumentException("User not found.");
            }

            var cartItemRepository = _unitOfWork.GetRepository<CartItem>();
            var cartItem = await cartItemRepository.GetByIdAsync(cartItemId);

            if (cartItem == null || cartItem.Cart.UserId != userEntity.UserId || cartItem.Cart.Status != "Active")
            {
                _logger.LogWarning("CartItemID: {CartItemId} not found or does not belong to user: {UserName}.", cartItemId, userName);
                throw new ArgumentException("Cart item not found.");
            }

            var productRepository = _unitOfWork.GetRepository<Product>();
            var product = await productRepository.GetByIdAsync(cartItem.ProductId);

            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found for CartItemID: {CartItemId}.", cartItem.ProductId, cartItemId);
                throw new ArgumentException("Product not found.");
            }

            // Restore stock
            product.StockQuantity += cartItem.Quantity;
            productRepository.Update(product);

            // Remove cart item
            cartItemRepository.Delete(cartItem);

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("CartItemID: {CartItemId} removed successfully.", cartItemId);
            return "Item removed from cart successfully.";
        }

        public async Task<string> ClearCartAsync(string userName)
        {
            _logger.LogInformation("Clearing cart for User: {UserName}", userName);

            var userRepository = _unitOfWork.GetRepository<User>();
            var user = await userRepository.FindAsync(u => u.Username == userName);
            var userEntity = user.FirstOrDefault();

            if (userEntity == null)
            {
                _logger.LogWarning("User {UserName} not found.", userName);
                throw new ArgumentException("User not found.");
            }

            var cartRepository = _unitOfWork.GetRepository<Cart>();
            var cart = await cartRepository.FindAsync(c => c.UserId == userEntity.UserId && c.Status == "Active");
            var activeCart = cart.FirstOrDefault();

            if (activeCart == null)
            {
                _logger.LogInformation("No active cart found for User: {UserName}", userName);
                return "No active cart to clear.";
            }

            var cartItemRepository = _unitOfWork.GetRepository<CartItem>();
            var cartItems = await cartItemRepository.FindAsync(ci => ci.CartId == activeCart.CartId);

            foreach (var cartItem in cartItems)
            {
                var productRepository = _unitOfWork.GetRepository<Product>();
                var product = await productRepository.GetByIdAsync(cartItem.ProductId);

                if (product != null)
                {
                    product.StockQuantity += cartItem.Quantity;
                    productRepository.Update(product);
                }

                cartItemRepository.Delete(cartItem);
            }

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Cart cleared successfully for User: {UserName}", userName);
            return "Cart cleared successfully.";
        }

        //public async Task<string> CheckoutAsync(string userName, CheckoutDto checkoutDto)
        //{
        //    _logger.LogInformation("Checkout initiated for User: {UserName}", userName);

        //    using var transaction = await _unitOfWork.BeginTransactionAsync();

        //    try
        //    {
        //        var userRepository = _unitOfWork.GetRepository<User>();
        //        var user = await userRepository.FindAsync(u => u.Username == userName);
        //        var userEntity = user.FirstOrDefault();

        //        if (userEntity == null)
        //        {
        //            _logger.LogWarning("User {UserName} not found.", userName);
        //            throw new ArgumentException("User not found.");
        //        }

        //        var cartRepository = _unitOfWork.GetRepository<Cart>();
        //        var cart = await cartRepository.FindAsync(c => c.UserId == userEntity.UserId && c.Status == "Active");
        //        var activeCart = cart.FirstOrDefault();

        //        if (activeCart == null)
        //        {
        //            _logger.LogWarning("No active cart found for User: {UserName}", userName);
        //            throw new ArgumentException("No active cart found.");
        //        }

        //        var cartItemRepository = _unitOfWork.GetRepository<CartItem>();
        //        var cartItems = await cartItemRepository.FindAsync(ci => ci.CartId == activeCart.CartID, includeProperties: "Product");

        //        if (!cartItems.Any())
        //        {
        //            _logger.LogWarning("Active cart for User: {UserName} is empty.", userName);
        //            throw new ArgumentException("Cart is empty.");
        //        }

        //        // Calculate total amount
        //        decimal totalAmount = 0;
        //        foreach (var cartItem in cartItems)
        //        {
        //            if (cartItem.Product.StockQuantity < cartItem.Quantity)
        //            {
        //                _logger.LogWarning("Insufficient stock for ProductID: {ProductId}.", cartItem.ProductID);
        //                throw new ArgumentException($"Insufficient stock for product: {cartItem.Product.ProductName}");
        //            }
        //            totalAmount += cartItem.Price * cartItem.Quantity;
        //        }

        //        // Create Order
        //        var orderRepository = _unitOfWork.GetRepository<Order>();
        //        var order = new Order
        //        {
        //            UserId = userEntity.UserId,
        //            OrderDate = DateTime.UtcNow,
        //            TotalAmount = totalAmount
        //        };
        //        await orderRepository.AddAsync(order);
        //        await _unitOfWork.CompleteAsync();
        //        _logger.LogInformation("OrderID: {OrderId} created for UserID: {UserId}", order.OrderId, userEntity.UserId);

        //        // Create OrderDetails
        //        var orderDetailRepository = _unitOfWork.GetRepository<OrderDetail>();
        //        foreach (var cartItem in cartItems)
        //        {
        //            var orderDetail = new OrderDetail
        //            {
        //                OrderId = order.OrderId,
        //                ProductId = cartItem.ProductID,
        //                ProductDescription = cartItem.Product.Description,
        //                Quantity = cartItem.Quantity,
        //                Price = cartItem.Price
        //            };
        //            await orderDetailRepository.AddAsync(orderDetail);

        //            // Deduct stock
        //            cartItem.Product.StockQuantity -= cartItem.Quantity;
        //            _unitOfWork.GetRepository<Product>().Update(cartItem.Product);

        //            // Remove cart item
        //            cartItemRepository.Delete(cartItem);
        //        }

        //        await _unitOfWork.CompleteAsync();

        //        // Update cart status
        //        activeCart.Status = "CheckedOut";
        //        cartRepository.Update(activeCart);
        //        await _unitOfWork.CompleteAsync();

        //        await transaction.CommitAsync();

        //        _logger.LogInformation("Checkout completed successfully for User: {UserName}, OrderID: {OrderId}", userName, order.OrderId);
        //        return $"Checkout successful. Order ID: {order.OrderId}";
        //    }
        //    catch (Exception ex)
        //    {
        //        await transaction.RollbackAsync();
        //        _logger.LogError(ex, "Checkout failed for User: {UserName}", userName);
        //        throw;
        //    }
        //}
    }
}
