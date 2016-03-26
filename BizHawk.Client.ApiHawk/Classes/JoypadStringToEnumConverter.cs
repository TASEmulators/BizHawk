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

				case "START":
					return JoypadButton.Start;

				case "SELECT":
					return JoypadButton.Select;

				case "UP":
					return JoypadButton.Up;

				case "DOWN":
					return JoypadButton.Down;

				case "LEFT":
					return JoypadButton.Left;

				case "RIGHT":
					return JoypadButton.Right;

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

				case JoypadButton.Start:
					return "Start";

				case JoypadButton.Select:
					return "Select";

				case JoypadButton.Up:
					return "Up";

				case JoypadButton.Down:
					return "Down";

				case JoypadButton.Left:
					return "Left";

				case JoypadButton.Right:
					return "Right";					

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
