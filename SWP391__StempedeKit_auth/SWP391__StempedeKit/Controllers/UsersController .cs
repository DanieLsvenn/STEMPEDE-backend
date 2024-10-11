using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391__StempedeKit.Data;
using SWP391__StempedeKit.Models;

namespace SWP391__StempedeKit.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]  // Only authenticated users can access these endpoints
    public class UsersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public UsersController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("profile")]
        public IActionResult GetUserProfile()
        {
            var userId = int.Parse(User.Identity.Name);
            var user = _unitOfWork.GetRepository<User>().GetById(userId);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }
    }

}
