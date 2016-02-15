using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerManager.Utility
{
    class Sendkeys
    {
        public static void Send(IntPtr handle, string text)
        {
            if (handle == IntPtr.Zero)
                return;
            SetForegroundWindow(handle);
            Thread.Sleep(100);
            System.Windows.Forms.SendKeys.SendWait(text);
            Thread.Sleep(text.Length * 25 + (text.Contains("~") ? 175 : 0));
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
