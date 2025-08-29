using ECommerce.BL.Helper;
using ECommerce.BL.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public BrandsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Retrieves a list of all brands.
        /// <returns>A list of brand names.</returns>
        /// <response code="200">Brands retrieved successfully.</response>
        /// <response code="500">Server error.</response>

        [HttpGet]
        [Route("~/Brands/GetAllBrands")]
        //[Authorize(Roles = $"{Roles.Admin}, {Roles.User}")]
        public async Task<IActionResult> GetAllBrands()
        {
            try
            {
                var brands = await _unitOfWork.brandServices.GetAllBrandsAsync();
                return Ok(brands);
            }
            catch (Exception ex)
            {
                // Log the exception (ex) here if needed
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
