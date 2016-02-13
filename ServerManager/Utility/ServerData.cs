using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Steam.Query;

namespace ServerManager.Utility
{
    class ServerData
    {
        public Steam.Query.Server Server { get; private set; }
        public ServerInfoResult Info { get; private set; }
        public ServerRulesResult Rules { get; private set; }

        public ServerData(IPEndPoint ep)
        {
            Server = new Steam.Query.Server(ep);
        }

        public void Refresh()
        {
            try
            {
                Info = Server.GetServerInfoSync(new Steam.Query.Server.GetServerInfoSettings());
                Rules = Server.GetServerRulesSync(new Steam.Query.Server.GetServerInfoSettings());
            }
            catch { }
        }
    }
}
