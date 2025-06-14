using ECommerce.BL.Specification.BaseSpacification;
using ECommerce.BL.Specification.Enums;
using ECommerce.DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text.Json;

namespace ECommerce.BL.Specification.ProductSpecification
{
    public class ProductSpecification : BaseSpecification<Product>
    {
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ProductSpecification"/> class for filtering and pagination.
        /// </summary>
        /// <param name="param">The parameters for filtering, sorting, and pagination.</param>
        public ProductSpecification(ProductParams param)
            : base(BuildExpression(param))
        {
            // Include related data
            AddInclude(p => p.Category);
            AddInclude(p => p.ProductMedia);

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
                    case SortProp.Id:
                        ApplyOrderBy(x => x.Id);
                        break;
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
                    case SortProp.Id:
                        ApplyOrderByDescending(x => x.Id);
                        break;
                    default:
                        ApplyOrderByDescending(x => x.Id);
                        break;
                }
            }

            // Pagination
            ApplyPagination(param.PageIndex, param.PageSize);
        }
        private static Expression<Func<Product, bool>> BuildExpression(ProductParams param)
        {
            Expression<Func<Product, bool>> expression = x => true;

            if (!string.IsNullOrEmpty(param.Search))
            {
                var search = param.Search.ToLower();
                expression = expression.And(x => x.Name.ToLower().Contains(search) ||
                                     x.Description.ToLower().Contains(search));
            }

            if (param.CategoryId.HasValue)
            {
                expression = expression.And(x => x.CategoryId == param.CategoryId.Value);
            }

            if (!string.IsNullOrEmpty(param.Status))
            {
                expression = expression.And(x => x.Status == param.Status);
            }

            if (param.AttributesFilter != null && param.AttributesFilter.Any())
            {
                foreach (var attr in param.AttributesFilter)
                {
                    var jsonPair = $"\"{attr.Key}\":\"{attr.Value}\"";
                    var jsonPairStart = $",{jsonPair}";
                    var jsonPairMiddle = $",{jsonPair}";
                    expression = expression.And(x => x.AdditionalAttributes != null &&
                        (EF.Functions.Like(x.AdditionalAttributes, $"%{jsonPair}%") ||
                         EF.Functions.Like(x.AdditionalAttributes, $"%{jsonPairStart}%") ||
                         EF.Functions.Like(x.AdditionalAttributes, $"%{jsonPairMiddle}%")));
                }

            }
            return expression;
        }
        public static bool AttributesFilter(Product product, ProductParams param)
        {
            if (param.AttributesFilter == null || !param.AttributesFilter.Any())
            {
                return true;
            }

            if (string.IsNullOrEmpty(product.AdditionalAttributes))
            {
                return false;
            }

            try
            {
                var attributes = JsonSerializer.Deserialize<Dictionary<string, string>>(product.AdditionalAttributes);
                return param.AttributesFilter.All(attr =>
                    attributes.TryGetValue(attr.Key, out var value) &&
                    value.Equals(attr.Value, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false; // Invalid JSON
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductSpecification"/> class for retrieving a product by ID.
        /// </summary>
        /// <param name="id">The ID of the product to retrieve.</param>
        public ProductSpecification(int id)
            : base(x => x.Id == id)
        {
            AddInclude(p => p.Category);
            AddInclude(p => p.ProductMedia);
        }
    }
    public static class ExpressionExtensions
    {
        public static Expression<Func<T, bool>> And<T>(
            this Expression<Func<T, bool>> expr1,
            Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters);
            return Expression.Lambda<Func<T, bool>>(
                Expression.AndAlso(expr1.Body, invokedExpr),
                expr1.Parameters);
        }
    }
}
