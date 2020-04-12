using System;
using System.Globalization;

using BizHawk.Client.Common;

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
				"AppleII" => CoreSystem.AppleII,
				"A26" => CoreSystem.Atari2600,
				"A78" => CoreSystem.Atari7800,
				"Coleco" => CoreSystem.ColecoVision,
				"C64" => CoreSystem.Commodore64,
				"DGB" => CoreSystem.DualGameBoy,
				"GB" => CoreSystem.GameBoy,
				"GBA" => CoreSystem.GameBoyAdvance,
				"GEN" => CoreSystem.Genesis,
				"INTV" => CoreSystem.Intellivision,
				"Libretro" => CoreSystem.Libretro,
				"Lynx" => CoreSystem.Lynx,
				"SMS" => CoreSystem.MasterSystem,
				"NDS" => CoreSystem.NintendoDS,
				"NES" => CoreSystem.NES,
				"N64" => CoreSystem.Nintendo64,
				"NULL" => CoreSystem.Null,
				"PCE" => CoreSystem.PCEngine,
				"PCECD" => CoreSystem.PCEngine,
				"SGX" => CoreSystem.PCEngine,
				"PSX" => CoreSystem.Playstation,
				"SAT" => CoreSystem.Saturn,
				"SNES" => CoreSystem.SNES,
				"TI83" => CoreSystem.TI83,
				"VEC" => CoreSystem.Vectrex,
				"WSWAN" => CoreSystem.WonderSwan,
				"ZXSpectrum" => CoreSystem.ZXSpectrum,
				"AmstradCPC" => CoreSystem.AmstradCPC,
				"GGL" => CoreSystem.GGL,
				"ChannelF" => CoreSystem.ChannelF,
				"GB3x" => CoreSystem.GB3x,
				"GB4x" => CoreSystem.GB4x,
				"MAME" => CoreSystem.MAME,
				"O2" => CoreSystem.Odyssey2,
				"MSX" => CoreSystem.MSX,
				"VB" => CoreSystem.VirtualBoy,
				"NGP" => CoreSystem.NeoGeoPocket,
				"DNGP" => CoreSystem.NeoGeoPocket,
				"SGB" => CoreSystem.SuperGameBoy,
				"UZE" => CoreSystem.UzeBox,
				"PCFX" => CoreSystem.PcFx,
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
				CoreSystem.AppleII => "AppleII",
				CoreSystem.Atari2600 => "A26",
				CoreSystem.Atari7800 => "A78",
				CoreSystem.ColecoVision => "Coleco",
				CoreSystem.Commodore64 => "C64",
				CoreSystem.DualGameBoy => "DGB",
				CoreSystem.GameBoy => "GB",
				CoreSystem.GameBoyAdvance => "GBA",
				CoreSystem.Genesis => "GEN",
				CoreSystem.Intellivision => "INTV",
				CoreSystem.Libretro => "Libretro",
				CoreSystem.Lynx => "Lynx",
				CoreSystem.MasterSystem => "SMS",
				CoreSystem.NES => "NES",
				CoreSystem.Nintendo64 => "N64",
				CoreSystem.Null => "NULL",
				CoreSystem.PCEngine => "PCE",
				CoreSystem.Playstation => "PSX",
				CoreSystem.Saturn => "SAT",
				CoreSystem.SNES => "SNES",
				CoreSystem.TI83 => "TI83",
				CoreSystem.WonderSwan => "WSWAN",
				CoreSystem.ZXSpectrum => "ZXSpectrum",
				CoreSystem.AmstradCPC => "AmstradCPC",
				CoreSystem.Odyssey2 => "O2",
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
