using System;
using Eto.Forms;

namespace EtoHawk.Mac
{
    public class Program
    {
        [STAThread]
        public static void Main (string[] args)
        {
            new Application (Eto.Platforms.Mac).Run (new MainForm ());
        }
    }
}

