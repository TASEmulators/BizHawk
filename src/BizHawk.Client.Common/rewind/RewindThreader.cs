using System;
using System.Collections.Concurrent;
using System.Threading;

namespace BizHawk.Client.Common
{
	public class RewindThreader : IDisposable
	{
		private readonly bool _isThreaded;
		private readonly Action<byte[]> _performCapture;
		private readonly Action<int> _performRewind;
		private readonly BlockingCollection<Job> _jobs = new BlockingCollection<Job>(16);
		private readonly ConcurrentStack<byte[]> _stateBufferPool = new ConcurrentStack<byte[]>();
		private readonly EventWaitHandle _rewindCompletedEvent;
		private readonly Thread _thread;

		public RewindThreader(Action<byte[]> performCapture, Action<int> performRewind, bool isThreaded)
		{
			_isThreaded = isThreaded;
			_performCapture = performCapture;
			_performRewind = performRewind;

			if (_isThreaded)
			{
				_rewindCompletedEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
				_thread = new Thread(ThreadProc) { IsBackground = true };
				_thread.Start();
			}
		}

		public void Dispose()
		{
			if (!_isThreaded)
			{
				return;
			}

			_jobs.CompleteAdding();
			_thread.Join();
			_rewindCompletedEvent.Dispose();
		}

		public void Rewind(int frames)
		{
			if (!_isThreaded)
			{
				_performRewind(frames);
				return;
			}

			_jobs.Add(new Job
			{
				Type = JobType.Rewind,
				Frames = frames
			});
			_rewindCompletedEvent.WaitOne();
		}

		public void Capture(byte[] coreSavestate)
		{
			if (!_isThreaded)
			{
				_performCapture(coreSavestate);
				return;
			}

			byte[] savestateCopy;
			while (_stateBufferPool.TryPop(out savestateCopy) && savestateCopy.Length != coreSavestate.Length)
			{
				savestateCopy = null;
			}

			savestateCopy ??= new byte[coreSavestate.Length];

			Buffer.BlockCopy(coreSavestate, 0, savestateCopy, 0, coreSavestate.Length);

			_jobs.Add(new Job
			{
				Type = JobType.Capture,
				CoreState = savestateCopy
			});
		}

		private void ThreadProc()
		{
			foreach (Job job in _jobs.GetConsumingEnumerable())
			{
				if (job.Type == JobType.Capture)
				{
					_performCapture(job.CoreState);
					_stateBufferPool.Push(job.CoreState);
				}

				if (job.Type == JobType.Rewind)
				{
					_performRewind(job.Frames);
					_rewindCompletedEvent.Set();
				}
			}
		}

		private enum JobType
		{
			Capture, Rewind
		}

		private sealed class Job
		{
			public JobType Type { get; set; }
			public byte[] CoreState { get; set; }
			public int Frames { get; set; }
		}
	}
}
