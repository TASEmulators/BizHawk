using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class ZwinderStateManager : IStateManager, IDisposable
	{
		private static readonly byte[] NonState = new byte[0];

		private readonly Func<int, bool> _reserveCallback;
		internal readonly SortedSet<int> StateCache = new SortedSet<int>();
		private ZwinderBuffer _current;
		private ZwinderBuffer _recent;

		// Used to re-fill gaps when still replaying input, but in a non-current area, also needed when switching branches
		private ZwinderBuffer _gapFiller;

		// These never decay, but can be invalidated, they are for reserved states
		// such as markers and branches, but also we naturally evict states from recent to reserved, based
		// on _ancientInterval
		private Dictionary<int, byte[]> _reserved = new Dictionary<int, byte[]>();

		// When recent states are evicted this interval is used to determine if we need to reserve the state
		// We always want to keep some states throughout the movie
		private readonly int _ancientInterval;

		internal ZwinderStateManager(ZwinderStateManagerSettings settings, Func<int, bool> reserveCallback)
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
			_reserveCallback = reserveCallback;
		}

		/// <param name="reserveCallback">Called when deciding to evict a state for the given frame, if true is returned, the state will be reserved</param>
		public ZwinderStateManager(Func<int, bool> reserveCallback)
			: this(new ZwinderStateManagerSettings(), reserveCallback)
		{
		}

		public void Engage(byte[] frameZeroState)
		{
			if (!_reserved.ContainsKey(0))
			{
				_reserved.Add(0, frameZeroState);
				StateCache.Add(0);
			}
		}

		private ZwinderStateManager(ZwinderBuffer current, ZwinderBuffer recent, ZwinderBuffer gapFiller, int ancientInterval, Func<int, bool> reserveCallback)
		{
			_current = current;
			_recent = recent;
			_gapFiller = gapFiller;
			_ancientInterval = ancientInterval;
			_reserveCallback = reserveCallback;
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

		public int Count => _current.Count + _recent.Count + _gapFiller.Count + _reserved.Count;

		internal class StateInfo
		{
			public int Frame { get; }
			public Func<Stream> Read { get; }
			public StateInfo(ZwinderBuffer.StateInformation si)
			{
				Frame = si.Frame;
				Read = si.GetReadStream;
			}

			public StateInfo(int frame, byte[] data)
			{
				Frame = frame;
				Read = () => new MemoryStream(data, false);
			}
		}

		// Enumerate all current and recent states in reverse order
		private IEnumerable<StateInfo> CurrentAndRecentStates()
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

		// Enumerate all gap states in reverse order
		private IEnumerable<StateInfo> GapStates()
		{
			for (var i = _gapFiller.Count - 1; i >= 0; i--)
			{
				yield return new StateInfo(_gapFiller.GetState(i));
			}
		}

		// Enumerate all reserved states in reverse order
		private IEnumerable<StateInfo> ReservedStates()
		{
			foreach (var key in _reserved.Keys.OrderByDescending(k => k))
			{
				yield return new StateInfo(key, _reserved[key]);
			}
		}

		/// <summary>
		/// Enumerate all states in reverse order
		/// </summary>
		internal IEnumerable<StateInfo> AllStates()
		{
			return CurrentAndRecentStates()
				.Concat(GapStates())
				.Concat(ReservedStates())
				.OrderByDescending(s => s.Frame);
		}

		public int Last => StateCache.Max();

		private int LastRing => CurrentAndRecentStates().FirstOrDefault()?.Frame ?? 0;

		internal void CaptureReserved(int frame, IStatable source)
		{
			if (_reserved.ContainsKey(frame))
			{
				return;
			}

			var ms = new MemoryStream();
			source.SaveStateBinary(new BinaryWriter(ms));
			_reserved.Add(frame, ms.ToArray());
			StateCache.Add(frame);
		}

		private void AddToReserved(ZwinderBuffer.StateInformation state)
		{
			if (_reserved.ContainsKey(state.Frame))
			{
				return;
			}

			var bb = new byte[state.Size];
			var ms = new MemoryStream(bb);
			state.GetReadStream().CopyTo(ms);
			_reserved.Add(state.Frame, bb);
			StateCache.Add(state.Frame);
		}

		public void EvictReserved(int frame)
		{
			if (frame == 0)
			{
				throw new InvalidOperationException("Frame 0 can not be evicted.");
			}

			_reserved.Remove(frame);
			StateCache.Remove(frame);
		}

		public void Capture(int frame, IStatable source, bool force = false)
		{
			// We already have this state, no need to capture
			if (StateCache.Contains(frame))
			{
				return;
			}

			if (_reserveCallback(frame))
			{
				CaptureReserved(frame, source);
				return;
			}

			// We do not want to consider reserved states for a notion of Last
			// reserved states can include future states in the case of branch states
			if (frame <= LastRing)
			{
				CaptureGap(frame, source);
				return;
			}

			_current.Capture(frame,
				s =>
				{
					source.SaveStateBinary(new BinaryWriter(s));
					StateCache.Add(frame);
				},
				index =>
				{
					var state = _current.GetState(index);
					StateCache.Remove(state.Frame);

					// If this is a reserved state, go ahead and reserve instead of potentially trying to force it into recent, for further eviction logic later
					if (_reserveCallback(state.Frame))
					{
						AddToReserved(state);
						return;
					}

					_recent.Capture(state.Frame,
						s =>
						{
							state.GetReadStream().CopyTo(s);
							StateCache.Add(state.Frame);
						},
						index2 => 
						{
							var state2 = _recent.GetState(index2);
							StateCache.Remove(state2.Frame);

							var from = _reserved.Count > 0 ? _reserved.Max(kvp => kvp.Key) : 0;

							var isReserved = _reserveCallback(state2.Frame);

							// Add to reserved if reserved, or if it matches an "ancient" state consideration
							if (isReserved || state2.Frame - from >= _ancientInterval)
							{
								AddToReserved(state2);
							}
						});
				},
				force);
		}

		private void CaptureGap(int frame, IStatable source)
		{
			_gapFiller.Capture(
				frame, s =>
				{
					StateCache.Add(frame);
					source.SaveStateBinary(new BinaryWriter(s));
				},
				index => StateCache.Remove(index));
		}

		public void Clear()
		{
			_current.InvalidateEnd(0);
			_recent.InvalidateEnd(0);
			_gapFiller.InvalidateEnd(0);
			StateCache.Clear();
			StateCache.Add(0);
			_reserved = _reserved
				.Where(kvp => kvp.Key == 0)
				.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
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
			return StateCache.Contains(frame);
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

		private bool InvalidateReserved(int frame)
		{
			var origCount = _reserved.Count;
			_reserved = _reserved
				.Where(kvp => kvp.Key <= frame)
				.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

			return _reserved.Count < origCount;
		}

		public void UpdateSettings(ZwinderStateManagerSettings settings) => Settings = settings;

		public bool InvalidateAfter(int frame)
		{
			if (frame < 0)
				throw new ArgumentOutOfRangeException(nameof(frame));
			var b1 = InvalidateNormal(frame);
			var b2 = InvalidateGaps(frame);
			var b3 = InvalidateReserved(frame);
			StateCache.RemoveWhere(s => s > frame);
			return b1 || b2 || b3;
		}

		public static ZwinderStateManager Create(BinaryReader br, ZwinderStateManagerSettings settings, Func<int, bool> reserveCallback)
		{
			var current = ZwinderBuffer.Create(br);
			var recent = ZwinderBuffer.Create(br);
			var gaps = ZwinderBuffer.Create(br);

			var ancientInterval = br.ReadInt32();

			var ret = new ZwinderStateManager(current, recent, gaps, ancientInterval, reserveCallback)
			{
				Settings = settings
			};

			var ancientCount = br.ReadInt32();
			for (var i = 0; i < ancientCount; i++)
			{
				var key = br.ReadInt32();
				var length = br.ReadInt32();
				var data = br.ReadBytes(length);
				ret._reserved.Add(key, data);
			}

			var allStates = ret.AllStates().ToList();
			foreach (var state in allStates)
			{
				ret.StateCache.Add(state.Frame);
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

		public void Dispose()
		{
			_current?.Dispose();
			_current = null;

			_recent?.Dispose();
			_recent = null;

			_gapFiller?.Dispose();
			_gapFiller = null;
		}
	}
}
