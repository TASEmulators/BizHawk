using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class Cheat
	{
		#region Constructors

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

		public static Cheat Separator
		{
			get { return new Cheat(SeparatorWatch.Instance, 0, null, false); }
		}

		#endregion

		#region private parts

		private readonly Watch _watch;
		private int? _compare;
		private int _val;
		private bool _enabled;

		private void Changes()
		{
			if (Changed != null)
			{
				Changed(this);
			}
		}

		#endregion

		#region Properties

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
			get { return _watch.Value; }
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
			get { return IsSeparator ? String.Empty : _watch.Notes; }
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
						return String.Empty;
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
							return String.Empty;
						case Watch.WatchSize.Byte:
							return (_watch as ByteWatch).FormatValue((byte)_compare.Value);
						case Watch.WatchSize.Word:
							return (_watch as WordWatch).FormatValue((ushort)_compare.Value);
						case Watch.WatchSize.DWord:
							return (_watch as DWordWatch).FormatValue((uint)_compare.Value);
					}
				}
				else
				{
					return String.Empty;
				}
			}
		}

		public event CheatEventHandler Changed;

		#endregion

		#region Actions

		public void Enable()
		{
			if (!IsSeparator)
			{
				var wasEnabled = _enabled;
				_enabled = true;
				if (!wasEnabled)
				{
					Changes();
				}
			}
		}

		public void Disable()
		{
			if (!IsSeparator)
			{
				var wasEnabled = _enabled;
				_enabled = false;
				if (wasEnabled)
				{
					Changes();
				}
			}
		}

		public void Toggle()
		{
			if (!IsSeparator)
			{
				_enabled ^= true;
				Changes();
			}
		}

		public void Pulse()
		{
			if (!IsSeparator && _enabled)
			{
				if (_compare.HasValue)
				{
					if (_compare.Value == _watch.Value)
					{
						_watch.Poke(_val.ToString());
					}
				}
				else
				{
					_watch.Poke(_val.ToString());
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

		#endregion
	}
}
