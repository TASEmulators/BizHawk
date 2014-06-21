/*
 * GameProgram.cs
 * 
 * Represents attribute data associated with ROMs
 * 
 * Copyright 2003, 2004, 2010 © Mike Murphy
 * 
 */

/*
 * unlike EMU7800 Core stuff, this has been hacked around a bit
 */

using System.Text;
using EMU7800.Core;

namespace EMU7800.Win
{
    public class GameProgram
    {
        public string MD5 { get; set; }
        public string Title { get; set; }
        public string Manufacturer { get; set; }
        public string Author { get; set; }
        public string Year { get; set; }
        public string ModelNo { get; set; }
        public string Rarity { get; set; }
        public CartType CartType { get; set; }
        public MachineType MachineType { get; set; }
        public Controller LController { get; set; }
        public Controller RController { get; set; }
        public string HelpUri { get; set; }

        public string DiscoveredRomFullName { get; set; }

        public override string ToString()
        {
            var s = new StringBuilder("GameSettings:\n");
            s.AppendFormat(" MD5: {0}\n", MD5);
            s.AppendFormat(" Title: {0}\n", Title);
            s.AppendFormat(" Manufacturer: {0}\n", Manufacturer);
            s.AppendFormat(" Author: {0}\n", Author);
            s.AppendFormat(" Year: {0}\n", Year);
            s.AppendFormat(" ModelNo: {0}\n", ModelNo);
            s.AppendFormat(" Rarity: {0}\n", Rarity);
            s.AppendFormat(" CartType: {0}\n", CartType);
            s.AppendFormat(" MachineType: {0}\n", MachineType);
            s.AppendFormat(" LController: {0}\n", LController);
            s.AppendFormat(" RController: {0}\n", RController);
            s.AppendFormat(" HelpUri: {0}", HelpUri);
            if (DiscoveredRomFullName != null) s.AppendFormat("\n Discovered Rom Filename: {0}", DiscoveredRomFullName);
            return s.ToString();
        }

        public GameProgram(string md5)
        {
            MD5 = md5;
        }

		/// <summary>
		/// not in db, so guess
		/// </summary>
		/// <param name="md5"></param>
		/// <returns></returns>
		public static GameProgram GetCompleteGuess(string md5)
		{
			GameProgram ret = new GameProgram(md5);
			ret.Title = "UNKNOWN";
			//ret.CartType = CartType.A7848; // will be guessed for us
			ret.MachineType = MachineType.A7800NTSC;
			ret.LController = Controller.Joystick;
			ret.RController = Controller.Joystick;
			return ret;
		}
    }
}