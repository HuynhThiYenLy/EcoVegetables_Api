using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EcoVegetables_Api.src.Request.Category
{
    public class UpdateCategoryRequest
    {
        public string? Name { get; set; } // Có thể null
        public int? ParentId { get; set; } // Có thể null
    }
}