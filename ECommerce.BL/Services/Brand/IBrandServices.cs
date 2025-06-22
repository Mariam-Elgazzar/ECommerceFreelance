using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.BL.Services.Brand
{
    public interface IBrandServices
    { 
        /// <summary>
        /// Retrieves a list of all brands.
        /// </summary>
        /// <returns>A list of brand names.</returns>
        Task<List<string>> GetAllBrandsAsync();
    }
}
