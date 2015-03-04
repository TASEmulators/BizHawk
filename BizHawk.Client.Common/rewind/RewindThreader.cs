using System;
using System.Collections.Concurrent;
using System.Threading;

namespace BizHawk.Client.Common
{
	public class RewindThreader : IDisposable
	{
		public static bool IsThreaded = false;

		private readonly ConcurrentQueue<Job> _jobs = new ConcurrentQueue<Job>();
		private readonly ConcurrentStack<byte[]> _stateBufferPool = new ConcurrentStack<byte[]>();
		private readonly EventWaitHandle _ewh;
		private readonly EventWaitHandle _ewh2;
		private readonly Thread _thread;
		private readonly Rewinder _rewinder;

		public RewindThreader(Rewinder rewinder, bool isThreaded)
		{
			IsThreaded = isThreaded;
			_rewinder = rewinder;

			if (IsThreaded)
			{
				_ewh = new EventWaitHandle(false, EventResetMode.AutoReset);
				_ewh2 = new EventWaitHandle(false, EventResetMode.AutoReset);
				_thread = new Thread(ThreadProc) { IsBackground = true };
				_thread.Start();
			}
		}

		public void Dispose()
		{
			if (!IsThreaded)
			{
				return;
			}

			var job = new Job { Type = JobType.Abort };
			_jobs.Enqueue(job);
			_ewh.Set();

			_thread.Join();
			_ewh.Dispose();
			_ewh2.Dispose();
		}

		public void Rewind(int frames)
		{
			if (!IsThreaded)
			{
				_rewinder._RunRewind(frames);
				return;
			}

			var job = new Job
			{
				Type = JobType.Rewind,
				Frames = frames
			};
			_jobs.Enqueue(job);
			_ewh.Set();
			_ewh2.WaitOne();
		}

		public void Capture(byte[] coreSavestate)
		{
			if (!IsThreaded)
			{
				_rewinder.RunCapture(coreSavestate);
				return;
			}

			byte[] savestateCopy = null;
			while (_stateBufferPool.TryPop(out savestateCopy) && savestateCopy.Length != coreSavestate.Length)
			{
				savestateCopy = null;
			}
			if (savestateCopy == null)
			{
				savestateCopy = new byte[coreSavestate.Length];
			}

			Buffer.BlockCopy(coreSavestate, 0, savestateCopy, 0, coreSavestate.Length);

			var job = new Job
			{
				Type = JobType.Capture,
				CoreState = savestateCopy
			};
			DoSafeEnqueue(job);
		}

		private void ThreadProc()
		{
			for (;; )
			{
				_ewh.WaitOne();
				while (_jobs.Count != 0)
				{
					Job job;
					if (_jobs.TryDequeue(out job))
					{
						if (job.Type == JobType.Abort)
						{
							return;
						}

						if (job.Type == JobType.Capture)
						{
							_rewinder.RunCapture(job.CoreState);
							_stateBufferPool.Push(job.CoreState);
						}

						if (job.Type == JobType.Rewind)
						{
							_rewinder._RunRewind(job.Frames);
							_ewh2.Set();
						}
					}
				}
			}
		}

		private void DoSafeEnqueue(Job job)
		{
			_jobs.Enqueue(job);
			_ewh.Set();

			// just in case... we're getting really behind.. slow it down here
			// if this gets backed up too much, then the rewind will seem to malfunction since it requires all the captures in the queue to complete first
			while (_jobs.Count > 15)
			{
				Thread.Sleep(0);
			}
		}

		private enum JobType
		{
			Capture, Rewind, Abort
		}

		private sealed class Job
		{
			public JobType Type { get; set; }
			public byte[] CoreState { get; set; }
			public int Frames { get; set; }
		}
	}
}
