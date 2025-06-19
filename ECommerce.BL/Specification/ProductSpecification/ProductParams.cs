using ECommerce.BL.Specification.Enums;

namespace ECommerce.BL.Specification.ProductSpecification
{
    public class ProductParams
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public string? Status { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public int? Quantity { get; set; }
        public SortProp? SortProp { get; set; }
        public SortDirection? SortDirection { get; set; }
    }
}

