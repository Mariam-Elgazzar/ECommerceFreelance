using ECommerce.BL.DTO.OrderDTOs;
using ECommerce.BL.UnitOfWork;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IUnitOfWork unitOfWork, ILogger<OrderController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #region Checkout 

        /// <summary>
        /// Initiates the checkout process for a user's cart.
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A response indicating the result of the checkout process.</returns>
        /// <response code="200">Checkout initiated successfully.</response>
        /// <response code="400">Invalid request data.</response>
        /// <response code="401">Unauthorized access.</response>
        /// <response code="500">Server error.</response>
        [HttpPost]
        [Route("~/Orders/Checkout")]
        //[Authorize(Roles = $"{Roles.User}, {Roles.Admin}")]
        public async Task<IActionResult> Checkout(OrderDTO dto)
        {
            try
            {
                var response = await _unitOfWork.OrderServices.ConfirmCheckout(dto);
                if (response == null)
                {
                    return BadRequest(new { Message = "Checkout failed. Please try again." });
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request" });
            }
        }

        #endregion
    }
}