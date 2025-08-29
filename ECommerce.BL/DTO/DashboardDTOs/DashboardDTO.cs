using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.BL.DTO.DashboardDTOs
{
    /// <summary>
    /// Data Transfer Object (DTO) for dashboard statistics related to heavy equipment eCommerce.
    /// Provides counts for products, orders, and their statuses, categorized by purchase and rent options.
    /// </summary>
    public class DashboardDTO
    {
        /// <summary>
        /// Gets or sets the total number of heavy equipment products available in the system.
        /// </summary>
        public int TotalProducts { get; set; }
        /// <summary>
        /// Gets or sets the total number of orders placed for heavy equipment.
        /// </summary>
        public int TotalOrders { get; set; }
        /// <summary>
        /// Gets or sets the total number of new heavy equipment products added to the system.
        /// </summary>
        /// <summary>
        /// Gets or sets the total number of new orders placed for heavy equipment.
        /// </summary>
        public int TotalRentProducts { get; set; }
        /// <summary>
        /// Gets or sets the total number of heavy equipment products available for purchase.
        /// </summary>
        public int TotalPurchaseProducts { get; set; }
        /// <summary>
        /// Gets or sets the total number of heavy equipment products that support both rent and purchase options.
        /// </summary>
        public int TotalRentAndPurchaseProducts { get; set; }
        public int TotalNewOrders { get; set; }
        /// <summary>
        /// Gets or sets the total number of orders currently under processing.
        /// </summary>
        public int TotalProcessingOrders { get; set; }
        /// <summary>
        /// Gets or sets the total number of completed orders for heavy equipment.
        /// </summary>
        public int TotalCompletedOrders { get; set; }
        /// <summary>
        /// Gets or sets the total number of cancelled orders for heavy equipment.
        /// </summary>
        public int TotalCancelledOrders { get; set; }
        /// <summary>
        /// Gets or sets the total number of heavy equipment products available for rent.
        /// </summary>
        /// <summary>
        /// Gets or sets the total number of orders placed for renting heavy equipment.
        /// </summary>
        public int TotalRentOrders { get; set; }
        /// <summary>
        /// Gets or sets the total number of orders placed for purchasing heavy equipment.
        /// </summary>
        public int TotalPurchaseOrders { get; set; }
    }
}
