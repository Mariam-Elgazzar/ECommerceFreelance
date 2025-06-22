using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ECommerce.BL.Services.Brand
{
    public class BrandServices : IBrandServices
    {
        private readonly string _connectionString;
        public BrandServices(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// Retrieves a list of all brands.
        /// </summary>
        /// <returns>A list of brand names.</returns>
        public async Task<List<string>> GetAllBrandsAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT DISTINCT Brand FROM Products where Brand != ''";
                return connection.Query<string>(query).ToList();
            }
        }
    }
}
