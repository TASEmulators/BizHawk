using System;

namespace BizHawk.Client.ApiHawk
{
	/// <summary>
	/// This enumeration list all buttons
	/// for all existing controllers
	/// </summary>
	[Flags]
	public enum JoypadButton
	{
		A = 1,
		B = 2,
		C = 4,
		X = 8,
		Y = 16,
		Z = 32,
		L = 64,
		R = 128,
		Start = 256,
		Select = 512,
		Up = 1024,
		Down = 2048,
		Left = 4096,
		Right = 8192,

		/// <summary>
		/// Master system Button 1
		/// </summary>
		B1 = 16384,

		/// <summary>
		/// Master system Button 1
		/// </summary>
		B2 = 32768,

		/// <summary>
		/// N64 C up
		/// </summary>
		CUp = 65536,

		/// <summary>
		/// N64 C down
		/// </summary>
		CDown = 131072,

		/// <summary>
		/// N64 C Left
		/// </summary>
		CLeft = 262144,

		/// <summary>
		/// N64 C Right
		/// </summary>
		CRight = 524288,

		/// <summary>
		/// N64 Analog stick
		/// </summary>
		AnalogStick = 1048576
	}
}
