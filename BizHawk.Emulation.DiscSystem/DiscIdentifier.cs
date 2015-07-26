using System;
using System.Collections.Generic;

//disc type identification logic

namespace BizHawk.Emulation.DiscSystem
{
	public enum DiscType
	{
		/// <summary>
		/// Disc contains audio in track 1. Nothing more can readily be determined
		/// </summary>
		AudioDisc,

		/// <summary>
		/// Nothing is known about this data disc type
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
		TurboCD,

		/// <summary>
		/// MegaDrive addon
		/// </summary>
		MegaCD
	}

	public class DiscIdentifier
	{
		public DiscIdentifier(Disc disc)
		{
			this.disc = disc;
			dsr = new DiscSectorReader(disc);
			
			//the first check for mode 0 should be sufficient for blocking attempts to read audio sectors, so dont do this
			//dsr.Policy.ThrowExceptions2048 = false;
		}

		Disc disc;
		DiscSectorReader dsr;
		Dictionary<int, byte[]> sectorCache = new Dictionary<int,byte[]>();
		
		/// <summary>
		/// Attempts to determine the type of the disc.
		/// In the future, we might return a struct or a class with more detailed information
		/// </summary>
		public DiscType DetectDiscType()
		{
			//check track 1's data type. if it's an audio track, further data-track testing is useless
			//furthermore, it's probably senseless (no binary data there to read)
			//however a sector could mark itself as audio without actually being.. we'll just wait for that one.
			if (dsr.ReadLBA_Mode(disc.TOC.TOCItems[1].LBA) == 0) return DiscType.AudioDisc;

			//sega doesnt put anything identifying in the cdfs volume info. but its consistent about putting its own header here in sector 0
			if (DetectSegaSaturn()) return DiscType.SegaSaturn;

			// not fully tested yet
			if (DetectMegaCD()) return DiscType.MegaCD;

			// not fully tested yet
			if (DetectPSX()) return DiscType.SonyPSX;

			//we dont know how to detect TurboCD.
			//an emulator frontend will likely just guess TurboCD if the disc is UnknownFormat
			//(we can also have a gameDB!)

			var discView = EDiscStreamView.DiscStreamView_Mode1_2048;
			if (disc.TOC.Session1Format == SessionFormat.Type20_CDXA)
				discView = EDiscStreamView.DiscStreamView_Mode2_Form1_2048;

			var iso = new ISOFile();
			bool isIso = iso.Parse(new DiscStream(disc, discView, 0));

			if (isIso)
			{
				var appId = System.Text.Encoding.ASCII.GetString(iso.VolumeDescriptors[0].ApplicationIdentifier).TrimEnd('\0', ' ');
				
				//for example: PSX magical drop F (JP SLPS_02337) doesn't have the correct iso PVD fields
				//but, some PSX games (junky rips) don't have the 'licensed by string' so we'll hope they get caught here
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
			return StringAt("SEGA SEGASATURN", 0);
		}

		/// <summary>
		/// probably wrong
		/// </summary>
		bool DetectMegaCD()
		{
			return StringAt("SEGADISCSYSTEM", 0) || StringAt("SEGADISCSYSTEM", 16);
		}

		bool DetectPSX()
		{
			if (!StringAt("          Licensed  by          ",0, 4)) return false;
			return (StringAt("Sony Computer Entertainment Euro", 32, 4)
				|| StringAt("Sony Computer Entertainment Inc.", 32, 4)
				|| StringAt("Sony Computer Entertainment Amer", 32, 4)
				|| StringAt("Sony Computer Entertainment of A", 32, 4)
				);
		}

		private bool StringAt(string s, int n, int lba = 0)
		{
			//read it if we dont have it cached
			//we wont be caching very much here, it's no big deal
			//identification is not something we want to take a long time
			byte[] data;
			if (!sectorCache.TryGetValue(lba, out data))
			{
				data = new byte[2048];
				dsr.ReadLBA_2048(lba, data, 0);
				sectorCache[lba] = data;
			}

			byte[] cmp = System.Text.Encoding.ASCII.GetBytes(s);
			byte[] cmp2 = new byte[cmp.Length];
			Buffer.BlockCopy(data, n, cmp2, 0, cmp.Length);
			return System.Linq.Enumerable.SequenceEqual(cmp, cmp2);
		}
	}
}