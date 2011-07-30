using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace BizHawk.MultiClient
{
	public class NullGame : RomGame
	{
		private List<string> options;
		private const int BankSize = 4096;
		new public RomStatus Status { get; private set; }
		new public string Name { get { return "Null Game"; } set { } }
		new public string FilesystemSafeName { get { return "Null Game"; } }

		public NullGame() 
		{
			FileData = new byte[1];
			FileData[0] = new byte();
			RomData = new byte[1];
			RomData[0] = new byte();
			System = "Null";
			Status = RomStatus.GoodDump;
			options = new List<String>();
			options.Add("null");
		}
		
		private byte[] DeInterleaveSMD(byte[] source)
		{
			return FileData;
		}

		private void CheckForPatchOptions()
		{
		}
		
		new public string SaveRamPath
		{
			get
			{
				return Path.Combine(PathManager.MakeAbsolutePath(Global.Config.BasePath, ""), "Null Game.SaveRAM");
			}
		}
		
		

		new public string SaveStatePrefix
		{
			get
			{
				string Bind = "";
				if (Global.Config.BindSavestatesToMovies && Global.MainForm.UserMovie.Mode != MOVIEMODE.INACTIVE)
					Bind += " - " + Path.GetFileNameWithoutExtension(Global.MainForm.UserMovie.Filename);
				
				return Path.Combine(PathManager.MakeAbsolutePath(Global.Config.BasePath, ""), "Null Game" + Bind);
			}
		}

		new public string MoviePrefix
		{
			get
			{
				return PathManager.MakeAbsolutePath(Global.Config.BasePath, "") + "Null Game";
			}
		}

		new public string ScreenshotPrefix
		{
			get
			{
				return PathManager.MakeAbsolutePath(Global.Config.BasePath, "") + "/" + "Null Game";
			}
		}
	}
}
