using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public abstract class LibDSDA
	{
		public enum MemoryArrayType : int
		{
			Things  = 0,
			Lines   = 1,
			Sectors = 2
		}

		public enum GameMode : int
		{
			Fail         = 0,
			Shareware    = 1 << 0, // DOOM 1 shareware, E1, M9
			Registered   = 1 << 1, // DOOM 1 registered, E3, M27
			Commercial   = 1 << 2, // DOOM 2 retail, E1 M34  (DOOM 2 german edition not handled)
			Retail       = 1 << 3, // DOOM 1 retail, E4, M36
			Indetermined = 1 << 4  // no IWAD found.
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct InitSettings
		{
			public int Player1Present;
			public int Player2Present;
			public int Player3Present;
			public int Player4Present;
			public int Player1Class;
			public int Player2Class;
			public int Player3Class;
			public int Player4Class;
			public int PreventLevelExit;
			public int PreventGameEnd;
			//public uint RNGSeed;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct PackedPlayerInput
		{
			public int RunSpeed;
			public int StrafingSpeed;
			public int TurningSpeed;
			public int WeaponSelect;
			public int Buttons;

			// Hexen + Heretic (Raven Games)
			public int FlyLook;
			public int ArtifactUse;

			// Hexen only
			public int Jump;
			public int EndPlayer;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct PackedRenderInfo
		{
			public int RenderVideo;
			public int RenderAudio;
			public int SfxVolume;
			public int MusicVolume;
			public int Gamma;
			public int ShowMessages;
			public int ReportSecrets;
			public int HeadsUpMode;
			public int DsdaExHud;
			public int DisplayCoordinates;
			public int DisplayCommands;
			public int MapTotals;
			public int MapTime;
			public int MapCoordinates;
			public int MapDetails;
			public int MapOverlay;
			public int PlayerPointOfView;
		}

		[BizImport(CallingConvention.Cdecl)]
		public abstract void dsda_get_audio(ref int n, ref IntPtr buffer);

		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract bool dsda_init(ref InitSettings settings, int argc, string[] argv);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void dsda_get_video(out int w, out int h, out int pitch, ref IntPtr buffer, out int palSize, ref IntPtr palBuffer);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int load_archive_cb(string filename, IntPtr buffer, int maxsize);
		[BizImport(CallingConvention.Cdecl)]
		public abstract int dsda_add_wad_file(string fileName, int fileSize, load_archive_cb feload_archive_cb);

		[BizImport(CallingConvention.Cdecl)]
		public abstract byte dsda_read_memory_array(MemoryArrayType type, uint addr);

		[BizImport(CallingConvention.Cdecl)]
		public abstract bool dsda_frame_advance(
			int commonInputs,
			ref PackedPlayerInput player1Inputs,
			ref PackedPlayerInput player2Inputs,
			ref PackedPlayerInput player3Inputs,
			ref PackedPlayerInput player4Inputs,
			ref PackedRenderInfo renderInfo);
	}
}
