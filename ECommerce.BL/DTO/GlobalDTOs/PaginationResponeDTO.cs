using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.BL.DTO.GlobalDTOs
{
    public class PaginationResponse<T>
    {
        public int PageSize { get; set; }
        public int PageIndex { get; set; }
        public int TotalCount { get; set; }
        public IReadOnlyList<T> Data { get; set; }
    }
}
