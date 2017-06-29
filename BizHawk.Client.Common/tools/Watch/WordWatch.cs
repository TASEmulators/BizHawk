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
	/// This class holds a word (16 bits) <see cref="Watch"/>
	/// </summary>
	public sealed class WordWatch : Watch
	{
		private ushort _previous;
		private ushort _value;

		/// <summary>
		/// Initializes a new instance of the <see cref="WordWatch"/> class
		/// </summary>
		/// <param name="domain"><see cref="MemoryDomain"/> where you want to track</param>
		/// <param name="address">The address you want to track</param>
		/// <param name="type">How you you want to display the value See <see cref="DisplayType"/></param>
		/// <param name="bigEndian">Specify the endianess. true for big endian</param>
		/// <param name="note">A custom note about the <see cref="Watch"/></param>
		/// <param name="value">Current value</param>
		/// <param name="previous">Previous value</param>
		/// <param name="changeCount">How many times value has changed</param>
		/// <exception cref="ArgumentException">Occurs when a <see cref="DisplayType"/> is incompatible with <see cref="WatchSize.Word"/></exception>
		internal WordWatch(MemoryDomain domain, long address, DisplayType type, bool bigEndian, string note, ushort value, ushort previous, int changeCount)
			: base(domain, address, WatchSize.Word, type, bigEndian, note)
		{
			_value = value == 0 ? GetWord() : value;
			_previous = previous;
			ChangeCount = changeCount;
		}

		/// <summary>
		/// Gets an Enumeration of <see cref="DisplayType"/>s that are valid for a <see cref="WordWatch"/>
		/// </summary>
		public static IEnumerable<DisplayType> ValidTypes
		{
			get
			{
				yield return DisplayType.Unsigned;
				yield return DisplayType.Signed;
				yield return DisplayType.Hex;
				yield return DisplayType.Binary;
				yield return DisplayType.FixedPoint_12_4;
			}
		}

		/// <summary>
		/// Get a list a <see cref="DisplayType"/> that can be used for this <see cref="WordWatch"/>
		/// </summary>
		/// <returns>An enumeration that contains all valid <see cref="DisplayType"/></returns>
		public override IEnumerable<DisplayType> AvailableTypes()
		{
			return ValidTypes;
		}

		/// <summary>
		/// Reset the previous value; set it to the current one
		/// </summary>
		public override void ResetPrevious()
		{
			_previous = GetWord();
		}

		/// <summary>
		/// Try to sets the value into the <see cref="MemoryDomain"/>
		/// at the current <see cref="Watch"/> address
		/// </summary>
		/// <param name="value">Value to set</param>
		/// <returns>True if value successfully sets; otherwise, false</returns>
		public override bool Poke(string value)
		{
			try
			{
				ushort val = 0;
				switch (Type)
				{
					case DisplayType.Unsigned:
						if (value.IsUnsigned())
						{
							val = (ushort)int.Parse(value);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.Signed:
						if (value.IsSigned())
						{
							val = (ushort)(short)int.Parse(value);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.Hex:
						if (value.IsHex())
						{
							val = (ushort)int.Parse(value, NumberStyles.HexNumber);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.Binary:
						if (value.IsBinary())
						{
							val = (ushort)Convert.ToInt32(value, 2);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.FixedPoint_12_4:
						if (value.IsFixedPoint())
						{
							val = (ushort)(double.Parse(value) * 16.0);
						}
						else
						{
							return false;
						}

						break;
				}

				if (Global.CheatList.Contains(Domain, Address))
				{
					var cheat = Global.CheatList.FirstOrDefault(c => c.Address == Address && c.Domain == Domain);
					if (cheat != (Cheat)null)
					{
						cheat.PokeValue(val);
						PokeWord(val);
						return true;
					}
				}

				PokeWord(val);
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
					_value = GetWord();

					if (_value != temp)
					{
						_previous = temp;
						ChangeCount++;
					}

					break;
				case PreviousType.LastFrame:
					_previous = _value;
					_value = GetWord();
					if (_value != Previous)
					{
						ChangeCount++;
					}

					break;
			}
		}

		// TODO: Implements IFormattable
		public string FormatValue(ushort val)
		{
			switch (Type)
			{
				default:
				case DisplayType.Unsigned:
					return val.ToString();
				case DisplayType.Signed:
					return ((short)val).ToString();
				case DisplayType.Hex:
					return val.ToHexString(4);
				case DisplayType.FixedPoint_12_4:
					return $"{val / 16.0:F4}";
				case DisplayType.Binary:
					return Convert.ToString(val, 2).PadLeft(16, '0').Insert(8, " ").Insert(4, " ").Insert(14, " ");
			}
		}

		/// <summary>
		/// Get a string representation of difference
		/// between current value and the previous one
		/// </summary>
		public override string Diff
		{
			get
			{
				string diff = "";
				int diffVal = _value - _previous;
				if (diffVal > 0)
				{
					diff = "+";
				}
				else if (diffVal < 0)
				{
					diff = "-";
				}

				return $"{diff}{((ushort)Math.Abs(diffVal))}";
			}
		}

		/// <summary>
		/// Get the maximum possible value
		/// </summary>
		public override uint MaxValue => ushort.MaxValue;

		/// <summary>
		/// Gets the current value
		/// </summary>
		public override int Value => GetWord();

		/// <summary>
		/// Gets the current value
		/// but with stuff I don't understand
		/// </summary>
		public override int ValueNoFreeze => GetWord(true);

		/// <summary>
		/// Get a string representation of the current value
		/// </summary>
		public override string ValueString => FormatValue(GetWord());

		/// <summary>
		/// Get the previous value
		/// </summary>
		public override int Previous => _previous;

		/// <summary>
		/// Get a string representation of the previous value
		/// </summary>
		public override string PreviousStr => FormatValue(_previous);
	}
}
