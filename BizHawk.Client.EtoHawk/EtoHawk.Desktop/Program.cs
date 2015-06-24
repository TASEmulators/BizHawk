using System;
using Eto.Forms;
using BizHawk.Client.EtoHawk;

namespace EtoHawk.Desktop
{
    public class Program
    {
        [STAThread]
        public static void Main (string[] args)
        {
            //Force WinForms temporarily, because the WPF is slow with the non accelerated drawing we're doing at the moment.
            //Switch back to AutoDetect, which will use WPF, when the performance doesn't suck.
            new Application(Eto.Platforms.WinForms).Run(new MainForm());
            //new Application (Eto.Platform.Detect).Run (new MainForm ());
        }
    }
}

