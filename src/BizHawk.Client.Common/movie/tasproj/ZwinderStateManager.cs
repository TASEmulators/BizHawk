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

		private readonly ZwinderBuffer _current;
		private readonly ZwinderBuffer _recent;

		// Used to re-fill gaps when still replaying input, but in a non-current area, also needed when switching branches
		private readonly ZwinderBuffer _gapFiller;

		// These never decay, but can be invalidated, but can be invalidated, they are for reserved states
		// such as markers and branches, but also we naturally evict states from recent to hear, based
		// on _ancientInterval
		private readonly List<KeyValuePair<int, byte[]>> _reserved = new List<KeyValuePair<int, byte[]>>();

		// When recent states are evicted this interval is used to determine if we need to reserve the state
		// We always want to keep some states throughout the movie
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

			_gapFiller = new ZwinderBuffer(new RewindConfig
			{
				UseCompression = settings.GapsUseCompression,
				BufferSize = settings.GapsBufferSize,
				TargetFrameLength = settings.GapsTargetFrameLength
			});

			_ancientInterval = settings.AncientStateInterval;
		}

		public ZwinderStateManager()
			:this(new ZwinderStateManagerSettings())
		{
		}

		public void Engage(byte[] frameZeroState)
		{
			_reserved.Add(new KeyValuePair<int, byte[]>(0, (byte[])frameZeroState.Clone()));
		}

		private ZwinderStateManager(ZwinderBuffer current, ZwinderBuffer recent, ZwinderBuffer gapFiller, int ancientInterval)
		{
			_current = current;
			_recent = recent;
			_gapFiller = gapFiller;
			_ancientInterval = ancientInterval;
		}
		
		public byte[] this[int frame]
		{
			get
			{
				var kvp = GetStateClosestToFrame(frame);
				if (kvp.Key != frame)
				{
					return NonState;
				}

				var ms = new MemoryStream();
				kvp.Value.CopyTo(ms);
				return ms.ToArray();
			}
		}

		// TODO: private set, refactor LoadTasprojExtras to hold onto a settings object and pass it in to Create() method
		public ZwinderStateManagerSettings Settings { get; set; }

		public int Count => _current.Count + _recent.Count + _gapFiller.Count + _reserved.Count + 1;

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
		/// Enumerate all states, excepting GapFiller , in reverse order
		/// </summary>
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
			for (var i = _reserved.Count - 1; i >= 0; i--)
			{
				yield return new StateInfo(_reserved[i]);
			}
		}

		// Only considers Current and Recent
		private IEnumerable<StateInfo> RingStates()
		{
			for (var i = _current.Count - 1; i >= 0; i--)
			{
				yield return new StateInfo(_current.GetState(i));
			}
			for (var i = _recent.Count - 1; i >= 0; i--)
			{
				yield return new StateInfo(_recent.GetState(i));
			}
		}

		private IEnumerable<StateInfo> GapStates()
		{
			for (var i = _gapFiller.Count - 1; i >= 0; i--)
			{
				yield return new StateInfo(_gapFiller.GetState(i));
			}
		}

		/// <summary>
		/// Enumerate all states in reverse order
		/// </summary>
		private IEnumerable<StateInfo> AllStates()
		{
			var l1 = NormalStates().GetEnumerator();
			var l2 = GapStates().GetEnumerator();
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

		private int LastRing => RingStates().FirstOrDefault()?.Frame ?? 0;

		public void CaptureReserved(int frame, IStatable source)
		{
			var ms = new MemoryStream();
			source.SaveStateBinary(new BinaryWriter(ms));
			_reserved.Add(new KeyValuePair<int, byte[]>(frame, ms.ToArray()));
		}

		public void Capture(int frame, IStatable source, bool force = false)
		{
			// We do not want to consider reserved states for a notion of Last
			// reserved states can include future states in the case of branch states
			if (frame <= LastRing)
			{
				CaptureGap(frame, source);
				return;
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
							var from = _reserved.Count > 0 ? _reserved[_reserved.Count - 1].Key : 0;
							if (state2.Frame - from >= _ancientInterval) 
							{
								var ms = new MemoryStream();
								state2.GetReadStream().CopyTo(ms);
								_reserved.Add(new KeyValuePair<int, byte[]>(state2.Frame, ms.ToArray()));
							}
						});
				},
				force);
		}

		public void CaptureGap(int frame, IStatable source)
		{
			_gapFiller.Capture(frame, s => source.SaveStateBinary(new BinaryWriter(s)));
		}

		public void Clear()
		{
			_current.InvalidateEnd(0);
			_recent.InvalidateEnd(0);
			_gapFiller.InvalidateEnd(0);
			_reserved.Clear();
		}

		public KeyValuePair<int, Stream> GetStateClosestToFrame(int frame)
		{
			if (frame < 0)
				throw new ArgumentOutOfRangeException(nameof(frame));

			var si = AllStates().First(s => s.Frame <= frame);
			return new KeyValuePair<int, Stream>(si.Frame, si.Read());
		}

		public bool HasState(int frame)
		{
			return AllStates().Any(s => s.Frame == frame);
		}

		private bool InvalidateGaps(int frame)
		{
			for (var i = 0; i < _gapFiller.Count; i++)
			{
				if (_gapFiller.GetState(i).Frame > frame)
				{
					_gapFiller.InvalidateEnd(i);
					return true;
				}
			}
			return false;
		}

		private bool InvalidateNormal(int frame)
		{
			for (var i = 0; i < _reserved.Count; i++)
			{
				if (_reserved[i].Key > frame)
				{
					_reserved.RemoveRange(i, _reserved.Count - i);
					_recent.InvalidateEnd(0);
					_current.InvalidateEnd(0);
					return true;
				}
			}
			for (var i = 0; i < _recent.Count; i++)
			{
				if (_recent.GetState(i).Frame > frame)
				{
					_recent.InvalidateEnd(i);
					_current.InvalidateEnd(0);
					return true;
				}
			}
			for (var i = 0; i < _current.Count; i++)
			{
				if (_current.GetState(i).Frame > frame)
				{
					_current.InvalidateEnd(i);
					return true;
				}
			}
			return false;
		}

		public void UpdateSettings(ZwinderStateManagerSettings settings) => Settings = settings;

		public bool InvalidateAfter(int frame)
		{
			if (frame < 0)
				throw new ArgumentOutOfRangeException(nameof(frame));
			var b1 = InvalidateNormal(frame);
			var b2 = InvalidateGaps(frame);
			return b1 || b2;
		}

		public static ZwinderStateManager Create(BinaryReader br, ZwinderStateManagerSettings settings)
		{
			var current = ZwinderBuffer.Create(br);
			var recent = ZwinderBuffer.Create(br);
			var gaps = ZwinderBuffer.Create(br);

			var ancientInterval = br.ReadInt32();

			var ret = new ZwinderStateManager(current, recent, gaps, ancientInterval)
			{
				Settings = settings
			};

			var ancientCount = br.ReadInt32();
			for (var i = 0; i < ancientCount; i++)
			{
				var key = br.ReadInt32();
				var length = br.ReadInt32();
				var data = br.ReadBytes(length);
				ret._reserved.Add(new KeyValuePair<int, byte[]>(key, data));
			}

			return ret;
		}

		public void SaveStateHistory(BinaryWriter bw)
		{
			_current.SaveStateBinary(bw);
			_recent.SaveStateBinary(bw);
			_gapFiller.SaveStateBinary(bw);

			bw.Write(_ancientInterval);

			bw.Write(_reserved.Count);
			foreach (var s in _reserved)
			{
				bw.Write(s.Key);
				bw.Write(s.Value.Length);
				bw.Write(s.Value);
			}
		}
	}
}
