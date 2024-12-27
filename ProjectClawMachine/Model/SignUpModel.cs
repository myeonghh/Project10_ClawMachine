using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectClawMachine.Model
{
    public class SignUpModel
    {
        public string UserId { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }

        // 유효성 검사 메서드
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(UserId) &&
                   !string.IsNullOrEmpty(Password) &&
                   !string.IsNullOrEmpty(PhoneNumber) &&
                   !string.IsNullOrEmpty(Address);
        }
    }
}
