using ECommerce.BL.Specification.BaseSpacification;
using ECommerce.BL.Specification.Enums;
using ECommerce.DAL.Models;
using System;

namespace ECommerce.BL.Specification.OrderSpecification
{
    public class OrderSpecification : BaseSpecification<Order>
    {
        public OrderSpecification(OrderParams param)
            : base(x =>
                param == null ||
                (string.IsNullOrEmpty(param.Search) &&
                 param.ProductId == 0 &&
                 string.IsNullOrEmpty(param.OrderStatus) &&
                 string.IsNullOrEmpty(param.ProductStatus) &&
                 string.IsNullOrEmpty(param.RentalPeriod) &&
                 !param.Date.HasValue) ||
                (
                    (string.IsNullOrEmpty(param.Search) ||
                     (
                         x.Name.ToLower().Contains(param.Search.ToLower()) ||
                         x.Email.ToLower().Contains(param.Search.ToLower()) ||
                         x.PhoneNumber.ToLower().Contains(param.Search.ToLower()) ||
                         x.Address.ToLower().Contains(param.Search.ToLower()) ||
                         x.Product.Name.ToLower().Contains(param.Search.ToLower()) 
                     )) &&
                    (param.ProductId == 0 ||
                     x.ProductId == param.ProductId) &&
                    (string.IsNullOrEmpty(param.OrderStatus) ||
                     x.OrderStatus.ToString() == param.OrderStatus) &&
                    (string.IsNullOrEmpty(param.ProductStatus) ||
                     x.ProductStatus.Contains(param.ProductStatus)) &&
                    (string.IsNullOrEmpty(param.RentalPeriod) ||
                     x.RentalPeriod.Contains(param.RentalPeriod)) &&
                    (!param.Date.HasValue ||
                     x.Date.Date == param.Date.Value.Date)
                ))
        {
            AddInclude(o => o.Product);

            var sortProp = param.SortProp ?? SortProp.Id;
            var sortDirection = param.SortDirection ?? SortDirection.Ascending;

            if (sortDirection == SortDirection.Ascending)
            {
                switch (sortProp)
                {
                    case SortProp.Id:
                        ApplyOrderBy(x => x.Id);
                        break;
                    case SortProp.Name:
                        ApplyOrderBy(x => x.Product.Name);
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
                    case SortProp.Id:
                        ApplyOrderByDescending(x => x.Id);
                        break;
                    case SortProp.Name:
                        ApplyOrderByDescending(x => x.Product.Name);
                        break; 
                    default:
                        ApplyOrderByDescending(x => x.Id);
                        break;
                }
            }

            ApplyPagination(param.PageIndex, param.PageSize);
        }

        public OrderSpecification(int orderId)
            : base(x => x.Id == orderId)
        {
            AddInclude(o => o.Product);
        }
    }
}