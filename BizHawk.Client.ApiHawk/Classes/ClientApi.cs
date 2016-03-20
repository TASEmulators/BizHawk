using System;
using System.Reflection;

namespace BizHawk.Client.ApiHawk
{
	/// <summary>
	/// This class contains some methods that
	/// interract with BizHawk client
	/// </summary>
	public static class ClientApi
	{
		#region Fields

		private static readonly Assembly clientAssembly;

		public static event EventHandler RomLoaded;

		#endregion

		#region cTor(s)

		static ClientApi()
		{
			clientAssembly = Assembly.GetEntryAssembly();
		}

		#endregion

		#region Methods

		/*public static void DoframeAdvance()
		{
			//StepRunLoop_Core
			Type emuLuaLib = clientAssembly.GetType("BizHawk.Client.EmuHawk.MainForm");
			//clientAssembly
			MethodInfo paddingMethod = emuLuaLib.GetMethod("FrameAdvance");
			paddingMethod.Invoke(paddingMethod, null);
		}*/		

		/// <summary>
		/// Raise when a rom is successfully Loaded
		/// </summary>
		public static void OnRomLoaded()
		{
			if (RomLoaded != null)
			{
				RomLoaded(null, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		/// <param name="top">Top padding</param>
		/// <param name="right">Right padding</param>
		/// <param name="bottom">Bottom padding</param>
		public static void SetExtraPadding(int left, int top, int right, int bottom)
		{
			Type emuLuaLib = clientAssembly.GetType("BizHawk.Client.EmuHawk.EmuHawkLuaLibrary");
			MethodInfo paddingMethod = emuLuaLib.GetMethod("SetClientExtraPadding");
			paddingMethod.Invoke(paddingMethod, new object[] { left, top, right, bottom });
		}

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		public static void SetExtraPadding(int left)
		{
			SetExtraPadding(left, 0, 0, 0);
		}

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		/// <param name="top">Top padding</param>
		public static void SetExtraPadding(int left, int top)
		{
			SetExtraPadding(left, top, 0, 0);
		}

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		/// <param name="top">Top padding</param>
		/// <param name="right">Right padding</param>
		public static void SetExtraPadding(int left, int top, int right)
		{
			SetExtraPadding(left, top, right, 0);
		}

		/// <summary>
		/// Convert a specified <see cref="EmulatedSystem"/> into a <see cref="string"/> used in BizHawk internal code
		/// </summary>
		/// <param name="system"><see cref="EmulatedSystem"/> to convert</param>
		/// <returns>Emulated system as <see cref="string"/> used in BizHawk code</returns>
		internal static string EmulatedSytemEnumToBizhawkString(EmulatedSystem system)
		{
			switch (system)
			{
				case EmulatedSystem.AppleII:
					return "AppleII";

				case EmulatedSystem.Atari2600:
					return "A26";

				case EmulatedSystem.Atari7800:
					return "A78";

				case EmulatedSystem.ColecoVision:
					return "Coleco";

				case EmulatedSystem.Commodore64:
					return "C64";

				case EmulatedSystem.DualGameBoy:
					return "DGB";

				case EmulatedSystem.GameBoy:
					return "GB";

				case EmulatedSystem.GameBoyAdvance:
					return "GBA";

				case EmulatedSystem.Genesis:
					return "GEN";

				case EmulatedSystem.Intellivision:
					return "INTV";

				case EmulatedSystem.Libretro:
					return "Libretro";

				case EmulatedSystem.Lynx:
					return "Lynx";

				case EmulatedSystem.MasterSystem:
					return "SMS";

				case EmulatedSystem.NES:
					return "NES";

				case EmulatedSystem.Nintendo64:
					return "N64";

				case EmulatedSystem.Null:
					return "NULL";

				case EmulatedSystem.PCEngine:
					return "PCE";

				case EmulatedSystem.Playstation:
					return "PSX";

				case EmulatedSystem.PSP:
					return "PSP";

				case EmulatedSystem.Saturn:
					return "SAT";

				case EmulatedSystem.SNES:
					return "SNES";

				case EmulatedSystem.TI83:
					return "TI83";

				case EmulatedSystem.WonderSwan:
					return "WSWAN";

				default:
					throw new IndexOutOfRangeException(string.Format("{0} is missing in convert list", system.ToString()));
			}		
		}

		/// <summary>
		/// Convert a BizHawk <see cref="string"/> to <see cref="EmulatedSystem"/>
		/// </summary>
		/// <param name="system">BizHawk systemId to convert</param>
		/// <returns>SytemID as <see cref="EmulatedSystem"/> enum</returns>
		internal static EmulatedSystem BizHawkStringToEmulatedSytemEnum(string system)
		{
			switch(system)
			{
				case "AppleII":
					return EmulatedSystem.AppleII;

				case "A26":
					return EmulatedSystem.Atari2600;

				case "A78":
					return EmulatedSystem.Atari2600;

				case "Coleco":
					return EmulatedSystem.ColecoVision;

				case "C64":
					return EmulatedSystem.Commodore64;

				case "DGB":
					return EmulatedSystem.DualGameBoy;

				case "GB":
					return EmulatedSystem.GameBoy;

				case "GBA":
					return EmulatedSystem.GameBoyAdvance;

				case "GEN":
					return EmulatedSystem.Genesis;

				case "INTV":
					return EmulatedSystem.Intellivision;

				case "Libretro":
					return EmulatedSystem.Libretro;

				case "Lynx":
					return EmulatedSystem.Lynx;

				case "SMS":
					return EmulatedSystem.MasterSystem;

				case "NES":
					return EmulatedSystem.NES;

				case "N64":
					return EmulatedSystem.Nintendo64;

				case "NULL":
					return EmulatedSystem.Null;

				case "PCE":
					return EmulatedSystem.PCEngine;

				case "PSX":
					return EmulatedSystem.Playstation;

				case "PSP":
					return EmulatedSystem.PSP;

				case "SAT":
					return EmulatedSystem.Saturn;

				case "SNES":
					return EmulatedSystem.SNES;

				case "TI83":
					return EmulatedSystem.TI83;

				case "WSWAN":
					return EmulatedSystem.WonderSwan;

				default:
					throw new IndexOutOfRangeException(string.Format("{0} is missing in convert list", system));
			}
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets current emulated system id as <see cref="EmulatedSystem"/> enum
		/// </summary>
		public static EmulatedSystem RunningSystem
		{
			get
			{
				return BizHawkStringToEmulatedSytemEnum(Common.Global.Emulator.SystemId);
			}
		}

		#endregion
	}
}
