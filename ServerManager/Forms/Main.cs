using ServerManager.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ServerManager.Forms
{
    class Main : Form, IDisposable
    {
        Server Server = new Server();

        private NotifyIcon notifyIcon;
        private ContextMenuStrip menu;
        private ToolStripMenuItem demarrer, arreter, options, executer, isvisible, rebootonfail, remote, information, quitter;
        private ToolStripTextBox command;
        private Color c = SystemColors.ControlLightLight;

        private Icon connected_wait = Icon.FromHandle(ServerManager.Properties.Resources.connected_wait.GetHicon());
        private Icon disconnected = Icon.FromHandle(ServerManager.Properties.Resources.disconnected.GetHicon());
        private Icon connected = Icon.FromHandle(ServerManager.Properties.Resources.connected.GetHicon());

        private Thread t;

        public Main()
        {
            this.Text = "ServerManager";
            this.Size = new Size(0, 0);
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.ShowIcon = false;
            this.BackColor = Color.Magenta;
            this.TransparencyKey = Color.Magenta;
            this.Opacity = 0;

            Form f = new Form();
            f.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            f.ShowInTaskbar = false;
            this.Owner = f;

            notifyIcon = new NotifyIcon();
            menu = new ContextMenuStrip();
            demarrer = new ToolStripMenuItem();
            arreter = new ToolStripMenuItem();
            options = new ToolStripMenuItem();
            executer = new ToolStripMenuItem();
            command = new ToolStripTextBox();
            isvisible = new ToolStripMenuItem();
            rebootonfail = new ToolStripMenuItem();
            remote = new ToolStripMenuItem();
            information = new ToolStripMenuItem();
            quitter = new ToolStripMenuItem();

            demarrer.Text = "Démarrer";
            demarrer.BackColor = c;
            demarrer.Image = ServerManager.Properties.Resources.start;
            demarrer.Click += (ob, ev) => 
            {
                isvisible.Image = ServerManager.Properties.Resources.visible;
                Server.Start();
            };

            arreter.Text = "Arrêter";
            arreter.BackColor = c;
            arreter.Image = ServerManager.Properties.Resources.shutdown;
            arreter.Click += (ob, ev) =>
            {
                isvisible.Image = ServerManager.Properties.Resources.invisible;
                if (Server.IsReady)
                {
                    Server.Stopping = true;
                    Utility.Sendkeys.Send(Server.Handle, "quit~");
                    Server.Process.WaitForExit(2500);
                }

                Server.Stop();
            };

            remote.Text = "Contrôle à distance";
            remote.BackColor = c;
            remote.Image = ServerManager.Properties.Resources.remote;
            remote.Checked = Program.Config.Values.Remote.Activated;
            remote.CheckOnClick = true;
            remote.Click += (ob, ev) =>
            {
                Program.Config.Values.Remote.Activated = remote.Checked;
                Server.Remote.ListenConnection = remote.Checked;
            };

            rebootonfail.Text = "Redémarrer en cas d'échec";
            rebootonfail.BackColor = c;
            rebootonfail.Image = ServerManager.Properties.Resources.restart;
            rebootonfail.Checked = Program.Config.Values.Connection.AutoRestart;
            rebootonfail.CheckOnClick = true;
            rebootonfail.Click += (ob, ev) =>
            {
                Program.Config.Values.Connection.AutoRestart = rebootonfail.Checked;
            };

            options.Text = "Options";
            options.BackColor = c;
            options.Image = ServerManager.Properties.Resources.settings;
            options.DropDown.Items.Add(rebootonfail);
            options.DropDown.Items.Add(remote);

            command.Text = "Commande ici";
            command.ForeColor = Color.Gray;
            command.GotFocus += (ob, ev) =>
            {
                if (command.Text == "Commande ici")
                    command.Text = "";
                command.ForeColor = Color.Black;
            };
            command.LostFocus += (ob, ev) =>
            {
                if (command.Text == "")
                    command.Text = "Commande ici";
                command.ForeColor = Color.Gray;
            };
            command.KeyDown += (ob, ev) =>
            {
                if (ev.KeyCode == Keys.Enter)
                {
                    Utility.Sendkeys.Send(Server.Handle, command.Text + "~");
                    command.Clear();
                    ev.Handled = ev.SuppressKeyPress = true;
                }
            };

            executer.Text = "Exécuter";
            executer.BackColor = c;
            executer.Image = ServerManager.Properties.Resources.execute;
            executer.DropDown.Items.Add(command);

            isvisible.Text = "Visibilité";
            isvisible.BackColor = c;
            isvisible.Image = ServerManager.Properties.Resources.invisible;
            isvisible.Click += (ob, ev) => 
            {
                isvisible.Image = Utility.View.ChangeVisibility(Server.Handle) ? ServerManager.Properties.Resources.visible : ServerManager.Properties.Resources.invisible;
            };

            information.Text = "Informations";
            information.BackColor = c;
            information.Image = ServerManager.Properties.Resources.information;
            information.Click += (ob, ev) =>
            {
                if (Server.IsReady && info != "" && title != "")
                    MessageBox.Show(this, info, title, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
            };

            quitter.Text = "Quitter";
            quitter.BackColor = c;
            quitter.Image = ServerManager.Properties.Resources.exit;
            quitter.Click += (ob, ev) =>
            {
                if (!Server.IsAlive)
                    Dispose();
                else
                {
                    DialogResult dr = MessageBox.Show("Voulez-vous arrêter le server en même temps?", "Confirmation", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                    if (dr == DialogResult.Yes)
                    {
                        if (Server.IsReady)
                            arreter.PerformClick();

                        Server.Stop();
                        Dispose();
                    }
                    else if (dr == DialogResult.No)
                        Dispose();
                    else if (dr == DialogResult.Cancel)
                        return;
                    Server.Stats.Save();
                }

                Program.Config.Save();
                Environment.Exit(0);
            };

            menu.Items.Add(demarrer);
            menu.Items.Add(arreter);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(options);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(executer);
            menu.Items.Add(isvisible);
            menu.Items.Add(information);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(quitter);

            foreach (ToolStripItem tsi in menu.Items)
                tsi.Enabled = false;
            demarrer.Enabled = true;
            options.Enabled = true;
            quitter.Enabled = true;

            notifyIcon.DoubleClick += (ob, ev) => 
            {
                if (!Server.IsAlive && title == "" && info == "")
                    Server.Start();
                else
                    isvisible.PerformClick();
            };

            t = new Thread(RefreshValues);
            this.Load += (ob, ev) => { t.Start(); };

            notifyIcon.Icon = disconnected;
            notifyIcon.ContextMenuStrip = menu;
            notifyIcon.Text = "ServerManager :: " + Path.GetFileName(Program.Config.Path);
            notifyIcon.Visible = true;
        }

        string title = "";
        string info = "";
        int refreshs = 0;
        private void RefreshValues()
        {
            while (Thread.CurrentThread.IsAlive)
            {
                Thread.Sleep(1000);

                try
                {
                    Invoke(new MethodInvoker(delegate
                    {
                        demarrer.Enabled = !Server.IsAlive;
                        arreter.Enabled = Server.IsAlive;
                        isvisible.Enabled = Server.IsAlive;
                        information.Enabled = Server.IsReady;
                        executer.Enabled = Server.IsReady;

                        notifyIcon.Icon = Server.IsAlive ? Server.IsReady ? connected : connected_wait : disconnected;

                        notifyIcon.Text = "ServerManager :: " + Path.GetFileName(Program.Config.Path);
                        title = "";
                        info = "";
                    }));

                    if (!Server.IsAlive)
                        continue;

                    if (!Server.IsReady)
                        continue;

                    if (!Server.Responding)
                    {
                        Server.Restart();
                        Invoke(new MethodInvoker(delegate
                        {
                            notifyIcon.ShowBalloonTip(5000, "Redémarrage", "Le serveur ne répondait plus et il a été redémarré!", ToolTipIcon.Warning);
                        }));

                        if (isvisible.Image == ServerManager.Properties.Resources.invisible)
                        {
                            while (Server.Handle == IntPtr.Zero) { Thread.Sleep(100); }
                            Utility.View.ChangeVisibility(Server.Handle);
                        }
                    }

                    Invoke(new MethodInvoker(delegate
                    {
                        notifyIcon.Text = Server.Data.Info.Name;
                        title = Server.Data.Info.Name;
                        info = "Carte : " + Server.Data.Info.Map + "\nMode de jeu : " + Server.Data.Info.Game + "\nPlaces : " + Server.Data.Info.Players + "/" + Server.Data.Info.MaxPlayers;
                    }));

                    if (Program.Config.Values.Remote.Activated && Server.Remote.Commands.Count > 0)
                        Utility.Sendkeys.Send(Server.Handle, Server.Remote.Commands.Dequeue() + "~");
                }
                catch { }

                refreshs++;
                if (refreshs % Program.Config.Values.RefreshRate == 0)
                    Server.Data.Refresh();
            }
        }

        protected override void Dispose(bool disposing)
        {
            DestroyIcon(connected_wait.Handle);
            DestroyIcon(connected.Handle);
            DestroyIcon(disconnected.Handle);
            
            base.Dispose(disposing);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);
    }
}
