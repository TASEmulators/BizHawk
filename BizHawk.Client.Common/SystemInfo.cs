using System.Collections.Generic;
using BizHawk.Client.ApiHawk;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// This class holds logic for System information.
	/// That means specifiactions about a system that BizHawk emulate
	/// </summary>
	public sealed class SystemInfo
	{
		#region Fields

		private const JoypadButton UpDownLeftRight = JoypadButton.Up | JoypadButton.Down | JoypadButton.Left | JoypadButton.Right;
		private const JoypadButton StandardButtons = JoypadButton.A | JoypadButton.B | JoypadButton.Start | JoypadButton.Select | UpDownLeftRight;

		private static readonly List<SystemInfo> allSystemInfos;

		private string _DisplayName;
		private CoreSystem _System;
		private JoypadButton _AvailableButtons;
		private int _MaxControllers;

		#endregion

		#region cTor(s)

		/// <summary>
		/// Global initialization stuff
		/// </summary>
		/// <remarks>DO NOT CHANGE List order because properties depends on it (and it is hardcoded)</remarks>
		static SystemInfo()
		{
			allSystemInfos = new List<SystemInfo>(26);

			allSystemInfos.Add(new SystemInfo(string.Empty));
			allSystemInfos.Add(new SystemInfo("NES", CoreSystem.NES, 2, StandardButtons));
			allSystemInfos.Add(new SystemInfo("Intellivision", CoreSystem.Intellivision, 2));
			allSystemInfos.Add(new SystemInfo("Sega Master System", CoreSystem.MasterSystem, 2, UpDownLeftRight | JoypadButton.B1 | JoypadButton.B2));
			allSystemInfos.Add(new SystemInfo("SG-1000", CoreSystem.MasterSystem, 1));
			allSystemInfos.Add(new SystemInfo("Game Gear", CoreSystem.MasterSystem, 1, UpDownLeftRight | JoypadButton.B1 | JoypadButton.B2));
			allSystemInfos.Add(new SystemInfo("TurboGrafx-16", CoreSystem.PCEngine, 1));
			allSystemInfos.Add(new SystemInfo("TurboGrafx - 16(CD)", CoreSystem.PCEngine, 1));
			allSystemInfos.Add(new SystemInfo("SuperGrafx", CoreSystem.PCEngine, 1));
			allSystemInfos.Add(new SystemInfo("Genesis", CoreSystem.Genesis, 2, UpDownLeftRight | JoypadButton.A | JoypadButton.B | JoypadButton.C | JoypadButton.X | JoypadButton.Y | JoypadButton.Z));
			allSystemInfos.Add(new SystemInfo("TI - 83", CoreSystem.TI83, 1));
			allSystemInfos.Add(new SystemInfo("SNES", CoreSystem.SNES, 8, StandardButtons | JoypadButton.X | JoypadButton.Y | JoypadButton.L | JoypadButton.R));
			allSystemInfos.Add(new SystemInfo("GB", CoreSystem.GameBoy, 1, StandardButtons));
			allSystemInfos.Add(new SystemInfo("Gameboy Color", CoreSystem.GameBoy, 1, StandardButtons)); //13 (0 based)
			allSystemInfos.Add(new SystemInfo("Atari 2600", CoreSystem.Atari2600, 1));
			allSystemInfos.Add(new SystemInfo("Atari 7800", CoreSystem.Atari7800, 1));
			allSystemInfos.Add(new SystemInfo("Commodore 64", CoreSystem.Commodore64, 1));
			allSystemInfos.Add(new SystemInfo("ColecoVision", CoreSystem.ColecoVision, 1));
			allSystemInfos.Add(new SystemInfo("Gameboy Advance", CoreSystem.GameBoyAdvance, 1, StandardButtons | JoypadButton.L | JoypadButton.R));
			allSystemInfos.Add(new SystemInfo("Nintendo 64", CoreSystem.Nintendo64, 4, StandardButtons ^ JoypadButton.Select | JoypadButton.Z | JoypadButton.CUp | JoypadButton.CDown | JoypadButton.CLeft | JoypadButton.CRight | JoypadButton.AnalogStick | JoypadButton.L | JoypadButton.R));
			allSystemInfos.Add(new SystemInfo("Saturn", CoreSystem.Saturn, 2, UpDownLeftRight | JoypadButton.A | JoypadButton.B | JoypadButton.C | JoypadButton.X | JoypadButton.Y | JoypadButton.Z));
			allSystemInfos.Add(new SystemInfo("Game Boy Link", CoreSystem.DualGameBoy, 2, StandardButtons));
			allSystemInfos.Add(new SystemInfo("WonderSwan", CoreSystem.WonderSwan, 1));
			allSystemInfos.Add(new SystemInfo("Lynx", CoreSystem.Lynx, 1));
			allSystemInfos.Add(new SystemInfo("PlayStation", CoreSystem.Playstation, 2));
			allSystemInfos.Add(new SystemInfo("Apple II", CoreSystem.AppleII, 1));
		}

		/// <summary>
		/// Initialize a new instance of <see cref="SystemInfo"/>
		/// </summary>
		/// <param name="displayName">A <see cref="string"/> that specify how the system name is displayed</param>
		/// <param name="system">A <see cref="CoreSystem"/> that specify what core is used</param>
		/// <param name="maxControllers">Maximum controller allowed by this system</param>
		/// <param name="availableButtons">Which buttons are available (i.e. are actually on the controller) for this system</param>
		private SystemInfo(string displayName, CoreSystem system, int maxControllers, JoypadButton availableButtons)
		{
			_DisplayName = displayName;
			_System = system;
			_MaxControllers = maxControllers;
			_AvailableButtons = availableButtons;
		}

		/// <summary>
		/// Initialize a new instance of <see cref="SystemInfo"/>
		/// </summary>
		/// <param name="displayName">A <see cref="string"/> that specify how the system name is displayed</param>
		/// <param name="system">A <see cref="CoreSystem"/> that specify what core is used</param>
		/// <param name="maxControllers">Maximum controller allowed by this system</param>
		private SystemInfo(string displayName, CoreSystem system, int maxControllers)
			: this(displayName, system, maxControllers, 0)
		{ }

		/// <summary>
		/// Initialize a new instance of <see cref="SystemInfo"/>
		/// </summary>
		/// <param name="displayName">A <see cref="string"/> that specify how the system name is displayed</param>
		private SystemInfo(string displayName)
			: this(displayName, CoreSystem.Null, 0, 0)
		{ }

		#endregion

		#region Methods		

		#region Get SystemInfo
		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Apple II
		/// </summary
		public static SystemInfo AppleII
		{
			get
			{
				return allSystemInfos[25];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Atari 2600
		/// </summary
		public static SystemInfo Atari2600
		{
			get
			{
				return allSystemInfos[14];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Atari 7800
		/// </summary>
		public static SystemInfo Atari7800
		{
			get
			{
				return allSystemInfos[15];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Commodore 64
		/// </summary>
		public static SystemInfo C64
		{
			get
			{
				return allSystemInfos[16];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Coleco Vision
		/// </summary>
		public static SystemInfo Coleco
		{
			get
			{
				return allSystemInfos[17];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Dual Gameboy
		/// </summary>
		public static SystemInfo DualGB
		{
			get
			{
				return allSystemInfos[21];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Gameboy
		/// </summary>
		public static SystemInfo GB
		{
			get
			{
				return allSystemInfos[12];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Gameboy Advance
		/// </summary>
		public static SystemInfo GBA
		{
			get
			{
				return allSystemInfos[18];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Gameboy Color
		/// </summary>
		public static SystemInfo GBC
		{
			get
			{
				return allSystemInfos[13];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Genesis
		/// </summary>
		public static SystemInfo Genesis
		{
			get
			{
				return allSystemInfos[9];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Game Gear
		/// </summary>
		public static SystemInfo GG
		{
			get
			{
				return allSystemInfos[5];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Intellivision
		/// </summary>
		public static SystemInfo Intellivision
		{
			get
			{
				return allSystemInfos[2];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Lynx
		/// </summary>
		public static SystemInfo Lynx
		{
			get
			{
				return allSystemInfos[23];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for NES
		/// </summary>
		public static SystemInfo Nes
		{
			get
			{
				return allSystemInfos[1];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Nintendo 64
		/// </summary>
		public static SystemInfo N64
		{
			get
			{
				return allSystemInfos[19];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Null (i.e. nothing is emulated) emulator
		/// </summary>
		public static SystemInfo Null
		{
			get
			{
				return allSystemInfos[0];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for PCEngine (TurboGrafx-16)
		/// </summary>
		public static SystemInfo PCE
		{
			get
			{
				return allSystemInfos[6];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for PCEngine (TurboGrafx-16) + CD
		/// </summary>
		public static SystemInfo PCECD
		{
			get
			{
				return allSystemInfos[7];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for PlayStation
		/// </summary>
		public static SystemInfo PSX
		{
			get
			{
				return allSystemInfos[24];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Sega Saturn
		/// </summary>
		public static SystemInfo Saturn
		{
			get
			{
				return allSystemInfos[20];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for SG-1000 (Sega Game 1000)
		/// </summary>
		public static SystemInfo SG
		{
			get
			{
				return allSystemInfos[4];
			}
		}



		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for PCEngine (Supergraph FX)
		/// </summary>
		public static SystemInfo SGX
		{
			get
			{
				return allSystemInfos[8];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Sega Master System
		/// </summary>
		public static SystemInfo SMS
		{
			get
			{
				return allSystemInfos[3];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for SNES
		/// </summary>
		public static SystemInfo SNES
		{
			get
			{
				return allSystemInfos[11];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for TI-83
		/// </summary>
		public static SystemInfo TI83
		{
			get
			{
				return allSystemInfos[10];
			}
		}


		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for TI-83
		/// </summary>
		public static SystemInfo WonderSwan
		{
			get
			{
				return allSystemInfos[22];
			}
		}
		#endregion Get SystemInfo

		/// <summary>
		/// Get a <see cref="SystemInfo"/> by its <see cref="CoreSystem"/>
		/// </summary>
		/// <param name="system"><see cref="CoreSystem"/> you're looking for</param>
		/// <returns>Mathing <see cref="SystemInfo"/></returns>
		public static SystemInfo FindByCoreSystem(CoreSystem system)
		{
			return allSystemInfos.Find(s => s._System == system);
		}

		/// <summary>
		/// Determine if this <see cref="SystemInfo"/> is equal to specified <see cref="object"/>
		/// </summary>
		/// <param name="obj"><see cref="object"/> to comapre to</param>
		/// <returns>True if object is equal to this instance; otherwise, false</returns>
		public override bool Equals(object obj)
		{
			if (obj is SystemInfo)
			{
				return this == (SystemInfo)obj;
			}
			else
			{
				return base.Equals(obj);
			}
		}

		/// <summary>
		/// Gets the haschode for current insance
		/// </summary>
		/// <returns>This instance hashcode</returns>
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/// <summary>
		/// Returns a <see cref="string"/> representation of current <see cref="SystemInfo"/>
		/// In fact, return the same as DisplayName property
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return _DisplayName;
		}


		/// <summary>
		/// Determine if two <see cref="SystemInfo"/> are equals.
		/// As it is all static instance, it just compare their reference
		/// </summary>
		/// <param name="system1">First <see cref="SystemInfo"/></param>
		/// <param name="system2">Second <see cref="SystemInfo"/></param>
		/// <returns>True if both system are equals; otherwise, false</returns>
		public static bool operator ==(SystemInfo system1, SystemInfo system2)
		{
			return ReferenceEquals(system1, system2);
		}

		/// <summary>
		/// Determine if two <see cref="SystemInfo"/> are different.
		/// As it is all static instance, it just compare their reference
		/// </summary>
		/// <param name="system1">First <see cref="SystemInfo"/></param>
		/// <param name="system2">Second <see cref="SystemInfo"/></param>
		/// <returns>True if both system are diferent; otherwise, false</returns>
		public static bool operator !=(SystemInfo system1, SystemInfo system2)
		{
			return !(system1 == system2);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets <see cref="JoypadButton"/> available for this system
		/// </summary>
		public JoypadButton AvailableButtons
		{
			get
			{
				return _AvailableButtons;
			}
		}

		/// <summary>
		/// Gets the sytem name as <see cref="string"/>
		/// </summary>
		public string DisplayName
		{
			get
			{
				return _DisplayName;
			}
		}


		/// <summary>
		/// Gets the maximum amount of controller allowed for this system
		/// </summary>
		public int MaxControllers
		{
			get
			{
				return _MaxControllers;
			}
		}

		/// <summary>
		/// Gets core used for this system as <see cref="CoreSystem"/> enum
		/// </summary>
		public CoreSystem System
		{
			get
			{
				return _System;
			}
		}

		#endregion
	}
}
