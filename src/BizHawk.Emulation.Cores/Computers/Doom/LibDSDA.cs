using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public abstract class CInterface
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int load_archive_cb(string filename, IntPtr buffer, int maxsize);

		[StructLayout(LayoutKind.Sequential)]
		public class InitSettings
		{
			public int _Player1Present;
			public int _Player2Present;
			public int _Player3Present;
			public int _Player4Present;
			public int _CompatibilityMode;
			public int _SkillLevel;
			public int _InitialEpisode;
			public int _InitialMap;
			public int _Turbo;
			public int _FastMonsters;
			public int _MonstersRespawn;
			public int _NoMonsters;
			public int _PlayerClass;
			public int _ChainEpisodes;
			public int _StrictMode;
			public int _PreventLevelExit;
			public int _PreventGameEnd;
		}

		[StructLayout(LayoutKind.Sequential)]
		public class PackedPlayerInput
		{
			public int _RunSpeed;
			public int _StrafingSpeed;
			public int _TurningSpeed;
			public int _FlyLook;
			public int _WeaponSelect;
			public int _ArtifactUse;
			public int _Fire;
			public int _Action;
			public int _AltWeapon;
		}

		[BizImport(CallingConvention.Cdecl)]
		public abstract void dsda_get_audio(ref int n, ref IntPtr buffer);

		[BizImport(CallingConvention.Cdecl)]
		public abstract bool dsda_init([In] InitSettings settings);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void dsda_frame_advance([In] PackedPlayerInput player1Inputs, [In] PackedPlayerInput player2Inputs, [In] PackedPlayerInput player3Inputs, [In] PackedPlayerInput player4Inputs, int renderVideo, int renderSound);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void dsda_get_video(out int w, out int h, out int pitch, ref IntPtr buffer, out int palSize, ref IntPtr palBuffer);

		[BizImport(CallingConvention.Cdecl)]
		public abstract bool dsda_add_wad_file(
			string fileName,
			int fileSize,
			load_archive_cb feload_archive_cb);
	}
}
