using System.Collections.Generic;
using System.Globalization;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// This class holds a quad word (64 bits) <see cref="Watch"/>
	/// </summary>
	public sealed class QWordWatch : Watch
	{
		/// <summary>
		/// Gets a list of <see cref="WatchDisplayType"/> for a <see cref="QWordWatch"/>
		/// </summary>
		public static readonly IReadOnlyList<WatchDisplayType> ValidTypes = [
			WatchDisplayType.Unsigned,
			WatchDisplayType.Signed,
			WatchDisplayType.Hex,
			WatchDisplayType.Binary,
			WatchDisplayType.Float,
		];

		private ulong _value;

		private ulong _previous;

		/// <summary>
		/// Initializes a new instance of the <see cref="QWordWatch"/> class
		/// </summary>
		/// <param name="domain"><see cref="MemoryDomain"/> where you want to track</param>
		/// <param name="address">The address you want to track</param>
		/// <param name="type">selected format for displaying the value</param>
		/// <param name="bigEndian">Specify the endianess. true for big endian</param>
		/// <param name="note">A custom note about the <see cref="Watch"/></param>
		/// <param name="value">Current value</param>
		/// <param name="previous">Previous value</param>
		/// <param name="changeCount">How many times value has changed</param>
		/// <exception cref="ArgumentException">Occurs when a <see cref="WatchDisplayType"/> is incompatible with <see cref="WatchSize.QWord"/></exception>
		internal QWordWatch(
			MemoryDomain domain,
			long address,
			WatchDisplayType type,
			bool bigEndian,
			string note,
			ulong value,
			ulong previous,
			int changeCount)
				: base(domain, address, WatchSize.QWord, type, bigEndian: bigEndian, note)
		{
			_value = value is 0 ? GetQWord() : value;
			_previous = previous;
			ChangeCount = changeCount;
		}

		/// <summary>
		/// Get a list of <see cref="WatchDisplayType"/> that can be used for a <see cref="QWordWatch"/>
		/// </summary>
		/// <returns>An enumeration that contains all valid <see cref="WatchDisplayType"/></returns>
		public override IReadOnlyList<WatchDisplayType> AvailableTypes()
			=> ValidTypes;

		/// <summary>
		/// Reset the previous value; set it to the current one
		/// </summary>
		public override void ResetPrevious()
			=> _previous = GetQWord();

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
				PokeQWord(Type switch
				{
					WatchDisplayType.Unsigned => ulong.Parse(value),
					WatchDisplayType.Signed => (ulong) long.Parse(value),
					WatchDisplayType.Hex => ulong.Parse(value, NumberStyles.HexNumber),
					WatchDisplayType.Float => NumberExtensions.ReinterpretAsUInt64(double.Parse(value, NumberFormatInfo.InvariantInfo)),
					WatchDisplayType.Binary => Convert.ToUInt64(value, fromBase: 2),
					_ => 0,
				});
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
					var nextValue = GetQWord();
					if (nextValue != _value)
					{
						_previous = nextValue;
						ChangeCount++;
					}
					_value = nextValue;
					break;
				case PreviousType.LastFrame:
					_previous = _value;
					_value = GetQWord();
					if (_value != Previous) ChangeCount++;
					break;
			}
		}

		// TODO: Implements IFormattable
		public string FormatValue(ulong val)
		{
			string FormatFloat()
				=> NumberExtensions.ReinterpretAsF64(val).ToString(NumberFormatInfo.InvariantInfo);
			string FormatBinary()
			{
				var str = Convert.ToString(unchecked((long) val), toBase: 2).PadLeft(64, '0');
				for (var i = 60; i > 0; i -= 4) str = str.Insert(i, " ");
				return str;
			}
			return Type switch
			{
				_ when !IsValid => "-",
				WatchDisplayType.Unsigned => val.ToString(),
				WatchDisplayType.Signed => ((long) val).ToString(),
				WatchDisplayType.Hex => $"{val:X16}",
				WatchDisplayType.Float => FormatFloat(),
				WatchDisplayType.Binary => FormatBinary(),
				_ => val.ToString(),
			};
		}

		/// <summary>
		/// Get a string representation of difference
		/// between current value and the previous one
		/// </summary>
		public override string Diff
			=> $"{unchecked((long) _value - (long) _previous):+#;-#;0}";

		/// <summary>
		/// Returns true if the Watch is valid, false otherwise
		/// </summary>
		public override bool IsValid
			=> Domain.Size is 0 || Address < (Domain.Size - (sizeof(ulong) - 1));

		/// <summary>
		/// Get the maximum possible value
		/// </summary>
		public override ulong MaxValue
			=> ulong.MaxValue;

		/// <summary>
		/// Get the current value
		/// </summary>
		public override long Value
			=> unchecked((long) GetQWord());

		/// <summary>
		/// Get a string representation of the current value
		/// </summary>
		public override string ValueString
			=> FormatValue(GetQWord());

		/// <summary>
		/// Get the previous value
		/// </summary>
		public override ulong Previous
			=> _previous;

		/// <summary>
		/// Get a string representation of the previous value
		/// </summary>
		public override string PreviousStr
			=> FormatValue(_previous);
	}
}
