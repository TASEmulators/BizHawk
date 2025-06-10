using System.Collections.Generic;

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

		public override IEnumerable<WatchDisplayType> AvailableTypes() => ValidTypes;

		public override void ResetPrevious()
		{
			_previous = GetWord();
		}

		public override bool Poke(string value)
		{
			try
			{
				PokeWord(unchecked((ushort) Watch.ParseValue(value, Size, Type)));
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
			=> IsValid ? Watch.FormatValue(val, Size, Type) : "-";

		public override string Diff => $"{_value - _previous:+#;-#;0}";

		public override int Value => GetWord();

		public override string ValueString => FormatValue(GetWord());

		public override uint Previous => _previous;

		public override string PreviousStr => FormatValue(_previous);
	}
}
