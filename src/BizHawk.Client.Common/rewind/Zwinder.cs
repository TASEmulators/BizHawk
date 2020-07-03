using System;
using System.IO;
using System.IO.Compression;
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
		private readonly IBinaryStateable _stateSource;

		public Zwinder(IBinaryStateable stateSource, IRewindSettings settings)
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

		public bool Rewind()
		{
			if (!Active || Count == 0)
				return false;
			var index = Count - 1;
			var state = _buffer.GetState(index);
			_stateSource.LoadStateBinary(new BinaryReader(state.GetReadStream()));
			_buffer.InvalidateEnd(index);
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
			// this possess no resources to dispose of, but other IRewinder impls might
		}
	}
}
