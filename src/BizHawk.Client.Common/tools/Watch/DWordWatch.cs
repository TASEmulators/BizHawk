using System.Collections.Generic;
using System.Globalization;
using BizHawk.Common.NumberExtensions;
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
		/// <param name="type">How you you want to display the value See <see cref="WatchDisplayType"/></param>
		/// <param name="bigEndian">Specify the endianess. true for big endian</param>
		/// <param name="note">A custom note about the <see cref="Watch"/></param>
		/// <param name="value">Current value</param>
		/// <param name="previous">Previous value</param>
		/// <param name="changeCount">How many times value has changed</param>
		/// <exception cref="ArgumentException">Occurs when a <see cref="WatchDisplayType"/> is incompatible with <see cref="WatchSize.DWord"/></exception>
		internal DWordWatch(MemoryDomain domain, long address, WatchDisplayType type, bool bigEndian, string note, uint value, uint previous, int changeCount)
			: base(domain, address, WatchSize.DWord, type, bigEndian, note)
		{
			_value = value == 0 ? GetDWord() : value;
			_previous = previous;
			ChangeCount = changeCount;
		}

		/// <summary>
		/// Gets a list of <see cref="WatchDisplayType"/> for a <see cref="DWordWatch"/>
		/// </summary>
		public static IEnumerable<WatchDisplayType> ValidTypes { get; } = [
			WatchDisplayType.Unsigned,
			WatchDisplayType.Signed,
			WatchDisplayType.Hex,
			WatchDisplayType.Binary,
			WatchDisplayType.FixedPoint_20_12,
			WatchDisplayType.FixedPoint_16_16,
			WatchDisplayType.Float,
		];

		/// <summary>
		/// Get a list of <see cref="WatchDisplayType"/> that can be used for a <see cref="DWordWatch"/>
		/// </summary>
		/// <returns>An enumeration that contains all valid <see cref="WatchDisplayType"/></returns>
		public override IEnumerable<WatchDisplayType> AvailableTypes()
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
				uint val = Type switch
				{
					WatchDisplayType.Unsigned => uint.Parse(value),
					WatchDisplayType.Signed => (uint)int.Parse(value),
					WatchDisplayType.Hex => uint.Parse(value, NumberStyles.HexNumber),
					WatchDisplayType.FixedPoint_20_12 => (uint)(double.Parse(value, NumberFormatInfo.InvariantInfo) * 4096.0),
					WatchDisplayType.FixedPoint_16_16 => (uint)(double.Parse(value, NumberFormatInfo.InvariantInfo) * 65536.0),
					WatchDisplayType.Float => NumberExtensions.ReinterpretAsUInt32(float.Parse(value, NumberFormatInfo.InvariantInfo)),
					WatchDisplayType.Binary => Convert.ToUInt32(value, 2),
					_ => 0
				};

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
				var _float = NumberExtensions.ReinterpretAsF32(val);
				return _float.ToString(NumberFormatInfo.InvariantInfo);
			}

			string FormatBinary()
			{
				var str = Convert.ToString(val, 2).PadLeft(32, '0');
				for (var i = 28; i > 0; i -= 4)
				{
					str = str.Insert(i, " ");
				}
				return str;
			}

			return Type switch
			{
				_ when !IsValid => "-",
				WatchDisplayType.Unsigned => val.ToString(),
				WatchDisplayType.Signed => ((int)val).ToString(),
				WatchDisplayType.Hex => $"{val:X8}",
				WatchDisplayType.FixedPoint_20_12 => ((int)val / 4096.0).ToString("0.######", NumberFormatInfo.InvariantInfo),
				WatchDisplayType.FixedPoint_16_16 => ((int)val / 65536.0).ToString("0.######", NumberFormatInfo.InvariantInfo),
				WatchDisplayType.Float => FormatFloat(),
				WatchDisplayType.Binary => FormatBinary(),
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
		public override uint Previous => _previous;

		/// <summary>
		/// Get a string representation of the previous value
		/// </summary>
		public override string PreviousStr => FormatValue(_previous);
	}
}
