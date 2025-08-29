using Dapper;
using ECommerce.BL.DTO.DashboardDTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.BL.Services.DashboardServices
{
    public class DashboardServices : IDashboardServices
    {
        private readonly string _connectionString;
        public DashboardServices(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        /// <summary>
        /// Retrieves dashboard statistics asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation, containing the <see cref="DashboardDTO"/> with statistics.</returns>
        public async Task<DashboardDTO> GetDashboardDataAsync()
        {
            const string sql = @"
                SELECT 
    COUNT(*) As TotalProducts,
    (SELECT COUNT(*) FROM Orders) As TotalOrders,
    COUNT(CASE WHEN Status = N'شراء' THEN 1 END) As TotalPurchaseProducts,
    COUNT(CASE WHEN Status = N'إيجار' THEN 1 END) As TotalRentProducts,
    COUNT(CASE WHEN Status = N'إيجار وشراء' THEN 1 END) As TotalRentAndPurchaseProducts,
    (SELECT COUNT(*) FROM Orders WHERE OrderStatus = 0) As TotalNewOrders,
    (SELECT COUNT(*) FROM Orders WHERE OrderStatus = 1) As TotalProcessingOrders,
    (SELECT COUNT(*) FROM Orders WHERE OrderStatus = 2) As TotalCompletedOrders,
    (SELECT COUNT(*) FROM Orders WHERE OrderStatus = 3) As TotalCancelledOrders,
    (SELECT COUNT(*) FROM Orders WHERE ProductStatus = N'إيجار') As TotalRentOrders,
    (SELECT COUNT(*) FROM Orders WHERE ProductStatus = N'شراء') As TotalPurchaseOrders
FROM Products;
            ";

            try
            {
                using (var dbConnection = new SqlConnection(_connectionString))
                {
                    var result = await dbConnection.QuerySingleAsync<DashboardDTO>(sql);
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}

