using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerManager.Utility
{
    class RemoteControl
    {
        private byte[] PASSWORD_HEADER { get { return new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }; } } //"¬╗╠¦"
        private byte[] LOGGED { get { return new byte[] { 0xAB, 0xCD, 0xDE, 0xFA }; } } //"½═Ì·"
        private byte[] DENIED { get { return new byte[] { 0xBA, 0xDC, 0xED, 0xAF }; } } //"║▄Ý»"
        private byte[] NOT_ALIVE { get { return new byte[] { 0x1A, 0x2B, 0x3C, 0x4D }; } } //"+<M"
        private byte[] NOT_READY { get { return new byte[] { 0xA1, 0xB2, 0xC3, 0xD4 }; } } //í▓┬È
        private byte[] CMD_OK { get { return new byte[] { 0xF1, 0x1F, 0xF1, 0x1F }; } } //"±▼±▼"
        private byte[] CMD_BAD { get { return new byte[] { 0xE2, 0x2E, 0xE2, 0x2E }; } } //"Ô.Ô."
        private byte[] ENQUEUED { get { return new byte[] { 0x11, 0x22, 0x33, 0x44 }; } } //"◄"3D"
        private byte[] LOCAL_FLAG { get { return new byte[] { 0x01 }; } } //☺

        private string Password { get; set; } //•ayylmao
        private Server Server { get; set; }
        public ushort Port { get; private set; }
        public Queue<string> Commands { get; set; }
        public bool ListenConnection { get; set; }

        Thread listen;

        public RemoteControl(string password, ushort port, Server server)
        {
            Commands = new Queue<string>();
            Password = password;
            Port = port;
            Server = server;

            ListenConnection = Program.Config.Values.Remote.Activated;
            listen = new Thread(Listen);
            listen.Start();
        }

        public void Listen()
        {
            TcpListener tl = new TcpListener(IPAddress.Any, Port);
            tl.Start();

            while (Thread.CurrentThread.IsAlive)
            {
                using (Socket socket = tl.AcceptSocket())
                {
                    if (!ListenConnection) continue;

                    using (NetworkStream ns = new NetworkStream(socket))
                    {
                        byte[] buffer = new byte[1024];
                        ns.Read(buffer, 0, buffer.Length);

                        if (CheckPassword(buffer))
                        {
                            ns.Write(LOGGED, 0, LOGGED.Length);

                            buffer = new byte[1024];
                            ns.Read(buffer, 0, buffer.Length);

                            ns.Write(Parse(buffer), 0, 4);
                        }
                        else
                            ns.Write(DENIED, 0, DENIED.Length);
                    }
                }
            }
        }

        public byte[] Parse(byte[] buffer)
        {
            if (!CheckLocalFlag(buffer))
            {
                if (!Server.IsAlive)
                    return NOT_ALIVE;
                Commands.Enqueue(Encoding.UTF8.GetString(buffer).TrimEnd('\0').TrimEnd('\n'));
                if (Server.IsAlive && !Server.IsReady)
                    return NOT_READY;
                else
                    return ENQUEUED;
            }
            else
            {
                string cmd = Encoding.UTF8.GetString(buffer).TrimEnd('\0').TrimEnd('\n').Substring(LOCAL_FLAG.Length).ToLower();
                if (cmd == "start")
                    Server.Start();
                else if (cmd == "stop")
                    Server.Stop();
                else if (cmd == "restart")
                    Server.Restart();
                else if (cmd == "autorestarton")
                    Program.Config.Values.Connection.AutoRestart = true;
                else if (cmd == "autorestartoff")
                    Program.Config.Values.Connection.AutoRestart = false;
                else if (cmd == "remoteoff")
                    Server.Remote.ListenConnection = false;
                else if (cmd == "exit")
                    Environment.Exit(1);
                else if (cmd == "status")
                    return new byte[] { (byte)(Server.IsAlive ? 0x1 : 0x0), (byte)(Server.IsReady ? 0x1 : 0x0), (byte)(Program.Config.Values.Connection.AutoRestart ? 0x1 : 0x0), Program.Config.Values.RefreshRate };
                else
                    return CMD_BAD;
                return CMD_OK;
            }
        }

        public bool CheckLocalFlag(byte[] buffer)
        {
            for (int i = 0; i < LOCAL_FLAG.Length; i++)
                if (buffer[i] != LOCAL_FLAG[i])
                    return false;
            return true;
        }

        public bool CheckPassword(byte[] buffer)
        {
            byte password = 0;
            for (int i = 0; i < PASSWORD_HEADER.Length + 1; i++)
                if (i == PASSWORD_HEADER.Length)
                    password = buffer[i];
                else if (buffer[i] != PASSWORD_HEADER[i])
                    return false;
            for (int i = PASSWORD_HEADER.Length; i - PASSWORD_HEADER.Length < password; i++)
                if (i - PASSWORD_HEADER.Length >= Password.Length)
                    return false;
                else if (Password[i - PASSWORD_HEADER.Length] != buffer[i + 1])
                    return false;
            return true;
        }
    }
}
