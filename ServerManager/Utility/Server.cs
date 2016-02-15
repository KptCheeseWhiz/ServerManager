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
    class Server : IDisposable
    {
        public IntPtr Handle { get { return IsAlive ? Process.MainWindowHandle : IntPtr.Zero; } }
        public bool IsAlive { get; set; }
        public bool IsReady { get; set; }
        public bool Stopping { get; set; }
        public Process Process { get; private set; }
        public ServerData Data { get; private set; }
        public RemoteControl Remote { get; private set; }
        public Statistics Stats { get; private set; }

        public bool Responding
        {
            get
            {
                if (Process.HasExited)
                    return true;
                return Process.Responding;
            }
        }

        Thread waitidle, drawgraph;

        public Server()
        {
            Process = new Process();
            Process.EnableRaisingEvents = true;
            Process.Exited += Process_Exited;
            if (File.Exists(Program.Config.Values.Connection.ServerPath))
                Process.StartInfo.FileName = Program.Config.Values.Connection.ServerPath;
            Process.StartInfo.Arguments = Program.Config.Values.Connection.ToString();

            IsReady = false;
            IsAlive = false;
            Stopping = false;

            Remote = new RemoteControl(Program.Config.Values.Remote.Password, Program.Config.Values.Remote.Port, this);
            Data = new ServerData(new IPEndPoint(IPAddress.Parse(Program.Config.Values.Connection.IP), Program.Config.Values.Connection.Port));

            if (Program.Config.Values.StartServerAtStartup)
                Start();

            if (Program.Config.Values.Stats.Activated)
            {
                drawgraph = new Thread(DrawGraph);
                drawgraph.Start();

                Stats = new Statistics();
            }

            waitidle = new Thread(WaitIdle);
            waitidle.Start();
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
        }

        public void Start()
        {
            if (IsAlive)
                return;

            Process.Start();
            IsAlive = true;
        }

        private void WaitIdle()
        {
            while (Thread.CurrentThread.IsAlive)
            {
                while (!IsAlive) { Thread.Sleep(100); }

                Process.WaitForInputIdle();
                if (Process.HasExited)
                {
                    IsAlive = false;
                    IsReady = false;
                    continue;
                }

                IsReady = true;
                Data.Refresh();
            }
        }

        public void Stop()
        {
            if (!IsAlive)
                return;

            IsAlive = false;
            IsReady = false;

            if (waitidle != null && waitidle.IsAlive)
                waitidle.Abort();

            Stopping = true;
            if (!Process.HasExited)
                Process.Kill();
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        public void DrawGraph()
        {
            while (Thread.CurrentThread.IsAlive)
            {
                Thread.Sleep((int)Program.Config.Values.Stats.MinuteDrawSample * 60 * 1000);
                while (!IsReady) { Thread.Sleep(1000); }
                Stats.AddPoint(DateTime.Now, Data.Info.Players);
            }
        }

        public void Dispose()
        {
            Process.Exited -= Process_Exited;
            waitidle.Abort();
        }
    }
}
