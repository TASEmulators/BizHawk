using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// A simple ring buffer rewinder
	/// </summary>
	public class Zwinder : IRewinder
	{
		/*
		Main goals:
		1. No copies, ever.  States are deposited directly to, and read directly from, one giant ring buffer.
			As a consequence, there is no multi-threading because there is nothing to thread.
		2. Support for arbitrary and changeable state sizes.  Frequency is calculated dynamically.
		3. No delta compression.  Keep it simple.  If there are cores that benefit heavily from delta compression, we should
			maintain a separate rewinder alongside this one that is customized for those cores.
		*/

		private readonly ZwinderBuffer _buffer;
		private readonly IStatable _stateSource;

		public Zwinder(IStatable stateSource, IRewindSettings settings)
		{
			_buffer = new ZwinderBuffer(settings);
			_stateSource = stateSource;
			Active = true;
		}

		/// <summary>
		/// How many states are actually in the state ringbuffer
		/// </summary>
		public int Count => _buffer.Count;

		public float FullnessRatio => Used / (float)Size;

		/// <summary>
		/// total number of bytes used
		/// </summary>
		/// <value></value>
		public long Used => _buffer.Used;

		/// <summary>
		/// Total size of the _buffer
		/// </summary>
		/// <value></value>
		public long Size => _buffer.Size;

		/// <summary>
		/// TODO: This is not a frequency, it's the reciprocal
		/// </summary>
		public int RewindFrequency => _buffer.RewindFrequency;

		public bool Active { get; private set; }

		public void Capture(int frame)
		{
			if (!Active)
				return;
			_buffer.Capture(frame, s => _stateSource.SaveStateBinary(new BinaryWriter(s)));
		}

		public bool Rewind(int frameToAvoid)
		{
			if (!Active || Count == 0)
				return false;
			var index = Count - 1;
			var state = _buffer.GetState(index);
			if (state.Frame == frameToAvoid)
			{
				if (Count > 1)
				{
					state = _buffer.GetState(index - 1);
				}
				using var br = new BinaryReader(state.GetReadStream());
				_stateSource.LoadStateBinary(br);
				_buffer.InvalidateLast();
			}
			else
			{
				// The emulator will frame advance without giving us a chance to
				// re-capture this frame, so we shouldn't invalidate this state just yet.
				using var br = new BinaryReader(state.GetReadStream());
				_stateSource.LoadStateBinary(br);
			}
			return true;
		}

		public void Suspend()
		{
			Active = false;
		}

		public void Resume()
		{
			Active = true;
		}

		public void Dispose()
		{
			_buffer.Dispose();
		}

		public void Clear()
		{
			_buffer.InvalidateAfter(-1);
		}
	}
}
