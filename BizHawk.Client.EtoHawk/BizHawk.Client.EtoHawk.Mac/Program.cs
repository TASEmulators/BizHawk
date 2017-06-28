using System;
using Eto;
using Eto.Forms;

namespace BizHawk.Client.EtoHawk.Mac
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            new Application(Platforms.Mac).Run(new MainForm());
        }
    }
}
