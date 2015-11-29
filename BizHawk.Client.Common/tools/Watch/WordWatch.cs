using BizHawk.Common.NumberExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BizHawk.Client.Common
{
	public sealed class WordWatch : Watch
	{
		#region Fields

		private ushort _previous;
		private ushort _value;

		#endregion

		#region cTor(s)

		internal WordWatch(MemoryDomain domain, long address, DisplayType type, bool bigEndian, string note, ushort value, ushort previous, int changeCount)
			: base(domain, address, WatchSize.Word, type, bigEndian, note)
		{
			this._value = value;
			this._previous = previous;
			this._changecount = changeCount;
		}

		internal WordWatch(MemoryDomain domain, long address, DisplayType type, bool bigEndian, string note)
			:this(domain, address, type, bigEndian, note, 0, 0, 0)
		{
			_previous = GetWord();
			_value = GetWord();
		}

		#endregion

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

		public override IEnumerable<DisplayType> AvailableTypes()
		{
			yield return DisplayType.Unsigned;
			yield return DisplayType.Signed;
			yield return DisplayType.Hex;
			yield return DisplayType.Binary;
			yield return DisplayType.FixedPoint_12_4;
		}

		public override uint MaxValue
		{
			get { return ushort.MaxValue; }
		}

		public override int Value
		{
			get { return GetWord(); }
		}

		public override int ValueNoFreeze
		{
			get { return GetWord(true); }
		}

		public override int Previous
		{
			get { return _previous; }
		}

		public override string PreviousStr
		{
			get { return FormatValue(_previous); }
		}

		public override void ResetPrevious()
		{
			_previous = GetWord();
		}

		public override string ValueString
		{
			get { return FormatValue(GetWord()); }
		}

		public override string ToString()
		{
			return Notes + ": " + ValueString;
		}

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
					return string.Format("{0:F4}", val / 16.0);
				case DisplayType.Binary:
					return Convert.ToString(val, 2).PadLeft(16, '0').Insert(8, " ").Insert(4, " ").Insert(14, " ");
			}
		}

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

				if (Global.CheatList.Contains(Domain, _address))
				{
					var cheat = Global.CheatList.FirstOrDefault(c => c.Address == _address && c.Domain == Domain);
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

		public override string Diff
		{
			get { return FormatValue((ushort)(_previous - _value)); }
		}

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
						_changecount++;
					}

					break;
				case PreviousType.LastFrame:
					_previous = _value;
					_value = GetWord();
					if (_value != Previous)
					{
						_changecount++;
					}

					break;
			}
		}
	}
}
