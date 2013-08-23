using System;
using System.Collections.Generic;

//disc type identification logic

namespace BizHawk.DiscSystem
{
	public enum DiscType
	{
		/// <summary>
		/// Nothing is known about this disc type
		/// </summary>
		UnknownFormat,

		/// <summary>
		/// This is definitely a CDFS disc, but we can't identify anything more about it
		/// </summary>
		UnknownCDFS,
		
		/// <summary>
		/// Sony PSX
		/// </summary>
		SonyPSX,
		
		/// <summary>
		/// Sony PSP
		/// </summary>
		SonyPSP,
		
		/// <summary>
		/// Sega Saturn
		/// </summary>
		SegaSaturn,
		
		/// <summary>
		/// Its not clear whether we can ever have enough info to ID a turboCD disc (we're using hashes)
		/// </summary>
		TurboCD
	}

	public partial class Disc
	{
		/// <summary>
		/// Attempts to determine the type of the disc.
		/// In the future, we might return a struct or a class with more detailed information
		/// </summary>
		public DiscType DetectDiscType()
		{
			//sega doesnt put anything identifying in the cdfs volume info. but its consistent about putting its own header here in sector 0
			if (DetectSegaSaturn()) return DiscType.SegaSaturn;

			//we dont know how to detect TurboCD.
			//an emulator frontend will likely just guess TurboCD if the disc is UnknownFormat

			var iso = new ISOParser.ISOFile();
			bool isIso = iso.Parse(DiscStream.Open_LBA_2048(this));

			if (isIso)
			{
				var appId = System.Text.Encoding.ASCII.GetString(iso.VolumeDescriptors[0].ApplicationIdentifier).TrimEnd('\0', ' ');
				if (appId == "PLAYSTATION")
					return DiscType.SonyPSX;
				if(appId == "PSP GAME")
					return DiscType.SonyPSP;
						
				return DiscType.UnknownCDFS;
			}

			return DiscType.UnknownFormat;
		}

		/// <summary>
		/// This is reasonable approach to ID saturn.
		/// </summary>
		bool DetectSegaSaturn()
		{
			byte[] data = new byte[2048];
			ReadLBA_2048(0, data, 0);
			byte[] cmp = System.Text.Encoding.ASCII.GetBytes("SEGA SEGASATURN");
			byte[] cmp2 = new byte[15];
			Buffer.BlockCopy(data, 0, cmp2, 0, 15);
			return System.Linq.Enumerable.SequenceEqual(cmp, cmp2);
		}
	}
}