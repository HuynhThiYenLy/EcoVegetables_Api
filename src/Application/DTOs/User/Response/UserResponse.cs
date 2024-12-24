using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ecovegetables_api.src.Application.DTOs.User.Response
{
    public class UserResponse
    {
        public int Id { get; set; }
        public string Fullname { get; set; }
        public string Email { get; set; }
    }
}