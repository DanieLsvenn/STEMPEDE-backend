
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Stem.Business.Constants;
using Stem.Business.Base;
using Stem.Business.Business;
using Stem.Data.Models;
using Stem.Common;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Stem.Data.DTO;
using System.Security.Claims;

namespace SWP391__StempedeKit_FA24.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserBusiness _userBusiness;
        private readonly STEMKITshopDBContext _shopDBContext;
        private readonly IEmailService _emailService;
        public AuthController(IUserBusiness userBusiness, IEmailService emailService)
        {
            _userBusiness = userBusiness;
            _emailService = emailService;
        }
        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> RegisterAsync([FromBody] UserRegisterDTO requestBody)
        {
            var serviceResponse = await _userBusiness.RegisterAsync(requestBody, UserConstants.CustomerRole);
            if (!serviceResponse.Succeeded)
            {
                return BadRequest(new { status = serviceResponse.Status, details = serviceResponse.Details });
            }

            var subject = "Chào mừng bạn đến với shop!";
            var body = $"Xin chào, Cảm ơn bạn đã đăng ký tài khoản tại KitStemHub! Chúng tôi rất vui khi có bạn là một phần của cộng đồng mua sắm của chúng tôi. Hãy khám phá và tận hưởng những ưu đãi đặc biệt dành riêng cho thành viên mới. Chúc bạn có trải nghiệm mua sắm tuyệt vời!";
            await _emailService.SendEmail(requestBody.Email!, subject, body);

            return Ok(new { status = serviceResponse.Status, details = serviceResponse.Details });
        }


        [HttpPost]
        [Route("Register/Staff")]
        public async Task<IActionResult> RegisterStaffAsync([FromBody] UserRegisterDTO requestBody)
        {
            var serviceResponse = await _userBusiness.RegisterAsync(requestBody, UserConstants.StaffRole);
            if (!serviceResponse.Succeeded)
            {
                return BadRequest(new { status = serviceResponse.Status, details = serviceResponse.Details });
            }

            var subject = "Chào mừng bạn đến với shop!";
            var body = $"Chào mừng bạn đến với KitStemHub! Chúc mừng bạn đã chính thức trở thành một thành viên trong đội ngũ của chúng tôi. Hy vọng chúng ta sẽ hợp tác hiệu quả và gặt hái nhiều thành công cùng nhau!";
            await _emailService.SendEmail(requestBody.Email!, subject, body);

            return Ok(new { status = serviceResponse.Status, details = serviceResponse.Details });
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDTO requestBody)
        {
            var serviceResponse = await _userBusiness.LoginAsync(requestBody);
            if (!serviceResponse.Succeeded)
            {
                return Unauthorized(new { status = serviceResponse.Status, details = serviceResponse.Details });
            }

            return Ok(new { status = serviceResponse.Status, details = serviceResponse.Details });
        }

        [HttpGet]
        [Route("Profile")]
        [Authorize(Roles = "customer")]
        public async Task<IActionResult> GetAsync()
        {
            var userName = User.FindFirst(ClaimTypes.Email)?.Value;
            var serviceResponse = await _userBusiness.GetAsync(userName!);
            if (!serviceResponse.Succeeded)
            {
                return BadRequest(new { status = serviceResponse.Status, details = serviceResponse.Details });
            }

            return Ok(new { status = serviceResponse.Status, details = serviceResponse.Details });
        }

        [HttpPut]
        [Route("Profile")]
        [Authorize(Roles = "customer")]
        public async Task<IActionResult> UpdateAsync(UserUpdateDTO userUpdateDTO)
        {
            var userName = User.FindFirst(ClaimTypes.Email)?.Value;

            var serviceResponse = await _userBusiness.UpdateAsync(userName!, userUpdateDTO);
            if (!serviceResponse.Succeeded)
            {
                return BadRequest(new { status = serviceResponse.Status, details = serviceResponse.Details });
            }
            return Ok(new { status = serviceResponse.Status, details = serviceResponse.Details });
        }

        [HttpPost]
        [Route("RefreshToken/{refreshToken:guid}")]
        public async Task<IActionResult> RefreshToken(int refreshToken)
        {
            var serviceResponse = await _userBusiness.RefreshTokenAsync(refreshToken);
            if (!serviceResponse.Succeeded)
            {
                return Unauthorized(new { status = serviceResponse.Status, details = serviceResponse.Details });
            }

            return Ok(new { status = serviceResponse.Status, details = serviceResponse.Details });
        }

        [HttpPost]
        [Route("RegisterWithRole/Only-For-Testing/{role}")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDTO requestBody, string role)
        {
            var serviceResponse = await _userBusiness.RegisterAsync(requestBody, role);
            if (!serviceResponse.Succeeded)
            {
                return BadRequest(new { status = serviceResponse.Status, details = serviceResponse.Details });
            }

            var subject = "Welcome to our shop!";
            var body = "Thank you for registering, We're excited to have you visit our shop. Explore our latest products and enjoy exclusive offers just for you!";
            await _emailService.SendEmail(requestBody.Email!, subject, body);

            return Ok(new { status = serviceResponse.Status, details = serviceResponse.Details });
        }
    }
}
   
    

