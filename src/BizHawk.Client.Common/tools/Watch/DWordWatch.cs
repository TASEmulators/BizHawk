using System.Collections.Generic;

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

		public override IEnumerable<WatchDisplayType> AvailableTypes()
		{
			return ValidTypes;
		}

		public override void ResetPrevious()
			=> _previous = GetDWord();

		public override bool Poke(string value)
		{
			try
			{
				PokeDWord(Watch.ParseValue(value, Size, Type));
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
			=> IsValid ? Watch.FormatValue(val, Size, Type) : "-";

		public override string Diff => $"{_value - (long)_previous:+#;-#;0}";

		public override bool IsValid => Domain.Size == 0 || Address < (Domain.Size - 3);

		public override uint MaxValue => uint.MaxValue;

		public override int Value => (int)GetDWord();

		public override string ValueString => FormatValue(GetDWord());

		public override uint Previous => _previous;

		public override string PreviousStr => FormatValue(_previous);
	}
}
