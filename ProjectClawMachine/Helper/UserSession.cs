using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectClawMachine.Helper
{
    public static class UserSession
    {
        // 현재 로그인한 유저 ID
        public static string CurrentUserId { get; set; }
    }

    public static class PageSession
    {
        // 현재 로그인한 유저 ID
        public static string CurrentPage { get; set; }
    }

    public static class EventSession
    {
        public static bool IsKeyDownEventAttached { get; set; } = false;
    }
}
