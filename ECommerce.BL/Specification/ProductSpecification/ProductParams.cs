using ECommerce.BL.Specification.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.BL.Specification.ProductSpecification
{
    public class ProductParams
    {
        public string? Search { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, string>? AttributesFilter { get; set; }
        public int? CategoryId { get; set; }
        public string? Status { get; set; }
        public SortProp? SortProp { get; set; }
        public SortDirection? SortDirection { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public ProductParams()
        {
            PageSize = Math.Clamp(PageSize, 1, 10);
        }
    }
}
