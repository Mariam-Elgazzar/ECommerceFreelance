using ECommerce.BL.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public DashboardController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Retrieves dashboard statistics asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation, containing the <see cref="DashboardDTO"/> with statistics.</returns>
        [HttpGet]
        [Route("~/Dashboard/GetDashboardData")]
        public async Task<IActionResult> GetDashboardDataAsync()
        {
            try
            {
                var dashboardData = await _unitOfWork.DashboardServices.GetDashboardDataAsync();
                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving dashboard data: {ex.Message}");
            }
        }
    }
}
