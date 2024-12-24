using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ecovegetables_api.src.Application.DTOs.User.Request
{
    public class UpdateRequest
    {
        public string? Fullname { get; set; }
        public string? Phone { get; set; }
        public string? Password { get; set; }
        public string? Avatar { get; set; }
        public string? Address { get; set; }
    }
} 