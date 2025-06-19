using ECommerce.BL.Specification.BaseSpacification;
using ECommerce.BL.Specification.Enums;
using ECommerce.DAL.Models;

namespace ECommerce.BL.Specification.ProductSpecification
{
    public class ProductSpecification : BaseSpecification<Product>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductSpecification"/> class for filtering and pagination.
        /// </summary>
        /// <param name="param">The parameters for filtering, sorting, and pagination.</param>
        public ProductSpecification(ProductParams param)
            : base(x => param == null ||
            (string.IsNullOrEmpty(param.Search) &&
            string.IsNullOrEmpty(param.Brand) &&
            string.IsNullOrEmpty(param.Model) &&
            string.IsNullOrEmpty(param.Status) &&
            !param.CategoryId.HasValue &&
            !param.Quantity.HasValue) ||
            ((param.Search != null && (
            (!string.IsNullOrEmpty(param.Search) && 
            x.Name.ToLower().Contains(param.Search.ToLower())) ||
            (x.Description != null && 
            x.Description.ToLower().Contains(param.Search.ToLower())))) ||
            (param.Brand != null && x.Brand != null && 
            x.Brand.ToLower().Contains(param.Brand.ToLower())) ||
            (param.Model != null && x.Modal != null && 
            x.Modal.ToLower().Contains(param.Model.ToLower())) ||
            (param.Status != null && x.Status != null && 
            x.Status.ToLower().Contains(param.Status.ToLower())) ||
            (param.CategoryId.HasValue && x.CategoryId == param.CategoryId.Value) ||
            (param.Quantity.HasValue && x.Quantity == param.Quantity.Value))
            )
        {
            // Include related data
            AddInclude(p => p.Category);
            AddInclude(pm => pm.ProductMedia);
            // Sorting
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
                    case SortProp.Brand:
                        ApplyOrderBy(x => x.Brand);
                        break;
                    case SortProp.Modal:
                        ApplyOrderBy(x => x.Modal);
                        break;
                    case SortProp.Quantity:
                        ApplyOrderBy(x => x.Quantity);
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
                    case SortProp.Brand:
                        ApplyOrderByDescending(x => x.Brand);
                        break;
                    case SortProp.Modal:
                        ApplyOrderByDescending(x => x.Modal);
                        break;
                    case SortProp.Quantity:
                        ApplyOrderByDescending(x => x.Quantity);
                        break;
                    case SortProp.Id:
                    default:
                        ApplyOrderByDescending(x => x.Id);
                        break;
                }
            }

            // Pagination
            ApplyPagination(param.PageIndex, param.PageSize);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductSpecification"/> class for retrieving a product by ID.
        /// </summary>
        /// <param name="id">The ID of the product to retrieve.</param>
        public ProductSpecification(int id)
            : base(x => x.Id == id)
        {
            AddInclude(p => p.Category);
            AddInclude(pm => pm.ProductMedia);
        }
    }
}
