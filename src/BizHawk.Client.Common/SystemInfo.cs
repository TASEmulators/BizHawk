using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// This class holds logic for System information.
	/// That means specifications about a system that BizHawk emulates
	/// </summary>
	public sealed class SystemInfo
	{
		private const JoypadButton UpDownLeftRight = JoypadButton.Up | JoypadButton.Down | JoypadButton.Left | JoypadButton.Right;
		private const JoypadButton StandardButtons = JoypadButton.A | JoypadButton.B | JoypadButton.Start | JoypadButton.Select | UpDownLeftRight;

		private static readonly List<SystemInfo> AllSystemInfos = new List<SystemInfo>();

		/// <summary>
		/// Initializes a new instance of the <see cref="SystemInfo"/> class
		/// </summary>
		/// <param name="displayName">A <see cref="string"/> that specify how the system name is displayed</param>
		/// <param name="system">A <see cref="CoreSystem"/> that specify what core is used</param>
		/// <param name="maxControllers">Maximum controller allowed by this system</param>
		/// <param name="availableButtons">Which buttons are available (i.e. are actually on the controller) for this system</param>
		private SystemInfo(string displayName, CoreSystem system, int maxControllers, JoypadButton availableButtons = 0)
		{
			DisplayName = displayName;
			System = system;
			MaxControllers = maxControllers;
			AvailableButtons = availableButtons;

			AllSystemInfos.Add(this);
		}

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Apple II
		/// </summary>
		public static SystemInfo Libretro { get; } = new SystemInfo("Libretro", CoreSystem.Libretro, 1);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Apple II
		/// </summary>
		public static SystemInfo AppleII { get; } = new SystemInfo("Apple II", CoreSystem.AppleII, 1);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Atari 2600
		/// </summary>
		public static SystemInfo Atari2600 { get; } = new SystemInfo("Atari 2600", CoreSystem.Atari2600, 1);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Atari 7800
		/// </summary>
		public static SystemInfo Atari7800 { get; } = new SystemInfo("Atari 7800", CoreSystem.Atari7800, 1);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Commodore 64
		/// </summary>
		public static SystemInfo C64 { get; } = new SystemInfo("Commodore 64", CoreSystem.Commodore64, 1);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Coleco Vision
		/// </summary>
		public static SystemInfo Coleco { get; } = new SystemInfo("ColecoVision", CoreSystem.ColecoVision, 1);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Triple Gameboy
		/// </summary>
		public static SystemInfo GB3x { get; } = new SystemInfo("Game Boy Link 3x", CoreSystem.GB3x, 3, StandardButtons);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Quad Gameboy
		/// </summary>
		public static SystemInfo GB4x { get; } = new SystemInfo("Game Boy Link 4x", CoreSystem.GB4x, 4, StandardButtons);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Dual Gameboy
		/// </summary>
		public static SystemInfo DualGB { get; } = new SystemInfo("Game Boy Link", CoreSystem.DualGameBoy, 2, StandardButtons);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Gameboy
		/// </summary>
		public static SystemInfo GB { get; } = new SystemInfo("GB", CoreSystem.GameBoy, 1, StandardButtons);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Gameboy Advance
		/// </summary>
		public static SystemInfo GBA { get; } = new SystemInfo("Gameboy Advance", CoreSystem.GameBoyAdvance, 1, StandardButtons | JoypadButton.L | JoypadButton.R);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Gameboy Color
		/// </summary>
		public static SystemInfo GBC { get; } = new SystemInfo("Gameboy Color", CoreSystem.GameBoy, 1, StandardButtons);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Nintendo DS
		/// </summary>
		public static SystemInfo NDS { get; } = new SystemInfo("NDS", CoreSystem.NintendoDS, 1, StandardButtons | JoypadButton.L | JoypadButton.R | JoypadButton.X | JoypadButton.Y);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Genesis
		/// </summary>
		public static SystemInfo Genesis { get; } = new SystemInfo("Genesis", CoreSystem.Genesis, 2, UpDownLeftRight | JoypadButton.A | JoypadButton.B | JoypadButton.C | JoypadButton.X | JoypadButton.Y | JoypadButton.Z);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Game Gear
		/// </summary>
		public static SystemInfo GG { get; } = new SystemInfo("Game Gear", CoreSystem.MasterSystem, 1, UpDownLeftRight | JoypadButton.B1 | JoypadButton.B2);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Intellivision
		/// </summary>
		public static SystemInfo Intellivision { get; } = new SystemInfo("Intellivision", CoreSystem.Intellivision, 2);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Lynx
		/// </summary>
		public static SystemInfo Lynx { get; } = new SystemInfo("Lynx", CoreSystem.Lynx, 1);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for NES
		/// </summary>
		public static SystemInfo Nes { get; } = new SystemInfo("NES", CoreSystem.NES, 2, StandardButtons);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Nintendo 64
		/// </summary>
		public static SystemInfo N64 { get; } = new SystemInfo("Nintendo 64", CoreSystem.Nintendo64, 4, StandardButtons ^ JoypadButton.Select | JoypadButton.Z | JoypadButton.CUp | JoypadButton.CDown | JoypadButton.CLeft | JoypadButton.CRight | JoypadButton.AnalogStick | JoypadButton.L | JoypadButton.R);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Null (i.e. nothing is emulated) emulator
		/// </summary>
		public static SystemInfo Null { get; } = new SystemInfo("", CoreSystem.Null, 0);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for PCEngine (TurboGrafx-16)
		/// </summary>
		public static SystemInfo PCE { get; } = new SystemInfo("TurboGrafx-16", CoreSystem.PCEngine, 1);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for PCEngine (TurboGrafx-16) + CD
		/// </summary>
		public static SystemInfo PCECD { get; } = new SystemInfo("TurboGrafx - 16(CD)", CoreSystem.PCEngine, 1);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for PlayStation
		/// </summary>
		public static SystemInfo PSX { get; } = new SystemInfo("PlayStation", CoreSystem.Playstation, 2);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Sega Saturn
		/// </summary>
		public static SystemInfo Saturn { get; } = new SystemInfo("Saturn", CoreSystem.Saturn, 2, UpDownLeftRight | JoypadButton.A | JoypadButton.B | JoypadButton.C | JoypadButton.X | JoypadButton.Y | JoypadButton.Z);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for SG-1000 (Sega Game 1000)
		/// </summary>
		public static SystemInfo SG { get; } = new SystemInfo("SG-1000", CoreSystem.MasterSystem, 1);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for PCEngine (Supergraph FX)
		/// </summary>
		public static SystemInfo SGX { get; } = new SystemInfo("SuperGrafx", CoreSystem.PCEngine, 1);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Sega Master System
		/// </summary>
		public static SystemInfo SMS { get; } = new SystemInfo("Sega Master System", CoreSystem.MasterSystem, 2, UpDownLeftRight | JoypadButton.B1 | JoypadButton.B2);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for SNES
		/// </summary>
		public static SystemInfo SNES { get; } = new SystemInfo("SNES", CoreSystem.SNES, 8, StandardButtons | JoypadButton.X | JoypadButton.Y | JoypadButton.L | JoypadButton.R);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for TI-83
		/// </summary>
		public static SystemInfo TI83 { get; } = new SystemInfo("TI - 83", CoreSystem.TI83, 1);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for WonderSwan
		/// </summary>
		public static SystemInfo WonderSwan { get; } = new SystemInfo("WonderSwan", CoreSystem.WonderSwan, 1);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Virtual Boy
		/// </summary>
		public static SystemInfo VirtualBoy { get; } = new SystemInfo("Virtual Boy", CoreSystem.VirtualBoy, 1);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Vectrex
		/// </summary>
		public static SystemInfo Vectrex { get; } = new SystemInfo("Vectrex", CoreSystem.Vectrex, 2);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for TI-83
		/// </summary>
		public static SystemInfo NeoGeoPocket { get; } = new SystemInfo("Neo-Geo Pocket", CoreSystem.NeoGeoPocket, 1);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for ZXSpectrum
		/// </summary>
		public static SystemInfo ZxSpectrum { get; } = new SystemInfo("ZX Spectrum", CoreSystem.ZXSpectrum, 2);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for AmstradCPC
		/// </summary>
		public static SystemInfo AmstradCpc { get; } = new SystemInfo("Amstrad CPC", CoreSystem.AmstradCPC, 2);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for GGL
		/// </summary>
		public static SystemInfo GGL { get; } = new SystemInfo("Game Gear Linked", CoreSystem.GGL, 2);

		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for ChannelF
		/// </summary>
		/// 
		public static SystemInfo ChannelF { get; } = new SystemInfo("Channel F", CoreSystem.ChannelF, 2);
		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for Odyssey2
		/// </summary>
		/// 
		public static SystemInfo O2 { get; } = new SystemInfo("Odyssey2", CoreSystem.Odyssey2, 2);
		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for MAME
		/// </summary>
		public static SystemInfo Mame { get; } = new SystemInfo("MAME", CoreSystem.MAME, 4);
		/// <summary>
		/// Gets the <see cref="SystemInfo"/> instance for MSX
		/// </summary>
		public static SystemInfo MSX { get; } = new SystemInfo("MSX", CoreSystem.MSX, 2);

		public static SystemInfo Sgb { get; } = new SystemInfo("SGB", CoreSystem.SuperGameBoy, 4);

		public static SystemInfo Pcfx { get; } = new SystemInfo("PCFX", CoreSystem.PcFx, 1);

		public static SystemInfo UzeBox { get; } = new SystemInfo("uzem", CoreSystem.UzeBox, 1);

		/// <summary>
		/// Get a <see cref="SystemInfo"/> by its <see cref="CoreSystem"/>
		/// </summary>
		/// <param name="system"><see cref="CoreSystem"/> you're looking for</param>
		/// <returns><see cref="SystemInfo"/></returns>
		public static SystemInfo FindByCoreSystem(CoreSystem system)
		{
			return AllSystemInfos.Find(s => s.System == system);
		}

		/// <summary>
		/// Determine if this <see cref="SystemInfo"/> is equal to specified <see cref="object"/>
		/// </summary>
		/// <param name="obj"><see cref="object"/> to compare to</param>
		/// <returns>True if object is equal to this instance; otherwise, false</returns>
		public override bool Equals(object obj)
		{
			if (obj is SystemInfo info)
			{
				return this == info;
			}

			return base.Equals(obj);
		}

		public override int GetHashCode() => DisplayName.GetHashCode(); // should be unique considering the property's purpose

		/// <summary>
		/// Returns a <see cref="string"/> representation of current <see cref="SystemInfo"/>
		/// In fact, return the same as DisplayName property
		/// </summary>
		public override string ToString() => DisplayName;

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
		/// <returns>True if both system are different; otherwise, false</returns>
		public static bool operator !=(SystemInfo system1, SystemInfo system2)
		{
			return !(system1 == system2);
		}

		/// <summary>
		/// Gets <see cref="JoypadButton"/> available for this system
		/// </summary>
		public JoypadButton AvailableButtons { get; }

		/// <summary>
		/// Gets the system name as <see cref="string"/>
		/// </summary>
		public string DisplayName { get; }

		/// <summary>
		/// Gets the maximum amount of controller allowed for this system
		/// </summary>
		public int MaxControllers { get; }

		/// <summary>
		/// Gets core used for this system as <see cref="CoreSystem"/> enum
		/// </summary>
		public CoreSystem System { get; }
	}
}
