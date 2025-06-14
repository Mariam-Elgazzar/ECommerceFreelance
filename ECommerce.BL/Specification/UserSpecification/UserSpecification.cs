using ECommerce.BL.Specification.BaseSpacification;
using ECommerce.BL.Specification.Enums;
using ECommerce.DAL.Extend;
using System;

namespace ECommerce.BL.Specification.UserSpecification
{
    public class UserSpecification : BaseSpecification<ApplicationUser>
    {
        public UserSpecification(UserParams param)
            : base(x =>
                (string.IsNullOrEmpty(param.Search) ||
                 x.Email.ToLower().Contains(param.Search.ToLower()) ||
                 x.FirstName.ToLower().Contains(param.Search.ToLower()) ||
                 x.LastName.ToLower().Contains(param.Search.ToLower())) &&
                (!param.IsDeleted.HasValue || x.IsDeleted == param.IsDeleted.Value))
        {
            var sortProp = param.SortProp ?? SortProp.Id;
            var sortDirection = param.SortDirection ?? SortDirection.Ascending;

            if (sortDirection == SortDirection.Ascending)
            {
                switch (sortProp)
                {
                    case SortProp.FName:
                        ApplyOrderBy(x => x.FirstName);
                        break;
                    case SortProp.LName:
                        ApplyOrderBy(x => x.LastName);
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
                    case SortProp.FName:
                        ApplyOrderByDescending(x => x.FirstName);
                        break;
                    case SortProp.LName:
                        ApplyOrderByDescending(x => x.LastName);
                        break;
                    case SortProp.Id:
                    default:
                        ApplyOrderByDescending(x => x.Id);
                        break;
                }
            }

            ApplyPagination(param.PageIndex, param.PageSize);
        }

        public UserSpecification(string id)
            : base(x => x.Id == id)
        {
        }
    }
}