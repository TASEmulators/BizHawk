using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public abstract class LibDSDA
	{
		public enum MemoryArrayType : int
		{
			Players = 0,
			Things  = 1,
			Lines   = 2,
			Sectors = 3
		}

		public enum GameMode : int
		{
			Fail         = 0b0000_0000,
			Shareware    = 0b0000_0001, // DOOM 1 shareware, E1, M9
			Registered   = 0b0000_0010, // DOOM 1 registered, E3, M27
			Commercial   = 0b0000_0100, // DOOM 2 retail, E1 M34  (DOOM 2 german edition not handled)
			Retail       = 0b0000_1000, // DOOM 1 retail, E4, M36
			Indetermined = 0b0001_0000, // no IWAD found.
		}

#pragma warning disable RCS1191
		[Flags]
		public enum Buttons : int
		{
			None           = 0b0000_0000_0000_0000,
			Fire           = 0b0000_0000_0000_0001,
			Use            = 0b0000_0000_0000_0010,
			ChangeWeapon   = 0b0000_0000_0000_0100,
			WeaponMask     = 0b0000_0000_0011_1000,
			InventoryLeft  = 0b0000_0000_0000_1000,
			InventoryRight = 0b0000_0000_0001_0000,
			InventorySkip  = 0b0000_0000_0010_0000,
			ArtifactUse    = 0b0000_0000_0100_0000,
			LookUp         = 0b0000_0000_1000_0000,
			LookDown       = 0b0000_0001_0000_0000,
			LookCenter     = 0b0000_0010_0000_0000,
			FlyUp          = 0b0000_0100_0000_0000,
			FlyDown        = 0b0000_1000_0000_0000,
			FlyCenter      = 0b0001_0000_0000_0000,
			EndPlayer      = 0b0000_0000_0100_0000,
			Jump           = 0b0000_0000_1000_0000,
			ArtifactMask   = 0b0000_0000_0011_1111,
		}
#pragma warning restore RCS1191

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
			public int FullVision;
			public int DisplayPlayer;
			//public uint RNGSeed;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct PackedPlayerInput
		{
			public int RunSpeed;
			public int StrafingSpeed;
			public int TurningSpeed;
			public int WeaponSelect;
			public Buttons Buttons;

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

		[StructLayout(LayoutKind.Sequential)]
		public struct VideoInfo
		{
			public int Width;
			public int Height;
			public int Pitch;
			public int PaletteSize;
			public IntPtr PaletteBuffer;
			public IntPtr VideoBuffer;
		}

		[BizImport(CallingConvention.Cdecl)]
		public abstract void dsda_get_audio(ref int n, ref IntPtr buffer);

		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract bool dsda_init(ref InitSettings settings, int argc, string[] argv);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void dsda_init_video(ref PackedRenderInfo renderInfo);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void dsda_get_video(out VideoInfo videoInfo);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int load_archive_cb(string filename, IntPtr buffer, int maxsize);
		[BizImport(CallingConvention.Cdecl)]
		public abstract GameMode dsda_add_wad_file(string fileName, int fileSize, load_archive_cb feload_archive_cb);

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
