using System;
using System.Globalization;
using BizHawk.Client.Common;

namespace BizHawk.Client.ApiHawk
{
	/// <summary>
	/// This class holds a converter for BizHawk joypad buttons (which is a simple <see cref="string"/>
	/// It allows you to convert it to a <see cref="JoypadButton"/> value and vice versa
	/// </summary>
	/// <remarks>I made it this way just in case one day we need it for WPF (DependencyProperty binding). Just uncomment :IValueConverter implementation
	/// I didn't implemented it because of mono compatibility
	/// </remarks>
	public sealed class JoypadStringToEnumConverter //:IValueConverter
	{
		/// <summary>
		/// Convert BizHawk button <see cref="string"/> to <see cref="JoypadButton"/> value
		/// </summary>
		/// <param name="value"><see cref="string"/> you want to convert</param>
		/// <param name="targetType">The type of the binding target property</param>
		/// <param name="parameter">The converter parameter to use; null in our case</param>
		/// <param name="cultureInfo">The culture to use in the converter</param>
		/// <returns>A <see cref="JoypadButton"/> that is equivalent to BizHawk <see cref="string"/> button</returns>
		/// <exception cref="IndexOutOfRangeException">Thrown when SystemId hasn't been found</exception>
		public object Convert(object value, Type targetType, object parameter, CultureInfo cultureInfo)
		{
			switch (((string)value).ToUpper())
			{
				case "A":
					return JoypadButton.A;

				case "B":
					return JoypadButton.B;				

				case "B1":
					return JoypadButton.B1;

				case "B2":
					return JoypadButton.B2;

				case "C":
					return JoypadButton.C;

				case "C UP":
					return JoypadButton.CUp;

				case "C DOWN":
					return JoypadButton.CDown;

				case "C LEFT":
					return JoypadButton.CLeft;

				case "C RIGHT":
					return JoypadButton.CRight;

				case "X":
					return JoypadButton.X;

				case "Y":
					return JoypadButton.Y;

				case "Z":
					return JoypadButton.Z;

				case "START":
					return JoypadButton.Start;

				case "SELECT":
					return JoypadButton.Select;

				case "UP":
				case "DPAD U":
					return JoypadButton.Up;

				case "DOWN":
				case "DPAD D":
					return JoypadButton.Down;

				case "LEFT":
				case "DPAD L":
					return JoypadButton.Left;

				case "RIGHT":
				case "DPAD R":
					return JoypadButton.Right;

				case "L":
					return JoypadButton.L;

				case "R":
					return JoypadButton.R;

				default:
					throw new IndexOutOfRangeException(string.Format("{0} is missing in convert list", value));
			}
		}


		/// <summary>
		/// Convert BizHawk button <see cref="string"/> to <see cref="JoypadButton"/> value
		/// </summary>
		/// <param name="value"><see cref="string"/> you want to convert</param>
		/// <returns>A <see cref="JoypadButton"/> that is equivalent to BizHawk <see cref="string"/> button</returns>
		/// <exception cref="IndexOutOfRangeException">Thrown when SystemId hasn't been found</exception>
		public JoypadButton Convert(string value)
		{
			return (JoypadButton)Convert(value, null, null, CultureInfo.CurrentCulture);
		}


		/// <summary>
		/// Convert a <see cref="JoypadButton"/> value to BizHawk <see cref="string"/>
		/// </summary>
		/// <param name="value"><see cref="JoypadButton"/> you want to convert</param>
		/// <param name="targetType">The type of the binding target property</param>
		/// <param name="parameter">In our case, we pass the <see cref="SystemInfo"/></param>
		/// <param name="cultureInfo">The culture to use in the converter</param>
		/// <returns>A <see cref="string"/> that is used by BizHawk</returns>
		/// <exception cref="IndexOutOfRangeException">Thrown when <see cref="JoypadButton"/> hasn't been found</exception>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo cultureInfo)
		{
			switch ((JoypadButton)value)
			{
				case JoypadButton.A:
					return "A";

				case JoypadButton.B:
					return "B";

				case JoypadButton.B1:
					return "B1";

				case JoypadButton.B2:
					return "B2";

				case JoypadButton.C:
					return "C";

				case JoypadButton.CUp:
					return "C Up";

				case JoypadButton.CDown:
					return "C Down";

				case JoypadButton.CLeft:
					return "C Left";

				case JoypadButton.CRight:
					return "C Right";

				case JoypadButton.X:
					return "X";

				case JoypadButton.Y:
					return "Y";

				case JoypadButton.Z:
					return "Z";

				case JoypadButton.Start:
					return "Start";

				case JoypadButton.Select:
					return "Select";

				case JoypadButton.Up:
					if (((SystemInfo)parameter) == SystemInfo.N64)
					{
						return "Dpad U";
					}
					else
					{
						return "Up";
					}

				case JoypadButton.Down:
					if (((SystemInfo)parameter) == SystemInfo.N64)
					{
						return "Dpad D";
					}
					else
					{
						return "Down";
					}

				case JoypadButton.Left:
					if (((SystemInfo)parameter) == SystemInfo.N64)
					{
						return "Dpad L";
					}
					else
					{
						return "Left";
					}

				case JoypadButton.Right:
					if (((SystemInfo)parameter) == SystemInfo.N64)
					{
						return "Dpad R";
					}
					else
					{
						return "Right";
					}

				case JoypadButton.L:
					return "L";

				case JoypadButton.R:
					return "R";

				default:
					throw new IndexOutOfRangeException(string.Format("{0} is missing in convert list", value));
			}
		}


		/// <summary>
		/// Convert a <see cref="JoypadButton"/> value to BizHawk <see cref="string"/>
		/// </summary>
		/// <param name="button"><see cref="JoypadButton"/> you want to convert</param>
		/// <param name="system">Current <see cref="SystemInfo"/></param>
		/// <returns>A <see cref="string"/> that is used by BizHawk</returns>
		/// <exception cref="IndexOutOfRangeException">Thrown when <see cref="JoypadButton"/> hasn't been found</exception>
		public string ConvertBack(JoypadButton button, SystemInfo system)
		{
			return (string)ConvertBack(button, null, system, CultureInfo.CurrentCulture);
		}
	}
}
