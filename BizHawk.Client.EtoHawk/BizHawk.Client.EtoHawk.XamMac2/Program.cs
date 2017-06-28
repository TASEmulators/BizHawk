using System;
using Eto;
using Eto.Forms;

/***
 * Note: This project is actually disabled in the build configuration due to a bug in Xamarin's mmp linker.
 * It won't build as of 2017-06-27
 * */

namespace BizHawk.Client.EtoHawk.XamMac2
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            new Application(Platforms.XamMac2).Run(new MainForm());
        }
    }
}
