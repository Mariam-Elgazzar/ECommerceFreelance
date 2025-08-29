using ECommerce.BL.DTO.DashboardDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.BL.Services.DashboardServices
{
    public interface IDashboardServices
    {
        Task<DashboardDTO> GetDashboardDataAsync();
    }
}
