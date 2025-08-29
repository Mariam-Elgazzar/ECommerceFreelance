using ECommerce.BL.Specification.Enums;
using System;

namespace ECommerce.BL.Specification.OrderSpecification
{
    public class OrderParams
    {
        public string? Search { get; set; }
        public int ProductId { get; set; }
        public string? OrderStatus { get; set; }
        public string? ProductStatus { get; set; }
        public string? RentalPeriod { get; set; }
        public DateTime? Date { get; set; }
        public SortProp? SortProp { get; set; }
        public SortDirection? SortDirection { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public OrderParams()
        {
            PageSize = Math.Clamp(PageSize, 1, 10);
        }
    }
}