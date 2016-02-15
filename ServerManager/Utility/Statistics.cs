using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerManager.Utility
{
    class Statistics
    {
        public Image Graphique { get; set; }
        static int width = 1600, height = 800;
        static int x_bars = 24, y_bars = Program.Config.Values.Connection.Arguments.MaxPlayers + 1;
        static float y_tab = 0, x_mod = width / x_bars, y_mod = height / y_bars;
        PointF last_p = PointF.Empty;
        DateTime dt_start;

        public Statistics()
        {
            dt_start = DateTime.Now;
            DrawGraphBackground();
        }

        public void DrawGraphBackground()
        {
            Graphique = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(Graphique))
            {
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.Clear(Color.White);
                using (Pen p = new Pen(Color.LightGray))
                    for (int x = 0; x < x_bars; x++)
                        g.DrawLine(p, x * x_mod, 0f, x * x_mod, height);

                using (SolidBrush sb = new SolidBrush(Color.Black))
                using (FontFamily ff = new FontFamily("Trebuchet MS"))
                using (Font f = new Font(ff, 10, FontStyle.Bold))
                {
                    for (int h = 0, t = 0; h < x_bars; h++, t++)
                    {
                        if (t == 24)
                            t = 0;
                        string th = "- " + (t < 10 ? "0" + t : t + "") + ":00 ->";
                        SizeF s = g.MeasureString(th, f);

                        if (h == 0)
                        {
                            y_tab = height / y_bars - s.Height;
                            using (Pen p = new Pen(Color.LightGray))
                                for (int y = 0; y < y_bars; y++)
                                    g.DrawLine(p, 0f, y_tab + y * y_mod, width, y_tab + y * y_mod);
                            using (Pen p = new Pen(Color.Black))
                                g.DrawLine(p, 0f, height - s.Height, width, height - s.Height);
                        }

                        g.DrawString(th, f, sb, h * x_mod + (x_mod / 2 - s.Width / 2), height - s.Height);
                    }

                    g.DrawString(DateTime.Now.ToString("yyyy-MM-dd"), f, sb, 0, 0);
                }
            }
        }

        public void AddPoint(DateTime dt, int nb)
        {
            TimeSpan ts = dt.Subtract(dt_start);
            PointF new_p = new PointF((float)ts.TotalMinutes * x_mod / 60f, y_tab + height - (y_mod * (nb + 1f)));
            if (last_p == PointF.Empty)
                last_p = new_p;

            using (Graphics g = Graphics.FromImage(Graphique))
            {
                using (Pen p = new Pen(Color.Red, 4f))
                    g.DrawLine(p, last_p, new_p);
                using (SolidBrush sb = new SolidBrush(Color.Black))
                using (FontFamily ff = new FontFamily("Trebuchet MS"))
                using (Font f = new Font(ff, 8, FontStyle.Regular))
                    g.DrawString(nb + "", f, sb, new_p);
            }

            if (new_p.X > width)
            {
                Save();
                DrawGraphBackground();

                new_p.X = new_p.X - width;
                last_p.X = last_p.X - width;

                AddPoint(dt, nb);
            }
            else
                last_p = new_p;
        }

        public void Save()
        {
            string img_name = Path.GetFileNameWithoutExtension(Program.Config.Path), oldname = img_name;
            int i = 1;
            while (File.Exists(oldname + ".png")) { oldname = img_name + "_" + i; i++; }
            Graphique.Save(oldname + ".png", ImageFormat.Png);
        }
    }
}
