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

		public static NewCheat SeparatorInstance
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
			set { if (!IsSeparator) { _enabled = value; } }
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

		#endregion

		#region Actions

		public void Pulse()
		{
			if (!_watch.IsSeparator && _enabled)
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
