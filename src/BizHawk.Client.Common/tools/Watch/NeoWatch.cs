using System.Collections.Generic;
using System.Linq.Expressions;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>an extremely versatile implementation (pointers!), but with presumably lower performance</summary>
	public sealed class NeoWatch : Watch
	{
		private static long CollapseAddress(long address, MemoryDomain domain)
			=> address;

		private Expression _addressAST;

		private uint _previous;

		private uint _value;

		private Watch _wrapped;

		public override string AddressString
			=> _addressAST.ToString();

		public override string Diff
			=> $"{_value - (long) _previous:+#;-#;0}";

		public override uint Previous
			=> _previous;

		public override string PreviousStr
			=> Watch.FormatValue(_previous, Size, Type);

		public override int Value
			=> _wrapped.Value;

		public override string ValueString
			=> _wrapped.ValueString;

		public int Width;

		private NeoWatch(Watch wrapped)
			: base(
				wrapped.Domain,
				wrapped.Address,
				wrapped.Size,
				wrapped.Type,
				bigEndian: wrapped.BigEndian,
				note: wrapped.Notes)
			=> _wrapped = wrapped;

		internal NeoWatch(
				MemoryDomain domain,
				long address,
				WatchSize size,
				WatchDisplayType type,
				bool bigEndian)
			: this(Watch.GenerateWatch(
				domain,
				CollapseAddress(address, domain),
				size,
				type,
				bigEndian: bigEndian)) {}

		public override IEnumerable<WatchDisplayType> AvailableTypes()
			=> Size switch
			{
				WatchSize.Byte => ByteWatch.ValidTypes,
				WatchSize.Word => WordWatch.ValidTypes,
				WatchSize.DWord => DWordWatch.ValidTypes,
				_ => [ ],
			};

		private void CollapseAddress()
		{
			//TODO
		}

		private uint CollapseAndPeek()
		{
			CollapseAddress();
			return Size switch
			{
				WatchSize.Byte => GetByte(),
				WatchSize.Word => GetWord(),
				_ => GetDWord(),
			};
		}

		public override bool Poke(string value)
		{
			CollapseAddress();
			try
			{
				var parsed = Watch.ParseValue(value, Size, Type);
				switch (Size)
				{
					case WatchSize.Byte:
						PokeByte(unchecked((byte) parsed));
						break;
					case WatchSize.Word:
						PokeWord(unchecked((ushort) parsed));
						break;
					case WatchSize.DWord:
						PokeDWord(parsed);
						break;
				}
				return true;
			}
			catch
			{
				return false;
			}
		}

		public override void ResetPrevious()
			=> _previous = CollapseAndPeek();

		public override void Update(PreviousType previousType)
		{
			switch (previousType)
			{
				case PreviousType.Original:
					CollapseAddress();
					// no-op
					break;
				case PreviousType.LastSearch:
					CollapseAddress();
					//TODO no-op?
					break;
				case PreviousType.LastFrame:
					_previous = _value;
					_value = CollapseAndPeek();
					if (_value != _previous) ChangeCount++;
					break;
				case PreviousType.LastChange:
					var newValue = CollapseAndPeek();
					if (newValue != _value)
					{
						_previous = _value;
						ChangeCount++;
					}
					_value = newValue;
					break;
			}
		}
	}
}
