using ECommerce.BL.Specification.Enums;

namespace ECommerce.BL.Specification.CategorySpecification
{
    public class CategoryParams
    {
        public string? Search { get; set; }
        public SortProp? SortProp { get; set; }
        public SortDirection? SortDirection { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public CategoryParams()
        {
            PageSize = Math.Clamp(PageSize, 1, 10);
        }
    }
}
