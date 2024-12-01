using System.Collections.Generic;
using System.Globalization;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// This class holds a byte (8 bits) <see cref="Watch"/>
	/// </summary>
	public sealed class ByteWatch : Watch
	{
		private byte _previous;
		private byte _value;

		/// <summary>
		/// Initializes a new instance of the <see cref="ByteWatch"/> class.
		/// </summary>
		/// <param name="domain"><see cref="MemoryDomain"/> where you want to track</param>
		/// <param name="address">The address you want to track</param>
		/// <param name="type">How you you want to display the value See <see cref="WatchDisplayType"/></param>
		/// <param name="bigEndian">Specify the endianess. true for big endian</param>
		/// <param name="note">A custom note about the <see cref="Watch"/></param>
		/// <param name="value">Current value</param>
		/// <param name="previous">Previous value</param>
		/// <param name="changeCount">How many times value has changed</param>
		/// <exception cref="ArgumentException">Occurs when a <see cref="WatchDisplayType"/> is incompatible with <see cref="WatchSize.Byte"/></exception>
		internal ByteWatch(MemoryDomain domain, long address, WatchDisplayType type, bool bigEndian, string note, byte value, byte previous, int changeCount)
			: base(domain, address, WatchSize.Byte, type, bigEndian, note)
		{
			_value = value == 0 ? GetByte() : value;
			_previous = previous;
			ChangeCount = changeCount;
		}

		/// <summary>
		/// Gets an enumeration of <see cref="WatchDisplayType"/> that are valid for a <see cref="ByteWatch"/>
		/// </summary>
		public static IEnumerable<WatchDisplayType> ValidTypes { get; } = [
			WatchDisplayType.Unsigned,
			WatchDisplayType.Signed,
			WatchDisplayType.Hex,
			WatchDisplayType.Binary,
		];

		/// <summary>
		/// Get a list a <see cref="WatchDisplayType"/> that can be used for this <see cref="ByteWatch"/>
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
			_previous = GetByte();
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
				byte val = Type switch
				{
					WatchDisplayType.Unsigned => byte.Parse(value),
					WatchDisplayType.Signed => (byte)sbyte.Parse(value),
					WatchDisplayType.Hex => byte.Parse(value, NumberStyles.HexNumber),
					WatchDisplayType.Binary => Convert.ToByte(value, 2),
					_ => 0
				};

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
		public override void Update(PreviousType previousType)
		{
			switch (previousType)
			{
				case PreviousType.Original:
					return;
				case PreviousType.LastChange:
					var temp = _value;
					_value = GetByte();
					if (_value != temp)
					{
						_previous = _value;
						ChangeCount++;
					}

					break;
				case PreviousType.LastFrame:
					_previous = _value;
					_value = GetByte();
					if (_value != Previous)
					{
						ChangeCount++;
					}

					break;
			}
		}

		// TODO: Implements IFormattable
		public string FormatValue(byte val)
		{
			return Type switch
			{
				_ when !IsValid => "-",
				WatchDisplayType.Unsigned => val.ToString(),
				WatchDisplayType.Signed => ((sbyte) val).ToString(),
				WatchDisplayType.Hex => $"{val:X2}",
				WatchDisplayType.Binary => Convert.ToString(val, 2).PadLeft(8, '0').Insert(4, " "),
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
		public override bool IsValid => Domain.Size == 0 || Address < Domain.Size;

		/// <summary>
		/// Get the maximum possible value
		/// </summary>
		public override uint MaxValue => byte.MaxValue;

		/// <summary>
		/// Get the current value
		/// </summary>
		public override int Value => GetByte();

		/// <summary>
		/// Get a string representation of the current value
		/// </summary>
		public override string ValueString => FormatValue(GetByte());

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
