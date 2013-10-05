using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
	public class NewCheat
	{
		#region Constructors

		public NewCheat(Watch watch, int? compare = null, bool enabled = true)
		{
			_enabled = enabled;
			_watch = watch;
			_compare = compare;
			if (!_watch.IsSeparator)
			{
				_val = _watch.Value.Value;
			}
		}

		public NewCheat(NewCheat cheat)
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
				_watch = Watch.GenerateWatch(cheat.Domain,
					cheat.Address.Value,
					cheat.Size,
					cheat.Type,
					cheat.Name,
					cheat.BigEndian.Value
					);
				_compare = cheat.Compare;
			}
		}

		public static NewCheat Separator
		{
			get { return new NewCheat(SeparatorWatch.Instance, null, false); }
		}

		#endregion

		#region Properties

		public bool IsSeparator
		{
			get { return _watch.IsSeparator; }
		}

		public bool Enabled
		{
			get { if (IsSeparator) return false; else return _enabled; }
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
			get { if (IsSeparator) return null; else return _watch.BigEndian; }
		}

		public int? Compare
		{
			get { if (_compare.HasValue && !IsSeparator) return _compare; else return null; }
		}

		public MemoryDomain Domain { get { return _watch.Domain; } }

		public Watch.WatchSize Size
		{
			get { return _watch.Size; }
		}

		public Watch.DisplayType Type
		{
			get { return _watch.Type; }
		}

		public string Name
		{
			get { if (IsSeparator) return String.Empty; else return _watch.Notes; }
		}

		public string AddressStr
		{
			get { return _watch.AddressString; }
		}

		public string ValueStr
		{
			get { return _watch.ValueString; }
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

		#endregion

		#region Actions

		public void Enable()
		{
			if (!IsSeparator)
			{
				_enabled = true;
			}
		}

		public void Disable()
		{
			if (!IsSeparator)
			{
				_enabled = false;
			}
		}

		public void Toggle()
		{
			if (!IsSeparator)
			{
				_enabled ^= true;
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
					return _watch.Address.Value == addr;
				case Watch.WatchSize.Word:
					return (addr == _watch.Address.Value) || (addr == _watch.Address + 1);
				case Watch.WatchSize.DWord:
					return (addr == _watch.Address.Value) || (addr == _watch.Address + 1) ||
						(addr == _watch.Address.Value + 2) || (addr == _watch.Address + 3);
			}
		}

		public void Increment()
		{
			if (!IsSeparator)
			{
				_val++;
			}
		}

		public void Decrement()
		{
			if (!IsSeparator)
			{
				_val--;
			}
		}

		#endregion

		#region private parts

		private Watch _watch;
		private int? _compare;
		private int _val;
		private bool _enabled;

		#endregion
	}
}
