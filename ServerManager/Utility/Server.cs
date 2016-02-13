using ServerManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerManager.Utility
{
    class Server
    {
        public IntPtr Handle { get { return IsAlive ? Process.MainWindowHandle : IntPtr.Zero; } }
        public bool IsAlive { get; set; }
        public bool IsReady { get; set; }
        public bool Stopping { get; set; }
        public Process Process { get; private set; }
        public ServerData Data { get; private set; }
        public RemoteControl Remote { get; private set; }

        public bool Responding
        {
            get
            {
                if (Process.HasExited)
                    return true;
                return Process.Responding;
            }
        }

        Thread waitidle;

        public Server()
        {
            Process = new Process();
            Process.EnableRaisingEvents = true;
            if (File.Exists(Program.Config.Values.Connection.ServerPath))
                Process.StartInfo.FileName = Program.Config.Values.Connection.ServerPath;
            Process.StartInfo.Arguments = Program.Config.Values.Connection.ToString();

            IsReady = false;
            IsAlive = false;
            Stopping = false;

            Remote = new RemoteControl(Program.Config.Values.Remote.Password, Program.Config.Values.Remote.Port, this);
            Data = new ServerData(new IPEndPoint(IPAddress.Parse(Program.Config.Values.Connection.IP), Program.Config.Values.Connection.Port));

            if (Program.Config.Values.Remote.Activated)
                Remote.StartListening();

            if (Program.Config.Values.StartServerAtStartup)
                Start();
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            IsReady = false;
            IsAlive = false;

            if (!Program.Config.Values.Connection.AutoRestart || Stopping)
            {
                if (Stopping)
                    Stopping = false;
                return;
            }

            Process.Start();
            IsAlive = true;

            if (waitidle != null && waitidle.IsAlive)
                waitidle.Abort();
            waitidle = new Thread(WaitIdle);
            waitidle.Start();
        }

        public void Start()
        {
            if (IsAlive)
                return;

            Process.Exited += Process_Exited;
            Process.Start();

            if (waitidle != null && waitidle.IsAlive)
                waitidle.Abort();

            waitidle = new Thread(WaitIdle);
            waitidle.Start();

            IsAlive = true;
        }

        private void WaitIdle()
        {
            Process.WaitForInputIdle();
            if (Process.HasExited)
                return;

            IsReady = true;
            Data.Refresh();
        }

        public void Stop()
        {
            if (!IsAlive)
                return;

            IsAlive = false;
            IsReady = false;

            if (waitidle != null && waitidle.IsAlive)
                waitidle.Abort();

            Process.Exited -= Process_Exited;
            if (!Process.HasExited)
                Process.Kill();
        }

        public void Restart()
        {
            Stop();
            Start();
        }
    }
}
