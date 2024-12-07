using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EcoVegetables_Api.src.Request.User
{
    public class UpdateRequest
    {
        public string? Fullname { get; set; }
        public string? Phone { get; set; }
        public string? Password { get; set; }
        public string? Avatar { get; set; }
        public bool? IsActive { get; set; } = true;
        public string? Address { get; set; }
    }
} 