using ECommerce.BL.Specification.BaseSpacification;
using ECommerce.BL.Specification.Enums;
using ECommerce.DAL.Models;

namespace ECommerce.BL.Specification.CategorySpecification
{
    public class CategorySpecification : BaseSpecification<Category>
    {
        public CategorySpecification(CategoryParams param)
            : base(x =>
                string.IsNullOrEmpty(param.Search) || x.Name.ToLower().Contains(param.Search.ToLower()) ||
                x.Description == null ||
                x.Description.ToLower().Contains(param.Search.ToLower()))
        {
            var sortProp = param.SortProp ?? SortProp.Id;
            var sortDirection = param.SortDirection ?? SortDirection.Ascending;

            if (sortDirection == SortDirection.Ascending)
            {
                switch (sortProp)
                {
                    case SortProp.Name:
                        ApplyOrderBy(x => x.Name);
                        break;
                    case SortProp.Description:
                        ApplyOrderBy(x => x.Description == null ? string.Empty : x.Description);
                        break;
                    case SortProp.Id:
                    default:
                        ApplyOrderBy(x => x.Id);
                        break;
                }
            }
            else
            {
                switch (sortProp)
                {
                    case SortProp.Name:
                        ApplyOrderByDescending(x => x.Name);
                        break;
                    case SortProp.Description:
                        ApplyOrderByDescending(x => x.Description == null ? string.Empty : x.Description);
                        break;
                    case SortProp.Id:
                    default:
                        ApplyOrderByDescending(x => x.Id);
                        break;
                }
            }

            ApplyPagination(param.PageIndex, param.PageSize);
        }
        public CategorySpecification(int id)
            : base(x => x.Id == id)
        {
        }
    }
}
