using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class ZwinderStateManager : IStateManager
	{
		private static readonly byte[] NonState = new byte[0];

		private byte[] _originalState;
		private readonly ZwinderBuffer _current;
		private readonly ZwinderBuffer _recent;
		private readonly List<KeyValuePair<int, byte[]>> _ancient = new List<KeyValuePair<int, byte[]>>();
		private readonly int _ancientInterval;

		public ZwinderStateManager()
		{
			Settings = new ZwinderStateManagerSettings();
			_current = new ZwinderBuffer(Settings.Current);
			_recent = new ZwinderBuffer(Settings.Recent);
			_ancientInterval = Settings.AncientStateInterval;
			_originalState = NonState;
		}

		public void Engage(byte[] frameZeroState)
		{
			_originalState = (byte[])frameZeroState.Clone();
		}

		private ZwinderStateManager(ZwinderBuffer current, ZwinderBuffer recent, byte[] frameZeroState, int ancientInterval)
		{
			_originalState = (byte[])frameZeroState.Clone();
			_current = current;
			_recent = recent;
			_ancientInterval = ancientInterval;
		}
		
		public byte[] this[int frame]
		{
			get
			{
				var kvp = GetStateClosestToFrame(frame + 1);
				if (kvp.Key != frame)
					return NonState;
				var ms = new MemoryStream();
				kvp.Value.CopyTo(ms);
				return ms.ToArray();
			}
		}

		// TODO: private set, refactor LoadTasprojExtras to hold onto a settings object and pass it in to Create() method
		public ZwinderStateManagerSettings Settings { get; set; }

		public int Count => _current.Count + _recent.Count + _ancient.Count + 1;

		public int Last
		{
			get
			{
				if (_current.Count > 0)
					return _current.GetState(_current.Count - 1).Frame;
				if (_recent.Count > 0)
					return _recent.GetState(_current.Count - 1).Frame;
				if (_ancient.Count > 0)
					return _ancient[_ancient.Count - 1].Key;
				return 0;
			}
		}

		public void Capture(int frame, IBinaryStateable source, bool force = false)
		{
			_current.Capture(frame,
				s => source.SaveStateBinary(new BinaryWriter(s)),
				index =>
				{
					var state = _current.GetState(index);
					_recent.Capture(state.Frame,
						s => state.GetReadStream().CopyTo(s),
						index2 => 
						{
							var state2 = _recent.GetState(index2);
							var from = _ancient.Count > 0 ? _ancient[_ancient.Count - 1].Key : 0;
							if (state2.Frame - from >= _ancientInterval) 
							{
								var ms = new MemoryStream();
								state2.GetReadStream().CopyTo(ms);
								_ancient.Add(new KeyValuePair<int, byte[]>(state2.Frame, ms.ToArray()));
							}
						});
				},
				force);
		}

		public void Clear()
		{
			_current.InvalidateEnd(0);
			_recent.InvalidateEnd(0);
			_ancient.Clear();
		}

		public KeyValuePair<int, Stream> GetStateClosestToFrame(int frame)
		{
			if (frame <= 0)
				throw new ArgumentOutOfRangeException(nameof(frame));

			for (var i = _current.Count - 1; i >= 0; i--)
			{
				var s = _current.GetState(i);
				if (s.Frame < frame)
					return new KeyValuePair<int, Stream>(s.Frame, s.GetReadStream());
			}
			for (var i = _recent.Count - 1; i >= 0; i--)
			{
				var s = _recent.GetState(i);
				if (s.Frame < frame)
					return new KeyValuePair<int, Stream>(s.Frame, s.GetReadStream());
			}
			for (var i = _ancient.Count - 1; i >= 0; i--)
			{
				if (_ancient[i].Key < frame)
					return new KeyValuePair<int, Stream>(_ancient[i].Key, new MemoryStream(_ancient[i].Value, false));
			}
			return new KeyValuePair<int, Stream>(0, new MemoryStream(_originalState, false));
		}

		public bool HasState(int frame)
		{
			if (frame == 0)
			{
				return true;
			}
			for (var i = _current.Count - 1; i >= 0; i--)
			{
				if (_current.GetState(i).Frame == frame)
					return true;
			}
			for (var i = _recent.Count - 1; i >= 0; i--)
			{
				if (_recent.GetState(i).Frame == frame)
					return true;
			}
			for (var i = _ancient.Count - 1; i >= 0; i--)
			{
				if (_ancient[i].Key == frame)
					return true;
			}
			return false;
		}

		public bool Invalidate(int frame)
		{
			if (frame <= 0)
				throw new ArgumentOutOfRangeException(nameof(frame));
			for (var i = 0; i < _ancient.Count; i++)
			{
				if (_ancient[i].Key >= frame)
				{
					_ancient.RemoveRange(i, _ancient.Count - i);
					_recent.InvalidateEnd(0);
					_current.InvalidateEnd(0);
					return true;
				}
			}
			for (var i = 0; i < _recent.Count; i++)
			{
				if (_recent.GetState(i).Frame >= frame)
				{
					_recent.InvalidateEnd(i);
					_current.InvalidateEnd(0);
					return true;
				}
			}
			for (var i = 0; i < _current.Count; i++)
			{
				if (_current.GetState(i).Frame >= frame)
				{
					_current.InvalidateEnd(i);
					return true;
				}
			}
			return false;
		}

		public void UpdateStateFrequency()
		{
			throw new NotImplementedException();
		}

		public static ZwinderStateManager Create(BinaryReader br, ZwinderStateManagerSettings settings)
		{
			var current = ZwinderBuffer.Create(br);
			var recent = ZwinderBuffer.Create(br);

			var original = br.ReadBytes(br.ReadInt32());

			var ancientInterval = br.ReadInt32();

			var ret = new ZwinderStateManager(current, recent, original, ancientInterval)
			{
				Settings = settings
			};

			var ancientCount = br.ReadInt32();
			for (var i = 0; i < ancientCount; i++)
			{
				var key = br.ReadInt32();
				var length = br.ReadInt32();
				var data = br.ReadBytes(length);
				ret._ancient.Add(new KeyValuePair<int, byte[]>(key, data));
			}

			return ret;
		}

		public void SaveStateHistory(BinaryWriter bw)
		{
			_current.SaveStateBinary(bw);
			_recent.SaveStateBinary(bw);

			bw.Write(_originalState.Length);
			bw.Write(_originalState);

			bw.Write(_ancientInterval);

			bw.Write(_ancient.Count);
			foreach (var s in _ancient)
			{
				bw.Write(s.Key);
				bw.Write(s.Value.Length);
				bw.Write(s.Value);
			}
		}
	}
}
