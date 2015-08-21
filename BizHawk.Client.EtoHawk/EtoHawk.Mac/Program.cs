using System;
using Eto;
using Eto.Forms;
using BizHawk.Client.EtoHawk;

namespace EtoHawk.Mac
{
    public class Program
    {
        [STAThread]
        public static void Main (string[] args)
        {
            Style.Add<Eto.Mac.Forms.ApplicationHandler>(null, h => h.AddFullScreenMenuItem = true);
            Style.Add<Eto.Mac.Forms.ApplicationHandler>(null, h => h.AllowClosingMainForm = true);

            new Application (Eto.Platforms.Mac).Run (new MainForm ());
        }
    }
}

