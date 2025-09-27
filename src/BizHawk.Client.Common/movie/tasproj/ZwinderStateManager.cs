using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class ZwinderStateManager : IStateManager, IDisposable
	{
		private static readonly byte[] NonState = Array.Empty<byte>();

		private readonly Func<int, bool> _reserveCallback;
		internal readonly SortedList<int> StateCache = new SortedList<int>();
		private ZwinderBuffer _current;
		private ZwinderBuffer _recent;

		// Used to re-fill gaps when still replaying input, but in a non-current area, also needed when switching branches
		private ZwinderBuffer _gapFiller;

		// These never decay, but can be invalidated, they are for reserved states
		// such as markers and branches, but also we naturally evict states from recent to reserved, based
		// on _ancientInterval
		private IDictionary<int, byte[]> _reserved;

		// When recent states are evicted this interval is used to determine if we need to reserve the state
		// We always want to keep some states throughout the movie
		private int _ancientInterval;

		internal ZwinderStateManager(ZwinderStateManagerSettings settings, Func<int, bool> reserveCallback)
		{
			UpdateSettings(settings, false);

			_reserveCallback = reserveCallback;
		}

		public void Engage(byte[] frameZeroState)
		{
			if (!_reserved.ContainsKey(0))
			{
				_reserved.Add(0, frameZeroState);
				AddStateCache(0);
			}
		}

		private ZwinderStateManager(ZwinderBuffer current, ZwinderBuffer recent, ZwinderBuffer gapFiller, Func<int, bool> reserveCallback, ZwinderStateManagerSettings settings)
		{
			_current = current;
			_recent = recent;
			_gapFiller = gapFiller;
			_reserveCallback = reserveCallback;
			Settings = settings;
			_ancientInterval = settings.AncientStateInterval;
			// init the reserved dictionary
			RebuildReserved();
		}

		public ZwinderStateManagerSettings Settings { get; private set; }
		IStateManagerSettings IStateManager.Settings => Settings;

		public IStateManager UpdateSettings(IStateManagerSettings settings, bool keepOldStates = false)
		{
			if (settings is not ZwinderStateManagerSettings zSettings)
			{
				IStateManager newManager = settings.CreateManager(_reserveCallback);
				newManager.Engage(GetStateClosestToFrame(0).Value.ReadAllBytes());
				if (keepOldStates)
				{
					foreach (int frame in StateCache)
					{
						Stream ss = GetStateClosestToFrame(frame).Value;
						newManager.Capture(frame, new StatableStream(ss, (int)ss.Length));
					}
				}
				Dispose();
				return newManager;
			}

			bool makeNewReserved = Settings?.AncientStoreType != zSettings.AncientStoreType;
			Settings = zSettings;

			_current = UpdateBuffer(_current, zSettings.Current(), keepOldStates);
			_recent = UpdateBuffer(_recent, zSettings.Recent(), keepOldStates);
			_gapFiller = UpdateBuffer(_gapFiller, zSettings.GapFiller(), keepOldStates);

			if (keepOldStates)
			{
				// For ancients, let's throw out states if doing so still satisfies the ancient state interval.
				if (zSettings.AncientStateInterval > _ancientInterval)
				{
					List<int> reservedFrames = _reserved.Keys.ToList();
					reservedFrames.Sort();
					for (int i = 1; i < reservedFrames.Count - 1; i++)
					{
						if (_reserveCallback(reservedFrames[i]))
							continue;

						if (reservedFrames[i + 1] - reservedFrames[i - 1] <= zSettings.AncientStateInterval)
						{
							EvictReserved(reservedFrames[i]);
							reservedFrames.RemoveAt(i);
							i--;
						}
					}
				}
			}
			else
			{
				if (_reserved != null)
				{
					List<int> framesToRemove = new List<int>();
					foreach (int f in _reserved.Keys)
					{
						if (f != 0 && !_reserveCallback(f))
							framesToRemove.Add(f);
					}
					foreach (int f in framesToRemove)
						EvictReserved(f);
				}
			}

			if (makeNewReserved)
				RebuildReserved();

			_ancientInterval = zSettings.AncientStateInterval;
			RebuildStateCache();

			return this;
		}

		private void RebuildReserved()
		{
			IDictionary<int, byte[]> newReserved;
			switch (Settings.AncientStoreType)
			{
				case IRewindSettings.BackingStoreType.Memory:
					newReserved = new Dictionary<int, byte[]>();
					break;
				case IRewindSettings.BackingStoreType.TempFile:
					newReserved = new TempFileStateDictionary();
					break;
				default:
					throw new InvalidOperationException("Unsupported store type for reserved states.");
			}
			if (_reserved != null)
			{
				foreach (var (f, data) in _reserved) newReserved.Add(f, data);
				(_reserved as TempFileStateDictionary)?.Dispose();
			}
			_reserved = newReserved;
		}

		private ZwinderBuffer UpdateBuffer(ZwinderBuffer buffer, RewindConfig newConfig, bool keepOldStates)
		{
			if (buffer == null) // just make a new one, plain and simple
				buffer = new ZwinderBuffer(newConfig);
			else if (!buffer.MatchesSettings(newConfig)) // no need to do anything if these settings are already in use
			{
				if (keepOldStates)
				{
					// force capture all the old states, let the buffer handle decay if they don't all fit
					ZwinderBuffer old = buffer;
					buffer = new ZwinderBuffer(newConfig);
					for (int i = 0; i < old.Count; i++)
					{
						ZwinderBuffer.StateInformation si = old.GetState(i);
						buffer.Capture(si.Frame, s =>
						{
							using var rs = si.GetReadStream();
							rs.CopyTo(s);
						}, index => HandleStateDecay(buffer, index), true);
					}
					old.Dispose();
				}
				else
				{
					buffer.Dispose();
					buffer = new ZwinderBuffer(newConfig);
				}
			}
			return buffer;
		}

		private void RebuildStateCache()
		{
			StateCache.Clear();
			foreach (StateInfo state in AllStates())
			{
				AddStateCache(state.Frame);
			}
		}

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
			foreach (var key in _reserved.Keys.OrderDescending())
			{
				yield return new StateInfo(key, _reserved[key]);
			}
		}

		/// <summary>
		/// Enumerate all states in the following order: current -> recent -> gap -> reserved states
		/// </summary>
		internal IEnumerable<StateInfo> AllStates()
		{
			return CurrentAndRecentStates()
				.Concat(GapStates())
				.Concat(ReservedStates());
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
			AddStateCache(frame);
		}

		private void AddToReserved(ZwinderBuffer.StateInformation state)
		{
			if (_reserved.ContainsKey(state.Frame))
			{
				return;
			}

			using var s = state.GetReadStream();
			_reserved.Add(state.Frame, s.ReadAllBytes());
			AddStateCache(state.Frame);
		}

		private void AddStateCache(int frame)
		{
			if (!StateCache.Contains(frame))
			{
				StateCache.Add(frame);
			}
		}

		private void EvictReserved(int frame)
		{
			if (frame == 0)
			{
				throw new InvalidOperationException("Frame 0 can not be evicted.");
			}

			if (_reserved.ContainsKey(frame))
			{
				_reserved.Remove(frame);
				StateCache.Remove(frame);
			}
		}

		public void Unreserve(int frame)
		{
			// Before removing the state, check if it should still be reserved.
			// For now, this just means checking if we need to keep this state to satisfy the ancient interval.
			if (ShouldKeepForAncient(frame))
				return;

			EvictReserved(frame);
		}

		/// <summary>
		/// This method is only to be used as the capture callback for a ZwinderBuffer, since it assumes the state is about to be removed from the buffer.
		/// </summary>
		private void HandleStateDecay(ZwinderBuffer buffer, int stateIndex)
		{
			var state = buffer.GetState(stateIndex);

			// Add to reserved buffer if externally reserved, or if it matches an "ancient" state consideration
			if (_reserveCallback(state.Frame) || ShouldKeepForAncient(state.Frame))
				AddToReserved(state);
			else
				// We remove from the state cache because this state is about to be removed, by the buffer.
				StateCache.Remove(state.Frame);
		}

		/// <summary>
		/// Will removing the state on this leave us with a gap larger than the ancient interval?
		/// </summary>
		private bool ShouldKeepForAncient(int frame)
		{
			int index = StateCache.BinarySearch(frame + 1);
			if (index < 0)
				index = ~index;
			if (index <= 1)
			{
				// index == 0 should not be possible. (It's the index of the state after the given frame.)
				// index == 1 would mean we are considering removing the state on frame 0.
				// We must always have a state on frame 0.
				return true;
			}
			if (index == StateCache.Count)
			{
				// There is no future state, so there is no gap between states for us to measure.
				// We're probably unreserving for a marker removal. Allow it to be removed, so we don't pollute _reserved.
				return false;
			}

			int nextState = StateCache[index];
			int previousState = StateCache[index - 2]; // assume StateCache[index - 1] == frame
			return nextState - previousState > _ancientInterval;
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

			// avoid capturing in this case
			if (source.AvoidRewind)
			{
				return;
			}

			// We use the gap buffer for forced capture to avoid crowding the "current" buffer and thus reducing it's actual span of covered frames.
			if (NeedsGap(frame) || force)
			{
				CaptureGap(frame, source);
				return;
			}

			_current.Capture(frame,
				s =>
				{
					source.SaveStateBinary(new BinaryWriter(s));
					AddStateCache(frame);
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
							using var rs = state.GetReadStream();
							rs.CopyTo(s);
							AddStateCache(state.Frame);
						},
						index2 => HandleStateDecay(_recent, index2));
				});
		}

		private bool NeedsGap(int frame)
		{
			// We don't want to "fill gaps" if we are past the latest state in the current/recent buffers.
			if (frame >= LastRing)
			{
				return false;
			}

			// When starting to fill gaps we won't actually know the true frequency, so fall back to current
			// Current may very well not be the same as gap, but it's a reasonable behavior to have a current sized gap before seeing filler sized gaps
			var frequency = _gapFiller.Count == 0 ? _current.RewindFrequency : _gapFiller.RewindFrequency;
			return !StateCache.Any(sc => sc < frame && sc > frame - frequency);
		}

		private void CaptureGap(int frame, IStatable source)
		{
			// We need to do this here for the following scenario
			// We are currently far enough in the game that there is a large "ancient interval" section
			// The user navigates to a frame after ancient interval 2, replay happens and we start filling gaps
			// Then the user, still without having made an edit, navigates to a frame before ancient interval 2, but after ancient interval 1
			// Without this logic, we end up with out of order states
			// We cannot use InvalidateGaps because that does not address the state cache or check for reserved states.
			for (int i = _gapFiller.Count - 1; i >= 0; i--)
			{
				var lastGap = _gapFiller.GetState(i);
				if (lastGap.Frame < frame)
					break;

				if (_reserveCallback(lastGap.Frame))
					AddToReserved(lastGap);
				else
					StateCache.Remove(lastGap.Frame);

				_gapFiller.InvalidateLast();
			}

			_gapFiller.Capture(
				frame, s =>
				{
					AddStateCache(frame);
					source.SaveStateBinary(new BinaryWriter(s));
				},
				index => HandleStateDecay(_gapFiller, index));
		}

		public void Clear()
		{
			_current.InvalidateAfter(-1);
			_recent.InvalidateAfter(-1);
			_gapFiller.InvalidateAfter(-1);
			StateCache.Clear();
			AddStateCache(0);
			_reserved = _reserved.Where(static kvp => kvp.Key is 0).ToDictionary(); //TODO clone needed?
		}

		public KeyValuePair<int, Stream> GetStateClosestToFrame(int frame)
		{
			if (frame < 0)
				throw new ArgumentOutOfRangeException(nameof(frame));

			StateInfo closestState = null;
			foreach (var state in AllStates())
			{
				if (state.Frame <= frame && (closestState is null || state.Frame > closestState.Frame))
				{
					closestState = state;
				}
			}
			return new KeyValuePair<int, Stream>(closestState!.Frame, closestState.Read());
		}

		public bool HasState(int frame)
		{
			return StateCache.Contains(frame);
		}

		private bool InvalidateGaps(int frame)
		{
			return _gapFiller.InvalidateAfter(frame);
		}

		private bool InvalidateNormal(int frame)
		{
			if (_recent.InvalidateAfter(frame))
			{
				_current.InvalidateAfter(-1);
				return true;
			}

			return _current.InvalidateAfter(frame);
		}

		private bool InvalidateReserved(int frame)
		{
			var origCount = _reserved.Count;
			_reserved = _reserved.Where(kvp => kvp.Key <= frame).ToDictionary(); //TODO clone needed?
			return _reserved.Count < origCount;
		}

		public bool InvalidateAfter(int frame)
		{
			if (frame < 0)
				throw new ArgumentOutOfRangeException(nameof(frame));
			var b1 = InvalidateNormal(frame);
			var b2 = InvalidateGaps(frame);
			var b3 = InvalidateReserved(frame);
			StateCache.RemoveAfter(frame);
			return b1 || b2 || b3;
		}

		public void LoadStateHistory(BinaryReader br)
		{
			int version = br.ReadByte();
			if (version == 0) throw new Exception("Unsupported GreenZone version.");

			_current.Load(br);
			_recent.Load(br);
			_gapFiller.Load(br);

			var ancientCount = br.ReadInt32();
			for (var i = 0; i < ancientCount; i++)
			{
				var key = br.ReadInt32();
				var length = br.ReadInt32();
				var data = br.ReadBytes(length);
				_reserved.Add(key, data);
			}

			RebuildStateCache();
		}

		public void SaveStateHistory(BinaryWriter bw)
		{
			// version
			bw.Write((byte)1);

			_current.SaveStateBinary(bw);
			_recent.SaveStateBinary(bw);
			_gapFiller.SaveStateBinary(bw);

			bw.Write(_reserved.Count);
			foreach (var (f, data) in _reserved)
			{
				bw.Write(f);
				bw.Write(data.Length);
				bw.Write(data);
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
