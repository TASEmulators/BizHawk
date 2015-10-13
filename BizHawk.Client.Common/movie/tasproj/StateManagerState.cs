using System;
using System.IO;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Represents a savestate in the TasStateManager
	/// </summary>
	internal class StateManagerState : IDisposable
	{
		private static long _stateId = 0;
		private TasStateManager _manager;

		private byte[] _state;
		private long _id;

		public int Frame { get; set; }

		public void Write(BinaryWriter w)
		{
			w.Write(Frame);
			w.Write(_state.Length);
			w.Write(_state);
		}

		public static StateManagerState Read(BinaryReader r, TasStateManager m)
		{
			int frame = r.ReadInt32();
			byte[] data = r.ReadBytes(r.ReadInt32());
			return new StateManagerState(m, data, frame);
		}

		public byte[] State
		{
			get
			{
				if (_state != null)
				{
					return _state;
				}

				return _manager.ndbdatabase.FetchAll(_id.ToString());
			}
			set
			{
				if (_state != null)
				{
					_state = value;
				}
				else
				{
					throw new Exception("Attempted to set a state to null.");
				}
			}
		}

		public int Length
		{
			get { return State.Length; }
		}

		public bool IsOnDisk
		{
			get { return _state == null; }
		}

		public StateManagerState(TasStateManager manager, byte[] state, int frame)
		{
			_manager = manager;
			_state = state;
			Frame = frame;

			if (_stateId > long.MaxValue - 100)
			{
				throw new InvalidOperationException();
			}

			_id = System.Threading.Interlocked.Increment(ref _stateId);
		}

		public void MoveToDisk()
		{
			if (IsOnDisk)
			{
				return;
			}

			_manager.ndbdatabase.Store(_id.ToString(), _state, 0, _state.Length);
			_state = null;
		}

		public void MoveToRAM()
		{
			if (!IsOnDisk)
			{
				return;
			}

			string key = _id.ToString();
			_state = _manager.ndbdatabase.FetchAll(key);
			_manager.ndbdatabase.Release(key);
		}

		public void Dispose()
		{
			if (!IsOnDisk)
				return;

			_manager.ndbdatabase.Release(_id.ToString());
		}
	}
}
