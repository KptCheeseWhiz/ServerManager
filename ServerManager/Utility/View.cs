using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ServerManager.Utility
{
    public class View
    {
        [DllImport("User32")]
        private static extern int ShowWindow(IntPtr hwnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        public static bool ChangeVisibility(IntPtr handle)
        {
            if (ShowWindow(handle, SW_HIDE) == 0)
            {
                ShowWindow(handle, SW_SHOW);
                return true;
            }
            else
                return false;
        }
    }
}
