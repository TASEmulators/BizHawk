using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using BizHawk.Common.NumberExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// This class holds a byte (8 bits) <see cref="Watch"/>
	/// </summary>
	public sealed class ByteWatch : Watch
	{
		#region Fields

		private byte _previous;
		private byte _value;

		#endregion

		#region cTor(s)

		/// <summary>
		/// Inialize a new instance of <see cref="ByteWatch"/>
		/// </summary>
		/// <param name="domain"><see cref="MemoryDomain"/> where you want to track</param>
		/// <param name="address">The address you want to track</param>
		/// <param name="type">How you you want to display the value See <see cref="DisplayType"/></param>
		/// <param name="bigEndian">Specify the endianess. true for big endian</param>
		/// <param name="note">A custom note about the <see cref="Watch"/></param>
		/// <param name="value">Current value</param>
		/// <param name="previous">Previous value</param>
		/// <param name="changeCount">How many times value has changed</param>
		/// <exception cref="ArgumentException">Occurs when a <see cref="DisplayType"/> is incompatible with <see cref="WatchSize.Byte"/></exception>
		internal ByteWatch(MemoryDomain domain, long address, DisplayType type, bool bigEndian, string note, byte value, byte previous, int changeCount)
			: base(domain, address, WatchSize.Byte, type, bigEndian, note)
		{
			if (value == 0)
			{
				this._value = GetByte();
			}
			else
			{
				this._value = value;
			}
			this._previous = previous;
			this._changecount = changeCount;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Enumerate wich <see cref="DisplayType"/> are valid for a <see cref="ByteWatch"/>
		/// </summary>
		public static IEnumerable<DisplayType> ValidTypes
		{
			get
			{
				yield return DisplayType.Unsigned;
				yield return DisplayType.Signed;
				yield return DisplayType.Hex;
				yield return DisplayType.Binary;
			}
		}

		#region Implements

		/// <summary>
		/// Get a list a <see cref="DisplayType"/> that can be used for this <see cref="ByteWatch"/>
		/// </summary>
		/// <returns>An enumartion that contains all valid <see cref="DisplayType"/></returns>
		public override IEnumerable<DisplayType> AvailableTypes()
		{
			return ValidTypes;
		}

		/// <summary>
		/// Reset the previous value; set it to the current one
		/// </summary>
		public override void ResetPrevious()
		{
			_previous = GetByte();
		}

		/// <summary>
		/// Try to sets the value into the <see cref="MemoryDomain"/>
		/// at the current <see cref="Watch"/> address
		/// </summary>
		/// <param name="value">Value to set</param>
		/// <returns>True if value successfully sets; othewise, false</returns>
		public override bool Poke(string value)
		{
			try
			{
				byte val = 0;
				switch (Type)
				{
					case DisplayType.Unsigned:
						if (value.IsUnsigned())
						{
							val = (byte)int.Parse(value);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.Signed:
						if (value.IsSigned())
						{
							val = (byte)(sbyte)int.Parse(value);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.Hex:
						if (value.IsHex())
						{
							val = (byte)int.Parse(value, NumberStyles.HexNumber);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.Binary:
						if (value.IsBinary())
						{
							val = (byte)Convert.ToInt32(value, 2);
						}
						else
						{
							return false;
						}

						break;
				}

				if (Global.CheatList.Contains(Domain, _address))
				{
					var cheat = Global.CheatList.FirstOrDefault(c => c.Address == _address && c.Domain == Domain);

					if (cheat != (Cheat)null)
					{
						cheat.PokeValue(val);
						PokeByte(val);
						return true;
					}
				}

				PokeByte(val);
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Update the Watch (read it from <see cref="MemoryDomain"/>
		/// </summary>
		public override void Update()
		{
			switch (Global.Config.RamWatchDefinePrevious)
			{
				case PreviousType.Original:
					return;
				case PreviousType.LastChange:
					var temp = _value;
					_value = GetByte();
					if (_value != temp)
					{
						_previous = _value;
						_changecount++;
					}

					break;
				case PreviousType.LastFrame:
					_previous = _value;
					_value = GetByte();
					if (_value != Previous)
					{
						_changecount++;
					}

					break;
			}
		}

		#endregion Implements

		//TODO: Implements IFormattable
		public string FormatValue(byte val)
		{
			switch (Type)
			{
				default:
				case DisplayType.Unsigned:
					return val.ToString();
				case DisplayType.Signed:
					return ((sbyte)val).ToString();
				case DisplayType.Hex:
					return val.ToHexString(2);
				case DisplayType.Binary:
					return Convert.ToString(val, 2).PadLeft(8, '0').Insert(4, " ");
			}
		}

		#endregion

		#region Properties

		#region Implements

		/// <summary>
		/// Get a string representation of difference
		/// between current value and the previous one
		/// </summary>
		public override string Diff
		{
			get
			{
				string diff = string.Empty;
				int diffVal = _value - _previous;
				if (diffVal > 0)
				{
					diff = "+";
				}
				else if (diffVal < 0)
				{
					diff = "-";
				}

				return string.Format("{0}{1}", diff, FormatValue((byte)Math.Abs(diffVal)));
			}
		}

		/// <summary>
		/// Get the maximum possible value
		/// </summary>
		public override uint MaxValue
		{
			get
			{
				return byte.MaxValue;
			}
		}

		/// <summary>
		/// Get the current value
		/// </summary>
		public override int Value
		{
			get
			{
				return GetByte();
			}
		}

		/// <summary>
		/// Gets the current value
		/// but with stuff I don't understand
		/// </summary>
		/// <remarks>zero 15-nov-2015 - bypass LIAR LOGIC, see fdc9ea2aa922876d20ba897fb76909bf75fa6c92 https://github.com/TASVideos/BizHawk/issues/326 </remarks>
		public override int ValueNoFreeze
		{
			get
			{
				return GetByte(true);
			}
		}

		/// <summary>
		/// Get a string representation of the current value
		/// </summary>
		public override string ValueString
		{
			get
			{
				return FormatValue(GetByte());
			}
		}

		/// <summary>
		/// Get the previous value
		/// </summary>
		public override int Previous
		{
			get
			{
				return _previous;
			}
		}

		/// <summary>
		/// Get a string representation of the previous value
		/// </summary>
		public override string PreviousStr
		{
			get
			{
				return FormatValue(_previous);
			}
		}

		#endregion Implements

		#endregion
	}
}
