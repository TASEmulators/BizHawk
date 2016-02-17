using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class Cheat
	{
		public enum COMPARISONTYPE
		{
			NONE,
			EQUAL,
			GREATER_THAN,
			GREATER_THAN_OR_EQUAL,
			LESS_THAN,
			LESS_THAN_OR_EQUAL,
			NOT_EQUAL
		};

		private readonly Watch _watch;
		private int? _compare;
		private int _val;
		private bool _enabled;
		private COMPARISONTYPE _comparisonType;
		

		public Cheat(Watch watch, int value, int? compare = null, bool enabled = true, COMPARISONTYPE comparisonType = COMPARISONTYPE.NONE)
		{
			_enabled = enabled;
			_watch = watch;
			_compare = compare;
			_val = value;
			_comparisonType = comparisonType;

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
					cheat.Name
				);
				_compare = cheat.Compare;
				_val = cheat.Value ?? 0;

				Pulse();
			}
		}

		public delegate void CheatEventHandler(object sender);
		public event CheatEventHandler Changed;

		public static Cheat Separator
		{
			get { return new Cheat(SeparatorWatch.Instance, 0, null, false); }
		}

		public bool IsSeparator
		{
			get { return _watch.IsSeparator; }
		}

		public bool Enabled
		{
			get { return !IsSeparator && _enabled; }
		}

		public long? Address
		{
			get { return _watch.Address; }
		}

		public int? Value
		{
			get { return IsSeparator ? (int?)null : _val; }
		}

		public bool? BigEndian
		{
			get { return IsSeparator ? (bool?)null : _watch.BigEndian; }
		}

		public int? Compare
		{
			get { return _compare.HasValue && !IsSeparator ? _compare : null; }
		}

		public MemoryDomain Domain
		{
			get { return _watch.Domain; }
		}

		public WatchSize Size
		{
			get { return _watch.Size; }
		}

		public char SizeAsChar
		{
			get { return _watch.SizeAsChar; }
		}

		public DisplayType Type
		{
			get { return _watch.Type; }
		}

		public char TypeAsChar
		{
			get { return _watch.TypeAsChar; }
		}

		public string Name
		{
			get { return IsSeparator ? string.Empty : _watch.Notes; }
		}

		public string AddressStr
		{
			get { return _watch.AddressString; }
		}

		public string ValueStr
		{
			get
			{
				switch (_watch.Size)
				{
					default:
					case WatchSize.Separator:
						return string.Empty;
					case WatchSize.Byte:
						return (_watch as ByteWatch).FormatValue((byte)_val);
					case WatchSize.Word:
						return (_watch as WordWatch).FormatValue((ushort)_val);
					case WatchSize.DWord:
						return (_watch as DWordWatch).FormatValue((uint)_val);
				}
			}
		}

		public string CompareStr
		{
			get
			{
				if (_compare.HasValue)
				{
					switch (_watch.Size)
					{
						default:
						case WatchSize.Separator:
							return string.Empty;
						case WatchSize.Byte:
							return (_watch as ByteWatch).FormatValue((byte)_compare.Value);
						case WatchSize.Word:
							return (_watch as WordWatch).FormatValue((ushort)_compare.Value);
						case WatchSize.DWord:
							return (_watch as DWordWatch).FormatValue((uint)_compare.Value);
					}
				}
				
				return string.Empty;
			}
		}

		public COMPARISONTYPE ComparisonType
		{
			get { return _comparisonType; }
		}

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

		private string GetStringForPulse(int val)
		{
			if (_watch.Type == DisplayType.Hex)
			{
				return val.ToString("X8");
			}
			
			return val.ToString();
		}

		public void Pulse()
		{
			if (!IsSeparator && _enabled)
			{
				if (_compare.HasValue)
				{
					switch (_comparisonType)
					{
						default:
						case COMPARISONTYPE.NONE: // This should never happen, but it's here just in case
							break;
						case COMPARISONTYPE.EQUAL:
							if (_compare.Value == _watch.ValueNoFreeze) 
							{
								_watch.Poke(GetStringForPulse(_val));
							}
							break;
						case COMPARISONTYPE.GREATER_THAN:
							if (_compare.Value > _watch.ValueNoFreeze) 
							{
								_watch.Poke(GetStringForPulse(_val));
							}
							break;
						case COMPARISONTYPE.GREATER_THAN_OR_EQUAL:
							if (_compare.Value >= _watch.ValueNoFreeze)
							{
								_watch.Poke(GetStringForPulse(_val));
							}
							break;
						case COMPARISONTYPE.LESS_THAN:
							if (_compare.Value < _watch.ValueNoFreeze) 
							{
								_watch.Poke(GetStringForPulse(_val));
							}
							break;
						case COMPARISONTYPE.LESS_THAN_OR_EQUAL:
							if (_compare.Value <= _watch.ValueNoFreeze)
							{
								_watch.Poke(GetStringForPulse(_val));
							}
							break;
						case COMPARISONTYPE.NOT_EQUAL:
							if (_compare.Value != _watch.ValueNoFreeze)
							{
								_watch.Poke(GetStringForPulse(_val));
							}
							break;
					}		
				}
				else
				{
					switch(_watch.Size)
					{
						case WatchSize.Byte:
							_watch.Poke((_watch as ByteWatch).FormatValue((byte)_val));
							break;
						case WatchSize.Word:
							_watch.Poke((_watch as WordWatch).FormatValue((ushort)_val));
							break;
						case WatchSize.DWord:
							_watch.Poke((_watch as DWordWatch).FormatValue((uint)_val));
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
					return (addr == _watch.Address) || (addr == (_watch.Address) + 1);
				case WatchSize.DWord:
					return (addr == (_watch.Address)) || (addr == (_watch.Address) + 1) ||
						(addr == (_watch.Address) + 2) || (addr == (_watch.Address) + 3);
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
					if (addr == (_watch.Address))
					{
						return (byte)(_val >> 8);
					}

					return (byte)(_val & 0xFF);
				case WatchSize.DWord:
					if (addr == (_watch.Address))
					{
						return (byte)((_val >> 24) & 0xFF);
					}
					else if (addr == (_watch.Address) + 1)
					{
						return (byte)((_val >> 16) & 0xFF);
					}
					else if (addr == ((_watch.Address)) + 2)
					{
						return (byte)((_val >> 8) & 0xFF);
					}

					return (byte)(_val & 0xFF);
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
			if (_watch.IsDiplayTypeAvailable(type))
			{
				_watch.Type = type;
				Changes();
			}
		}

		private void Changes()
		{
			if (Changed != null)
			{
				Changed(this);
			}
		}

		public override bool Equals(object obj)
		{
			if (obj is Watch)
			{
				var watch = obj as Watch;
				return Domain == watch.Domain && Address == watch.Address;
			}

			if (obj is Cheat)
			{
				var cheat = obj as Cheat;
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
			if (((object)a == null) || ((object)b == null))
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
			if (((object)a == null) || ((object)b == null))
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
