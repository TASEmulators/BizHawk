using System.Collections.Generic;
using System.Globalization;
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
		/// <param name="type">How you you want to display the value See <see cref="WatchDisplayType"/></param>
		/// <param name="bigEndian">Specify the endianess. true for big endian</param>
		/// <param name="note">A custom note about the <see cref="Watch"/></param>
		/// <param name="value">Current value</param>
		/// <param name="previous">Previous value</param>
		/// <param name="changeCount">How many times value has changed</param>
		/// <exception cref="ArgumentException">Occurs when a <see cref="WatchDisplayType"/> is incompatible with <see cref="WatchSize.Word"/></exception>
		internal WordWatch(MemoryDomain domain, long address, WatchDisplayType type, bool bigEndian, string note, ushort value, ushort previous, int changeCount)
			: base(domain, address, WatchSize.Word, type, bigEndian, note)
		{
			_value = value == 0 ? GetWord() : value;
			_previous = previous;
			ChangeCount = changeCount;
		}

		/// <summary>
		/// Gets an Enumeration of <see cref="WatchDisplayType"/>s that are valid for a <see cref="WordWatch"/>
		/// </summary>
		public static IEnumerable<WatchDisplayType> ValidTypes { get; } = [
			WatchDisplayType.Unsigned,
			WatchDisplayType.Signed,
			WatchDisplayType.Hex,
			WatchDisplayType.Binary,
			WatchDisplayType.FixedPoint_12_4,
		];

		/// <summary>
		/// Get a list a <see cref="WatchDisplayType"/> that can be used for this <see cref="WordWatch"/>
		/// </summary>
		/// <returns>An enumeration that contains all valid <see cref="WatchDisplayType"/></returns>
		public override IEnumerable<WatchDisplayType> AvailableTypes() => ValidTypes;

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
				ushort val = Type switch
				{
					WatchDisplayType.Unsigned => ushort.Parse(value),
					WatchDisplayType.Signed => (ushort)short.Parse(value),
					WatchDisplayType.Hex => ushort.Parse(value, NumberStyles.HexNumber),
					WatchDisplayType.Binary => Convert.ToUInt16(value, 2),
					WatchDisplayType.FixedPoint_12_4 => (ushort)(double.Parse(value, NumberFormatInfo.InvariantInfo) * 16.0),
					_ => 0
				};

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
		public override void Update(PreviousType previousType)
		{
			switch (previousType)
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
			return Type switch
			{
				_ when !IsValid => "-",
				WatchDisplayType.Unsigned => val.ToString(),
				WatchDisplayType.Signed => ((short) val).ToString(), WatchDisplayType.Hex => $"{val:X4}",
				WatchDisplayType.FixedPoint_12_4 => ((short)val / 16.0).ToString("F4", NumberFormatInfo.InvariantInfo),
				WatchDisplayType.Binary => Convert
					.ToString(val, 2)
					.PadLeft(16, '0')
					.Insert(8, " ")
					.Insert(4, " ")
					.Insert(14, " "),
				_ => val.ToString()
			};
		}

		/// <summary>
		/// Get a string representation of difference
		/// between current value and the previous one
		/// </summary>
		public override string Diff => $"{_value - _previous:+#;-#;0}";

		/// <summary>
		/// Returns true if the Watch is valid, false otherwise
		/// </summary>
		public override bool IsValid => Domain.Size == 0 || Address < (Domain.Size - 1);

		/// <summary>
		/// Get the maximum possible value
		/// </summary>
		public override uint MaxValue => ushort.MaxValue;

		/// <summary>
		/// Gets the current value
		/// </summary>
		public override int Value => GetWord();

		/// <summary>
		/// Get a string representation of the current value
		/// </summary>
		public override string ValueString => FormatValue(GetWord());

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
