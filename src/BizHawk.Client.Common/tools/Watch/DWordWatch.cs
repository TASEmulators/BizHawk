using System;
using System.Collections.Generic;
using System.Globalization;

using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// This class holds a double word (32 bits) <see cref="Watch"/>
	/// </summary>
	public sealed class DWordWatch : Watch
	{
		private uint _value;
		private uint _previous;

		/// <summary>
		/// Initializes a new instance of the <see cref="DWordWatch"/> class
		/// </summary>
		/// <param name="domain"><see cref="MemoryDomain"/> where you want to track</param>
		/// <param name="address">The address you want to track</param>
		/// <param name="type">How you you want to display the value See <see cref="DisplayType"/></param>
		/// <param name="bigEndian">Specify the endianess. true for big endian</param>
		/// <param name="note">A custom note about the <see cref="Watch"/></param>
		/// <param name="value">Current value</param>
		/// <param name="previous">Previous value</param>
		/// <param name="changeCount">How many times value has changed</param>
		/// <exception cref="ArgumentException">Occurs when a <see cref="DisplayType"/> is incompatible with <see cref="WatchSize.DWord"/></exception>
		internal DWordWatch(MemoryDomain domain, long address, DisplayType type, bool bigEndian, string note, uint value, uint previous, int changeCount)
			: base(domain, address, WatchSize.DWord, type, bigEndian, note)
		{
			_value = value == 0 ? GetDWord() : value;
			_previous = previous;
			ChangeCount = changeCount;
		}

		/// <summary>
		/// Gets a list of <see cref="DisplayType"/> for a <see cref="DWordWatch"/>
		/// </summary>
		public static IEnumerable<DisplayType> ValidTypes
		{
			get
			{
				yield return DisplayType.Unsigned;
				yield return DisplayType.Signed;
				yield return DisplayType.Hex;
				yield return DisplayType.Binary;
				yield return DisplayType.FixedPoint_20_12;
				yield return DisplayType.FixedPoint_16_16;
				yield return DisplayType.Float;
			}
		}

		/// <summary>
		/// Get a list of <see cref="DisplayType"/> that can be used for a <see cref="DWordWatch"/>
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
				uint val = 0;
				switch (Type)
				{
					case DisplayType.Unsigned:
						if (value.IsUnsigned())
						{
							val = (uint)int.Parse(value);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.Signed:
						if (value.IsSigned())
						{
							val = (uint)int.Parse(value);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.Hex:
						if (value.IsHex())
						{
							val = (uint)int.Parse(value, NumberStyles.HexNumber);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.FixedPoint_20_12:
						if (value.IsFixedPoint())
						{
							val = (uint)(int)(double.Parse(value) * 4096.0);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.FixedPoint_16_16:
						if (value.IsFixedPoint())
						{
							val = (uint)(int)(double.Parse(value) * 65536.0);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.Float:
						if (value.IsFloat())
						{
							var bytes = BitConverter.GetBytes(float.Parse(value));
							val = BitConverter.ToUInt32(bytes, 0);
						}
						else
						{
							return false;
						}

						break;
				}

				PokeDWord(val);
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
		public override void Update(PreviousType previousType)
		{
			switch (previousType)
			{
				case PreviousType.Original:
					return;
				case PreviousType.LastChange:
					var temp = _value;
					_value = GetDWord();
					if (_value != temp)
					{
						_previous = _value;
						ChangeCount++;
					}

					break;
				case PreviousType.LastFrame:
					_previous = _value;
					_value = GetDWord();
					if (_value != Previous)
					{
						ChangeCount++;
					}

					break;
			}
		}

		// TODO: Implements IFormattable
		public string FormatValue(uint val)
		{
			string FormatFloat()
			{
				var bytes = BitConverter.GetBytes(val);
				var _float = BitConverter.ToSingle(bytes, 0);
				return _float.ToString();
			};

			return Type switch
			{
				_ when !IsValid => "-",
				DisplayType.Unsigned => val.ToString(),
				DisplayType.Signed => ((int)val).ToString(),
				DisplayType.Hex => $"{val:X8}",
				DisplayType.FixedPoint_20_12 => $"{(int)val / 4096.0:0.######}",
				DisplayType.FixedPoint_16_16 => $"{(int)val / 65536.0:0.######}",
				DisplayType.Float => FormatFloat(),
				_ => val.ToString()
			};
		}

		/// <summary>
		/// Get a string representation of difference
		/// between current value and the previous one
		/// </summary>
		public override string Diff => $"{_value - (long)_previous:+#;-#;0}";

		/// <summary>
		/// Returns true if the Watch is valid, false otherwise
		/// </summary>
		public override bool IsValid => Domain.Size == 0 || Address < (Domain.Size - 3);

		/// <summary>
		/// Get the maximum possible value
		/// </summary>
		public override uint MaxValue => uint.MaxValue;

		/// <summary>
		/// Get the current value
		/// </summary>
		public override int Value => (int)GetDWord();

		/// <summary>
		/// Get a string representation of the current value
		/// </summary>
		public override string ValueString => FormatValue(GetDWord());

		/// <summary>
		/// Get the previous value
		/// </summary>
		public override int Previous => (int)_previous;

		/// <summary>
		/// Get a string representation of the previous value
		/// </summary>
		public override string PreviousStr => FormatValue(_previous);
	}
}
