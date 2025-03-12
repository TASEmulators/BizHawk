using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public abstract class CInterface
	{
		public enum MemoryArrayType : int
		{
			Things = 0,
			Lines = 1,
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

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int load_archive_cb(string filename, IntPtr buffer, int maxsize);

		[StructLayout(LayoutKind.Sequential)]
		public struct InitSettings
		{
			public int _Player1Present;
			public int _Player2Present;
			public int _Player3Present;
			public int _Player4Present;
			public int _Player1Class;
			public int _Player2Class;
			public int _Player3Class;
			public int _Player4Class;
			public int _PreventLevelExit;
			public int _PreventGameEnd;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct PackedPlayerInput
		{
			public int _RunSpeed;
			public int _StrafingSpeed;
			public int _TurningSpeed;
			public int _WeaponSelect;
			public int _Fire;
			public int _Action;
			public int _Automap;

			// Hexen + Heretic (Raven Games)
			public int _FlyLook;
			public int _ArtifactUse;

			// Hexen only
			public int _Jump;
			public int _EndPlayer;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct PackedRenderInfo
		{
			public int _RenderVideo;
			public int _RenderAudio;
			public int _PlayerPointOfView;
		}

		[BizImport(CallingConvention.Cdecl)]
		public abstract void dsda_get_audio(ref int n, ref IntPtr buffer);

		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract bool dsda_init(ref InitSettings settings, int argc, string[] argv);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void dsda_frame_advance(
			ref PackedPlayerInput player1Inputs,
			ref PackedPlayerInput player2Inputs,
			ref PackedPlayerInput player3Inputs,
			ref PackedPlayerInput player4Inputs,
			ref PackedRenderInfo renderInfo);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void dsda_get_video(out int w, out int h, out int pitch, ref IntPtr buffer, out int palSize, ref IntPtr palBuffer);

		[BizImport(CallingConvention.Cdecl)]
		public abstract int dsda_add_wad_file(
			string fileName,
			int fileSize,
			load_archive_cb feload_archive_cb);
		
		[BizImport(CallingConvention.Cdecl)]
		public abstract byte dsda_read_memory_array(
			MemoryArrayType type,
			uint addr);
	}
}
