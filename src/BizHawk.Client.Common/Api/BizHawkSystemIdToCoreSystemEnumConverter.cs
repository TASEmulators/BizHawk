using System;
using System.Globalization;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// This class holds a converter for BizHawk SystemId (which is a simple <see cref="string"/>
	/// It allows you to convert it to a <see cref="CoreSystem"/> value and vice versa
	/// </summary>
	/// <remarks>I made it this way just in case one day we need it for WPF (DependencyProperty binding). Just uncomment :IValueConverter implementation
	/// I didn't implemented it because of mono compatibility
	/// </remarks>
	public sealed class BizHawkSystemIdToEnumConverter //:IValueConverter
	{
		/// <summary>
		/// Convert BizHawk SystemId <see cref="string"/> to <see cref="CoreSystem"/> value
		/// </summary>
		/// <param name="value"><see cref="string"/> you want to convert</param>
		/// <param name="targetType">The type of the binding target property</param>
		/// <param name="parameter">The converter parameter to use; null in our case</param>
		/// <param name="cultureInfo">The culture to use in the converter</param>
		/// <returns>A <see cref="CoreSystem"/> that is equivalent to BizHawk SystemId <see cref="string"/></returns>
		/// <exception cref="IndexOutOfRangeException">Thrown when SystemId hasn't been found</exception>
		public object Convert(object value, Type targetType, object parameter, CultureInfo cultureInfo)
		{
			return (string) value switch
			{
				VSystemID.Raw.AppleII => CoreSystem.AppleII,
				VSystemID.Raw.A26 => CoreSystem.Atari2600,
				VSystemID.Raw.A78 => CoreSystem.Atari7800,
				VSystemID.Raw.Coleco => CoreSystem.ColecoVision,
				VSystemID.Raw.C64 => CoreSystem.Commodore64,
				VSystemID.Raw.GBL => CoreSystem.GameBoyLink,
				VSystemID.Raw.GB => CoreSystem.GameBoy,
				VSystemID.Raw.GBA => CoreSystem.GameBoyAdvance,
				VSystemID.Raw.GEN => CoreSystem.Genesis,
				VSystemID.Raw.INTV => CoreSystem.Intellivision,
				VSystemID.Raw.Libretro => CoreSystem.Libretro,
				VSystemID.Raw.Lynx => CoreSystem.Lynx,
				VSystemID.Raw.SMS => CoreSystem.MasterSystem,
				VSystemID.Raw.NDS => CoreSystem.NintendoDS,
				VSystemID.Raw.NES => CoreSystem.NES,
				VSystemID.Raw.N64 => CoreSystem.Nintendo64,
				VSystemID.Raw.NULL => CoreSystem.Null,
				VSystemID.Raw.PCE => CoreSystem.PCEngine,
				VSystemID.Raw.PCECD => CoreSystem.PCEngine,
				VSystemID.Raw.SGX => CoreSystem.PCEngine,
				VSystemID.Raw.PSX => CoreSystem.Playstation,
				VSystemID.Raw.SAT => CoreSystem.Saturn,
				VSystemID.Raw.SNES => CoreSystem.SNES,
				VSystemID.Raw.TI83 => CoreSystem.TI83,
				VSystemID.Raw.VEC => CoreSystem.Vectrex,
				VSystemID.Raw.WSWAN => CoreSystem.WonderSwan,
				VSystemID.Raw.ZXSpectrum => CoreSystem.ZXSpectrum,
				VSystemID.Raw.AmstradCPC => CoreSystem.AmstradCPC,
				VSystemID.Raw.GGL => CoreSystem.GGL,
				VSystemID.Raw.ChannelF => CoreSystem.ChannelF,
				VSystemID.Raw.MAME => CoreSystem.MAME,
				VSystemID.Raw.O2 => CoreSystem.Odyssey2,
				VSystemID.Raw.MSX => CoreSystem.MSX,
				VSystemID.Raw.VB => CoreSystem.VirtualBoy,
				VSystemID.Raw.NGP => CoreSystem.NeoGeoPocket,
				VSystemID.Raw.SGB => CoreSystem.SuperGameBoy,
				VSystemID.Raw.UZE => CoreSystem.UzeBox,
				VSystemID.Raw.PCFX => CoreSystem.PcFx,
				_ => throw new IndexOutOfRangeException($"{value} is missing in convert list")
			};
		}


		/// <summary>
		/// Convert BizHawk SystemId <see cref="string"/> to <see cref="CoreSystem"/> value
		/// </summary>
		/// <param name="value"><see cref="string"/> you want to convert</param>
		/// <returns>A <see cref="CoreSystem"/> that is equivalent to BizHawk SystemId <see cref="string"/></returns>
		/// <exception cref="IndexOutOfRangeException">Thrown when SystemId hasn't been found</exception>
		public CoreSystem Convert(string value)
		{
			return (CoreSystem)Convert(value, null, null, CultureInfo.CurrentCulture);
		}


		/// <summary>
		/// Convert a <see cref="CoreSystem"/> value to BizHawk SystemId <see cref="string"/>
		/// </summary>
		/// <param name="value"><see cref="CoreSystem"/> you want to convert</param>
		/// <param name="targetType">The type of the binding target property</param>
		/// <param name="parameter">The converter parameter to use; null in our case</param>
		/// <param name="cultureInfo">The culture to use in the converter</param>
		/// <returns>A <see cref="string"/> that is used by BizHawk SystemId</returns>
		/// <exception cref="IndexOutOfRangeException">Thrown when <see cref="CoreSystem"/> hasn't been found</exception>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo cultureInfo)
		{
			return (CoreSystem) value switch
			{
				CoreSystem.AppleII => VSystemID.Raw.AppleII,
				CoreSystem.Atari2600 => VSystemID.Raw.A26,
				CoreSystem.Atari7800 => VSystemID.Raw.A78,
				CoreSystem.ChannelF => VSystemID.Raw.ChannelF,
				CoreSystem.ColecoVision => VSystemID.Raw.Coleco,
				CoreSystem.Commodore64 => VSystemID.Raw.C64,
				CoreSystem.GameBoyLink => VSystemID.Raw.GBL,
				CoreSystem.GameBoy => VSystemID.Raw.GB,
				CoreSystem.GameBoyAdvance => VSystemID.Raw.GBA,
				CoreSystem.Genesis => VSystemID.Raw.GEN,
				CoreSystem.GGL => VSystemID.Raw.GGL,
				CoreSystem.Intellivision => VSystemID.Raw.INTV,
				CoreSystem.Libretro => VSystemID.Raw.Libretro,
				CoreSystem.Lynx => VSystemID.Raw.Lynx,
				CoreSystem.MAME => VSystemID.Raw.MAME,
				CoreSystem.MasterSystem => VSystemID.Raw.SMS,
				CoreSystem.MSX => VSystemID.Raw.MSX,
				CoreSystem.NeoGeoPocket => VSystemID.Raw.NGP,
				CoreSystem.NES => VSystemID.Raw.NES,
				CoreSystem.Nintendo64 => VSystemID.Raw.N64,
				CoreSystem.NintendoDS => VSystemID.Raw.NDS,
				CoreSystem.Null => VSystemID.Raw.NULL,
				CoreSystem.PCEngine => VSystemID.Raw.PCE,
				CoreSystem.PcFx => VSystemID.Raw.PCFX,
				CoreSystem.Playstation => VSystemID.Raw.PSX,
				CoreSystem.Saturn => VSystemID.Raw.SAT,
				CoreSystem.SNES => VSystemID.Raw.SNES,
				CoreSystem.SuperGameBoy => VSystemID.Raw.SGB,
				CoreSystem.TI83 => VSystemID.Raw.TI83,
				CoreSystem.UzeBox => VSystemID.Raw.UZE,
				CoreSystem.Vectrex => VSystemID.Raw.VEC,
				CoreSystem.VirtualBoy => VSystemID.Raw.VB,
				CoreSystem.WonderSwan => VSystemID.Raw.WSWAN,
				CoreSystem.ZXSpectrum => VSystemID.Raw.ZXSpectrum,
				CoreSystem.AmstradCPC => VSystemID.Raw.AmstradCPC,
				CoreSystem.Odyssey2 => VSystemID.Raw.O2,
				_ => throw new IndexOutOfRangeException($"{value} is missing in convert list")
			};
		}


		/// <summary>
		/// Convert a <see cref="CoreSystem"/> value to BizHawk SystemId <see cref="string"/>
		/// </summary>
		/// <param name="value"><see cref="CoreSystem"/> you want to convert</param>
		/// <returns>A <see cref="string"/> that is used by BizHawk SystemId</returns>
		/// <exception cref="IndexOutOfRangeException">Thrown when <see cref="CoreSystem"/> hasn't been found</exception>
		public string ConvertBack(CoreSystem value)
		{
			return (string)ConvertBack(value, null, null, CultureInfo.CurrentCulture);
		}
	}
}
