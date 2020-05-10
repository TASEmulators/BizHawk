using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class Cheat
	{
		public enum CompareType
		{
			None,
			Equal,
			GreaterThan,
			GreaterThanOrEqual,
			LessThan,
			LessThanOrEqual,
			NotEqual
		}

		private readonly Watch _watch;
		private int? _compare;
		private int _val;
		private bool _enabled;

		public Cheat(Watch watch, int value, int? compare = null, bool enabled = true, CompareType comparisonType = CompareType.None)
		{
			_enabled = enabled;
			_watch = watch;
			_compare = compare;
			_val = value;
			ComparisonType = comparisonType;

			Pulse();
		}

		public Cheat(Cheat cheat)
		{
			if (cheat.IsSeparator)
			{
				_enabled = false;
				_watch = SeparatorWatch.Instance;
				_compare = null;
			}
			else
			{
				_enabled = cheat.Enabled;
				_watch = Watch.GenerateWatch(
					cheat.Domain,
					cheat.Address ?? 0,
					cheat.Size,
					cheat.Type,
					cheat.BigEndian ?? false,
					cheat.Name);
				_compare = cheat.Compare;
				_val = cheat.Value ?? 0;

				Pulse();
			}
		}

		public delegate void CheatEventHandler(object sender);
		public event CheatEventHandler Changed;

		public static Cheat Separator => new Cheat(SeparatorWatch.Instance, 0, null, false);

		public bool IsSeparator => _watch.IsSeparator;

		public bool Enabled => !IsSeparator && _enabled;

		public long? Address => _watch.Address;

		public int? Value => IsSeparator ? (int?)null : _val;

		public bool? BigEndian => IsSeparator ? (bool?)null : _watch.BigEndian;

		public int? Compare => _compare.HasValue && !IsSeparator ? _compare : null;

		public MemoryDomain Domain => _watch.Domain;

		public WatchSize Size => _watch.Size;

		public char SizeAsChar => _watch.SizeAsChar;

		public DisplayType Type => _watch.Type;

		public char TypeAsChar => _watch.TypeAsChar;

		public string Name => IsSeparator ? "" : _watch.Notes;

		public string AddressStr => _watch.AddressString;

		public string ValueStr =>
			_watch.Size switch
				{
					WatchSize.Byte => ((ByteWatch) _watch).FormatValue((byte)_val),
					WatchSize.Word => ((WordWatch) _watch).FormatValue((ushort)_val),
					WatchSize.DWord => ((DWordWatch) _watch).FormatValue((uint)_val),
					WatchSize.Separator => "",
					_ => ""
				};

		public string CompareStr
		{
			get
			{
				if (_compare.HasValue)
				{
					return _watch.Size switch
					{
						WatchSize.Byte => ((ByteWatch) _watch).FormatValue((byte)_compare.Value),
						WatchSize.Word => ((WordWatch) _watch).FormatValue((ushort)_compare.Value),
						WatchSize.DWord => ((DWordWatch) _watch).FormatValue((uint)_compare.Value),
						WatchSize.Separator => "",
						_ => ""
					};
				}
				
				return "";
			}
		}

		public CompareType ComparisonType { get; }

		public void Enable(bool handleChange = true)
		{
			if (!IsSeparator)
			{
				var wasEnabled = _enabled;
				_enabled = true;
				if (!wasEnabled && handleChange)
				{
					Changes();
				}
			}
		}

		public void Disable(bool handleChange = true)
		{
			if (!IsSeparator)
			{
				var wasEnabled = _enabled;
				_enabled = false;
				if (wasEnabled && handleChange)
				{
					Changes();
				}
			}
		}

		public void Toggle(bool handleChange = true)
		{
			if (!IsSeparator)
			{
				_enabled ^= true;
				if (handleChange)
				{
					Changes();
				}
			}
		}

		private string GetStringForPulse(int val) =>
			_watch.Type == DisplayType.Hex
				? val.ToString("X8")
				: val.ToString();

		public void Pulse()
		{
			if (!IsSeparator && _enabled)
			{
				if (_compare.HasValue)
				{
					switch (ComparisonType)
					{
						default:
						case CompareType.None: // This should never happen, but it's here just in case.  adelikat: And yet it does! Cheat Code converter doesn't do this.  Changing this to default to equal since 99.9999% of all cheats are going to be equals
						case CompareType.Equal:
							if (_compare.Value == _watch.ValueNoFreeze) 
							{
								_watch.Poke(GetStringForPulse(_val));
							}

							break;
						case CompareType.GreaterThan:
							if (_compare.Value > _watch.ValueNoFreeze) 
							{
								_watch.Poke(GetStringForPulse(_val));
							}

							break;
						case CompareType.GreaterThanOrEqual:
							if (_compare.Value >= _watch.ValueNoFreeze)
							{
								_watch.Poke(GetStringForPulse(_val));
							}

							break;
						case CompareType.LessThan:
							if (_compare.Value < _watch.ValueNoFreeze) 
							{
								_watch.Poke(GetStringForPulse(_val));
							}

							break;
						case CompareType.LessThanOrEqual:
							if (_compare.Value <= _watch.ValueNoFreeze)
							{
								_watch.Poke(GetStringForPulse(_val));
							}

							break;
						case CompareType.NotEqual:
							if (_compare.Value != _watch.ValueNoFreeze)
							{
								_watch.Poke(GetStringForPulse(_val));
							}

							break;
					}
				}
				else
				{
					switch (_watch.Size)
					{
						case WatchSize.Byte:
							_watch.Poke(((ByteWatch)_watch).FormatValue((byte)_val));
							break;
						case WatchSize.Word:
							_watch.Poke(((WordWatch)_watch).FormatValue((ushort)_val));
							break;
						case WatchSize.DWord:
							_watch.Poke(((DWordWatch)_watch).FormatValue((uint)_val));
							break;
					}
				}
			}
		}

		public bool Contains(long addr)
		{
			switch (_watch.Size)
			{
				default:
				case WatchSize.Separator:
					return false;
				case WatchSize.Byte:
					return _watch.Address == addr;
				case WatchSize.Word:
					return addr == _watch.Address || addr == _watch.Address + 1;
				case WatchSize.DWord:
					return addr == _watch.Address || addr == _watch.Address + 1 ||
						addr == _watch.Address + 2 || addr == _watch.Address + 3;
			}
		}

		public byte? GetByteVal(long addr)
		{
			if (!Contains(addr))
			{
				return null;
			}

			switch (_watch.Size)
			{
				default:
				case WatchSize.Separator:
				case WatchSize.Byte:
					return (byte?)_val;
				case WatchSize.Word:
					if (_watch.BigEndian)
					{
						if (addr == _watch.Address)
						{
							return (byte)(_val >> 8);
						}
						return (byte)(_val & 0xFF);
					}
					else
					{
						if (addr == _watch.Address)
						{
							return (byte)(_val & 0xFF);
						}
						return (byte)(_val >> 8);
					}
				case WatchSize.DWord:
					if (_watch.BigEndian)
					{
						if (addr == _watch.Address)
						{
							return (byte)((_val >> 24) & 0xFF);
						}

						if (addr == _watch.Address + 1)
						{
							return (byte)((_val >> 16) & 0xFF);
						}

						if (addr == _watch.Address + 2)
						{
							return (byte)((_val >> 8) & 0xFF);
						}

						return (byte)(_val & 0xFF);
					}
					else
					{
						if (addr == _watch.Address)
						{
							return (byte)(_val & 0xFF);
						}

						if (addr == _watch.Address + 1)
						{
							return (byte)((_val >> 8) & 0xFF);
						}

						if (addr == _watch.Address + 2)
						{
							return (byte)((_val >> 16) & 0xFF);
						}

						return (byte)((_val >> 24) & 0xFF);
					}
			}
		}

		public void PokeValue(int val)
		{
			if (!IsSeparator)
			{
				_val = val;
			}
		}

		public void Increment()
		{
			if (!IsSeparator)
			{
				_val++;
				if (_val > _watch.MaxValue)
				{
					_val = 0;
				}

				Pulse();
				Changes();
			}
		}

		public void Decrement()
		{
			if (!IsSeparator)
			{
				_val--;
				if ((uint)_val > _watch.MaxValue)
				{
					_val = (int)_watch.MaxValue;
				}

				Pulse();
				Changes();
			}
		}

		public void SetType(DisplayType type)
		{
			if (_watch.IsDisplayTypeAvailable(type))
			{
				_watch.Type = type;
				Changes();
			}
		}

		private void Changes()
		{
			Changed?.Invoke(this);
		}

		public override bool Equals(object obj)
		{
			if (obj is Watch watch)
			{
				return Domain == watch.Domain && Address == watch.Address;
			}

			if (obj is Cheat cheat)
			{
				return Domain == cheat.Domain && Address == cheat.Address;
			}

			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return Domain.GetHashCode() + (int)(Address ?? 0);
		}

		public static bool operator ==(Cheat a, Cheat b)
		{
			// If one is null, but not both, return false.
			if ((object)a == null || (object)b == null)
			{
				return false;
			}

			return a.Domain == b.Domain && a.Address == b.Address;
		}

		public static bool operator !=(Cheat a, Cheat b)
		{
			return !(a == b);
		}

		public static bool operator ==(Cheat a, Watch b)
		{
			// If one is null, but not both, return false.
			if ((object)a == null || (object)b == null)
			{
				return false;
			}

			return a.Domain == b.Domain && a.Address == b.Address;
		}

		public static bool operator !=(Cheat a, Watch b)
		{
			return !(a == b);
		}
	}
}
