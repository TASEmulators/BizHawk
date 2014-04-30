using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class Cheat
	{
		private readonly Watch _watch;
		private int? _compare;
		private int _val;
		private bool _enabled;

		public Cheat(Watch watch, int value, int? compare = null, bool enabled = true)
		{
			_enabled = enabled;
			_watch = watch;
			_compare = compare;
			_val = value;

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
					cheat.Name,
					cheat.BigEndian ?? false);
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

		public int? Address
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

		public Watch.WatchSize Size
		{
			get { return _watch.Size; }
		}

		public char SizeAsChar
		{
			get { return _watch.SizeAsChar; }
		}

		public Watch.DisplayType Type
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
					case Watch.WatchSize.Separator:
						return string.Empty;
					case Watch.WatchSize.Byte:
						return (_watch as ByteWatch).FormatValue((byte)_val);
					case Watch.WatchSize.Word:
						return (_watch as WordWatch).FormatValue((ushort)_val);
					case Watch.WatchSize.DWord:
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
						case Watch.WatchSize.Separator:
							return string.Empty;
						case Watch.WatchSize.Byte:
							return (_watch as ByteWatch).FormatValue((byte)_compare.Value);
						case Watch.WatchSize.Word:
							return (_watch as WordWatch).FormatValue((ushort)_compare.Value);
						case Watch.WatchSize.DWord:
							return (_watch as DWordWatch).FormatValue((uint)_compare.Value);
					}
				}
				
				return string.Empty;
			}
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
			if (_watch.Type == Watch.DisplayType.Hex)
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
					if (_compare.Value == _watch.Value)
					{
						_watch.Poke(GetStringForPulse(_val));
					}
				}
				else
				{
					_watch.Poke(GetStringForPulse(_val));
				}
			}
		}

		public bool Contains(int addr)
		{
			switch (_watch.Size)
			{
				default:
				case Watch.WatchSize.Separator:
					return false;
				case Watch.WatchSize.Byte:
					return (_watch.Address ?? 0) == addr;
				case Watch.WatchSize.Word:
					return (addr == (_watch.Address ?? 0)) || (addr == (_watch.Address ?? 0) + 1);
				case Watch.WatchSize.DWord:
					return (addr == (_watch.Address ?? 0)) || (addr == (_watch.Address ?? 0) + 1) ||
						(addr == (_watch.Address ?? 0) + 2) || (addr == (_watch.Address ?? 0) + 3);
			}
		}

		public byte? GetByteVal(int addr)
		{
			if (!Contains(addr))
			{
				return null;
			}

			switch (_watch.Size)
			{
				default:
				case Watch.WatchSize.Separator:
				case Watch.WatchSize.Byte:
					return (byte?)_val;
				case Watch.WatchSize.Word:
					if (addr == (_watch.Address ?? 0))
					{
						return (byte)(_val >> 8);
					}

					return (byte)(_val & 0xFF);
				case Watch.WatchSize.DWord:
					if (addr == (_watch.Address ?? 0))
					{
						return (byte)((_val >> 24) & 0xFF);
					}
					else if (addr == (_watch.Address ?? 0) + 1)
					{
						return (byte)((_val >> 16) & 0xFF);
					}
					else if (addr == ((_watch.Address ?? 0)) + 2)
					{
						return (byte)((_val >> 8) & 0xFF);
					}

					return (byte)(_val & 0xFF);
			}
		}

		public void Increment()
		{
			if (!IsSeparator)
			{
				_val++;
				Pulse();
				Changes();
			}
		}

		public void Decrement()
		{
			if (!IsSeparator)
			{
				_val--;
				Pulse();
				Changes();
			}
		}

		public void SetType(Watch.DisplayType type)
		{
			if (Watch.AvailableTypes(_watch.Size).Contains(type))
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
				return this.Domain == watch.Domain && this.Address == watch.Address;
			}

			if (obj is Cheat)
			{
				var cheat = obj as Cheat;
				return this.Domain == cheat.Domain && this.Address == cheat.Address;
			}

			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return this.Domain.GetHashCode() + this.Address ?? 0;
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
