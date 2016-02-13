using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ServerManager
{
    public class Config
    {
        public string Path { get; set; }
        private Data values = null;
        public Data Values
        {
            get
            {
                if (values == null)
                    values = Load();
                return values;
            }
        }

        public Config(string file)
        {
            Path = file;
        }

        public Data Load()
        {
            try
            {
                if (File.Exists(Path))
                    using (StreamReader sr = new StreamReader(Path))
                        return JsonConvert.DeserializeObject<Data>(sr.ReadToEnd());
                else
                    return Data.Empty;
            }
            catch
            {
                return Data.Empty;
            }
        }

        public void Save(string path = "")
        {
            using (StreamWriter sw = new StreamWriter(path == "" ? Path : path))
                sw.WriteLine(JsonConvert.SerializeObject(Values, Formatting.Indented));
        }

        public bool Check()
        {
            return Values.CheckAll();
        }

        [Serializable]
        public class Data
        {
            public static Data Empty
            {
                get
                {
                    return new Data()
                    {
                        StartAtWindowsStartup = false,
                        StartServerAtStartup = false,
                        RefreshRate = 0,
                        Remote = new Data.RemoteControl()
                        {
                            Activated = false,
                            Password = null,
                            Port = 0
                        },
                        Connection = new Data.ServerConnection()
                        {
                            ServerPath = null,
                            IP = null,
                            Port = 0,
                            Arguments = new Data.ServerConnection.ServerArguments()
                            {
                                Game = null,
                                MaxPlayers = 0,
                                GameMode = null,
                                Map = null,
                                AuthKey = null,
                                Workshop = null
                            }
                        }
                    };
                }
            }

            public bool StartAtWindowsStartup { get; set; }
            public bool StartServerAtStartup { get; set; }
            public byte RefreshRate { get; set; }
            public RemoteControl Remote { get; set; }
            public ServerConnection Connection { get; set; }

            public bool CheckAll()
            {
                if (Connection == null || Connection.Arguments == null || Remote == null)
                    return false;
                return RefreshRate > 0 && Remote.Check() && Connection.Check() && Connection.Arguments.Check();
            }

            [Serializable]
            public class RemoteControl
            {
                public bool Activated { get; set; }
                public string Password { get; set; }
                public ushort Port { get; set; }

                public bool Check()
                {
                    return (Port >= 1024 && Port <= 49151);
                }
            }

            [Serializable]
            public class ServerConnection
            {
                public string ServerPath { get; set; }
                public ServerArguments Arguments { get; set; }
                public bool AutoRestart { get; set; }
                public string IP { get; set; }
                public ushort Port { get; set; }

                public override string ToString()
                {
                    return Arguments.ToString().Replace("{port}", Port.ToString());
                }

                public bool Check()
                {
                    IPAddress unused;
                    return File.Exists(ServerPath) && IPAddress.TryParse(IP, out unused) && (Port >= 1024 && Port <= 49151);
                }

                [Serializable]
                public class ServerArguments
                {
                    public string Game { get; set; }
                    public byte MaxPlayers { get; set; }
                    public string GameMode { get; set; }
                    public string Map { get; set; }
                    public string AuthKey { get; set; }
                    public string Workshop { get; set; }

                    public override string ToString()
                    {
                        return "-console -nocrashdialog -port {port} +maxplayers " + MaxPlayers + " +map " + Map + " +gamemode " + GameMode + " +host_workshop_collection " + Workshop + " -authkey " + AuthKey;
                    }

                    public bool Check()
                    {
                        return !string.IsNullOrEmpty(Game) && (MaxPlayers >= 1 && MaxPlayers <= 128);
                    }
                }
            }
        }
    }
}
