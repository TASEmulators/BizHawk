using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class ZwinderStateManager : IStateManager
	{
		private static readonly byte[] NonState = new byte[0];

		private byte[] _originalState;
		private readonly ZwinderBuffer _current;
		private readonly ZwinderBuffer _recent;
		private readonly ZwinderBuffer _highPriority;
		private readonly List<KeyValuePair<int, byte[]>> _ancient = new List<KeyValuePair<int, byte[]>>();
		private readonly int _ancientInterval;

		public ZwinderStateManager(ZwinderStateManagerSettings settings)
		{
			Settings = settings;

			_current = new ZwinderBuffer(new RewindConfig
			{
				UseCompression = settings.CurrentUseCompression,
				BufferSize = settings.CurrentBufferSize,
				TargetFrameLength = settings.CurrentTargetFrameLength
			});
			_recent = new ZwinderBuffer(new RewindConfig
			{
				UseCompression = settings.RecentUseCompression,
				BufferSize = settings.RecentBufferSize,
				TargetFrameLength = settings.RecentTargetFrameLength
			});

			_highPriority = new ZwinderBuffer(new RewindConfig
			{
				UseCompression = settings.PriorityUseCompression,
				BufferSize = settings.PriorityBufferSize,
				TargetFrameLength = settings.PriorityTargetFrameLength
			});

			_ancientInterval = settings.AncientStateInterval;
			_originalState = NonState;
		}

		public ZwinderStateManager()
			:this(new ZwinderStateManagerSettings())
		{
		}

		public void Engage(byte[] frameZeroState)
		{
			_originalState = (byte[])frameZeroState.Clone();
		}

		private ZwinderStateManager(ZwinderBuffer current, ZwinderBuffer recent, ZwinderBuffer highPriority, byte[] frameZeroState, int ancientInterval)
		{
			_originalState = (byte[])frameZeroState.Clone();
			_current = current;
			_recent = recent;
			_highPriority = highPriority;
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

		public int Count => _current.Count + _recent.Count + _highPriority.Count + _ancient.Count + 1;

		private class StateInfo
		{
			public int Frame { get; }
			public Func<Stream> Read { get; }
			public StateInfo(ZwinderBuffer.StateInformation si)
			{
				Frame = si.Frame;
				Read = si.GetReadStream;
			}
			public StateInfo(KeyValuePair<int, byte[]> kvp)
				:this(kvp.Key, kvp.Value)
			{
			}
			public StateInfo(int frame, byte[] data)
			{
				Frame = frame;
				Read = () => new MemoryStream(data, false);
			}
		}

		/// <summary>
		/// Enumerate all states, excepting high priority, in reverse order
		/// </summary>
		/// <returns></returns>
		private IEnumerable<StateInfo> NormalStates()
		{
			for (var i = _current.Count - 1; i >= 0; i--)
			{
				yield return new StateInfo(_current.GetState(i));
			}
			for (var i = _recent.Count - 1; i >= 0; i--)
			{
				yield return new StateInfo(_recent.GetState(i));
			}
			for (var i = _ancient.Count - 1; i >= 0; i--)
			{
				yield return new StateInfo(_ancient[i]);
			}
			yield return new StateInfo(0, _originalState);
		}

		/// <summary>
		/// Enumerate high priority states in reverse order
		/// </summary>
		/// <returns></returns>
		private IEnumerable<StateInfo> HighPriorityStates()
		{
			for (var i = _highPriority.Count - 1; i >= 0; i--)
			{
				yield return new StateInfo(_highPriority.GetState(i));
			}
		}

		/// <summary>
		/// Enumerate all states in reverse order
		/// </summary>
		private IEnumerable<StateInfo> AllStates()
		{
			var l1 = NormalStates().GetEnumerator();
			var l2 = HighPriorityStates().GetEnumerator();
			var l1More = l1.MoveNext();
			var l2More = l2.MoveNext();
			while (l1More || l2More)
			{
				if (l1More)
				{
					if (l2More)
					{
						if (l1.Current.Frame > l2.Current.Frame)
						{
							yield return l1.Current;
							l1More = l1.MoveNext();
						}
						else
						{
							yield return l2.Current;
							l2More = l2.MoveNext();
						}
					}
					else
					{
						yield return l1.Current;
						l1More = l1.MoveNext();
					}
				}
				else
				{
					yield return l2.Current;
					l2More = l2.MoveNext();
				}
			}
		}

		public int Last => AllStates().First().Frame;

		public void Capture(int frame, IStatable source, bool force = false)
		{
			if (frame <= Last)
			{
				CaptureHighPriority(frame, source);
			}

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

		public void CaptureHighPriority(int frame, IStatable source)
		{
			_highPriority.Capture(frame, s => source.SaveStateBinary(new BinaryWriter(s)));
		}

		public void Clear()
		{
			_current.InvalidateEnd(0);
			_recent.InvalidateEnd(0);
			_highPriority.InvalidateEnd(0);
			_ancient.Clear();
		}

		public KeyValuePair<int, Stream> GetStateClosestToFrame(int frame)
		{
			if (frame <= 0)
				throw new ArgumentOutOfRangeException(nameof(frame));

			var si = AllStates().First(s => s.Frame < frame);
			return new KeyValuePair<int, Stream>(si.Frame, si.Read());
		}

		public bool HasState(int frame)
		{
			return AllStates().Any(s => s.Frame == frame);
		}

		private bool InvalidateHighPriority(int frame)
		{
			for (var i = 0; i < _highPriority.Count; i++)
			{
				if (_highPriority.GetState(i).Frame >= frame)
				{
					_highPriority.InvalidateEnd(i);
					return true;
				}
			}
			return false;
		}

		private bool InvalidateNormal(int frame)
		{
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

		public void UpdateSettings(ZwinderStateManagerSettings settings) => Settings = settings;

		public bool Invalidate(int frame)
		{
			if (frame <= 0)
				throw new ArgumentOutOfRangeException(nameof(frame));
			var b1 = InvalidateNormal(frame);
			var b2 = InvalidateHighPriority(frame);
			return b1 || b2;
		}

		public static ZwinderStateManager Create(BinaryReader br, ZwinderStateManagerSettings settings)
		{
			var current = ZwinderBuffer.Create(br);
			var recent = ZwinderBuffer.Create(br);
			var highPriority = ZwinderBuffer.Create(br);

			var original = br.ReadBytes(br.ReadInt32());

			var ancientInterval = br.ReadInt32();

			var ret = new ZwinderStateManager(current, recent, highPriority, original, ancientInterval)
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
			_highPriority.SaveStateBinary(bw);

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
