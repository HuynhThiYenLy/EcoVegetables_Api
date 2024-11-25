using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ecovegetables_api.src.Request
{
    public class OtpVerificationRequest
    {
        public string Email { get; set; }
        public string Otp { get; set; }
    }
}