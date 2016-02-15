using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ServerManager
{
    public static class Program
    {
        public static Config Config { get; set; }
        public const string DEFAULT_FILE = "config.json";

        [STAThread]
        static void Main(string[] args)
        {
            Config = new Config(args.Length == 0 ? DEFAULT_FILE : File.Exists(args[0]) ? args[0] : DEFAULT_FILE);

            if (!Config.Check())
            {
                Config.Save(DEFAULT_FILE);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Forms.Creation());
            }
            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Forms.Main());
            }
        }
    }
}
