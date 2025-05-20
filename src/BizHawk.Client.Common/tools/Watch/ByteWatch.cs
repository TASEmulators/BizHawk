using System.Collections.Generic;

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

		public override IEnumerable<WatchDisplayType> AvailableTypes()
		{
			return ValidTypes;
		}

		public override void ResetPrevious()
		{
			_previous = GetByte();
		}

		public override bool Poke(string value)
		{
			try
			{
				PokeByte(unchecked((byte) Watch.ParseValue(value, Size, Type)));
				return true;
			}
			catch
			{
				return false;
			}
		}

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
			=> IsValid ? Watch.FormatValue(val, Size, Type) : "-";

		public override string Diff => $"{_value - _previous:+#;-#;0}";

		public override bool IsValid => Domain.Size == 0 || Address < Domain.Size;

		public override uint MaxValue => byte.MaxValue;

		public override int Value => GetByte();

		public override string ValueString => FormatValue(GetByte());

		public override uint Previous => _previous;

		public override string PreviousStr => FormatValue(_previous);
	}
}
