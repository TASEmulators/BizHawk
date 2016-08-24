using System;
using System.Globalization;

namespace BizHawk.Client.ApiHawk
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
			switch ((string)value)
			{
				case "AppleII":
					return CoreSystem.AppleII;

				case "A26":
					return CoreSystem.Atari2600;

				case "A78":
					return CoreSystem.Atari2600;

				case "Coleco":
					return CoreSystem.ColecoVision;

				case "C64":
					return CoreSystem.Commodore64;

				case "DGB":
					return CoreSystem.DualGameBoy;

				case "GB":
					return CoreSystem.GameBoy;

				case "GBA":
					return CoreSystem.GameBoyAdvance;

				case "GEN":
					return CoreSystem.Genesis;

				case "INTV":
					return CoreSystem.Intellivision;

				case "Libretro":
					return CoreSystem.Libretro;

				case "Lynx":
					return CoreSystem.Lynx;

				case "SMS":
					return CoreSystem.MasterSystem;

				case "NES":
					return CoreSystem.NES;

				case "N64":
					return CoreSystem.Nintendo64;

				case "NULL":
					return CoreSystem.Null;

				case "PCE":
				case "PCECD":
				case "SGX":
					return CoreSystem.PCEngine;

				case "PSX":
					return CoreSystem.Playstation;

				case "PSP":
					return CoreSystem.PSP;

				case "SAT":
					return CoreSystem.Saturn;

				case "SNES":
					return CoreSystem.SNES;

				case "TI83":
					return CoreSystem.TI83;

				case "WSWAN":
					return CoreSystem.WonderSwan;

				default:
					throw new IndexOutOfRangeException(string.Format("{0} is missing in convert list", value));
			}
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
			switch ((CoreSystem)value)
			{
				case CoreSystem.AppleII:
					return "AppleII";

				case CoreSystem.Atari2600:
					return "A26";

				case CoreSystem.Atari7800:
					return "A78";

				case CoreSystem.ColecoVision:
					return "Coleco";

				case CoreSystem.Commodore64:
					return "C64";

				case CoreSystem.DualGameBoy:
					return "DGB";

				case CoreSystem.GameBoy:
					return "GB";

				case CoreSystem.GameBoyAdvance:
					return "GBA";

				case CoreSystem.Genesis:
					return "GEN";

				case CoreSystem.Intellivision:
					return "INTV";

				case CoreSystem.Libretro:
					return "Libretro";

				case CoreSystem.Lynx:
					return "Lynx";

				case CoreSystem.MasterSystem:
					return "SMS";

				case CoreSystem.NES:
					return "NES";

				case CoreSystem.Nintendo64:
					return "N64";

				case CoreSystem.Null:
					return "NULL";

				case CoreSystem.PCEngine:
					return "PCE";

				case CoreSystem.Playstation:
					return "PSX";

				case CoreSystem.PSP:
					return "PSP";

				case CoreSystem.Saturn:
					return "SAT";

				case CoreSystem.SNES:
					return "SNES";

				case CoreSystem.TI83:
					return "TI83";

				case CoreSystem.WonderSwan:
					return "WSWAN";

				default:
					throw new IndexOutOfRangeException(string.Format("{0} is missing in convert list", value.ToString()));
			}
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
