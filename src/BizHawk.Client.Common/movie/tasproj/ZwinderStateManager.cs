using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	// todo: maybe interface this?
	public class ZwinderStateManagerSettingsWIP
	{
		/// <summary>
		/// Buffer settings when navigating near now
		/// </summary>
		public IRewindSettings Current { get; set; } = new RewindConfig
		{
			UseCompression = false,
			BufferSize = 64,
			TargetFrameLength = 1000,
		};
		/// <summary>
		/// Buffer settings when navigating directly before the Current buffer
		/// </summary>
		/// <value></value>
		public IRewindSettings Recent { get; set; } = new RewindConfig
		{
			UseCompression = false,
			BufferSize = 64,
			TargetFrameLength = 10000,
		};
		/// <summary>
		/// How often to maintain states when outside of Current and Recent intervals
		/// </summary>
		/// <value></value>
		public int AncientStateInterval { get; set; } = 5000;

		/// <summary>
		/// TODO: NUKE THIS, it doesn't belong here, maybe?
		/// </summary>
		/// <value></value>
		public bool SaveStateHistory { get; set; } = true;
	}
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
			Settings = new ZwinderStateManagerSettingsWIP();
			_current = new ZwinderBuffer(Settings.Current);
			_recent = new ZwinderBuffer(Settings.Recent);
			_ancientInterval = Settings.AncientStateInterval;
			_originalState = new byte[0];
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
		
		public byte[] this[int frame] => throw new NotImplementedException();

		public ZwinderStateManagerSettingsWIP Settings { get; set; }

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

		public bool Any() => true;

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

		public static ZwinderStateManager Create(BinaryReader br)
		{
			var current = ZwinderBuffer.Create(br);
			var recent = ZwinderBuffer.Create(br);

			var original = br.ReadBytes(br.ReadInt32());

			var ancientInterval = br.ReadInt32();

			var ret = new ZwinderStateManager(current, recent, null, ancientInterval);

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
