using ECommerce.BL.Specification.Enums;

namespace ECommerce.BL.Specification.UserSpecification
{
    public class UserParams
    {
        public string? Search { get; set; }
        //public string? Description { get; set; }
        public SortProp? SortProp { get; set; }
        public SortDirection? SortDirection { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public bool? IsDeleted { get; set; }
        public UserParams()
        {
            PageSize = Math.Clamp(PageSize, 1, 10);
        }
    }
}
