using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace BizHawk.MultiClient
{
	public partial class MainForm
	{
		private StreamBlobDatabase RewindBuf = new StreamBlobDatabase(Global.Config.Rewind_OnDisk, Global.Config.Rewind_BufferSize * (long)1024 * (long)1024);
		private RewindThreader RewindThread;

		private byte[] LastState;
		private bool RewindImpossible;
		private int RewindFrequency = 1;
		private bool RewindDeltaEnable = false;

		public float Rewind_FullnessRatio { get { return RewindBuf.FullnessRatio; } }
		public int Rewind_Count { get { return RewindBuf.Count; } }
        public long Rewind_Size { get { return RewindBuf.Size; } }
		/// <summary>
		/// Manages a ring buffer of storage which can continually chow its own tail to keep growing forward.
		/// Probably only useful for the rewind buffer, so I didnt put it in another file
		/// </summary>
		class StreamBlobDatabase : IDisposable
		{
			public void Dispose()
			{
				mStream.Dispose();
				mStream = null;
			}
			
			public StreamBlobDatabase(bool onDisk, long capacity)
			{
				mCapacity = capacity;
				if (onDisk)
				{
					var path = Path.Combine(System.IO.Path.GetTempPath(), "bizhawk.rewindbuf-pid" + System.Diagnostics.Process.GetCurrentProcess().Id + "-" + Guid.NewGuid().ToString());
					
					//I checked the DeleteOnClose operation to make sure it cleans up when the process is aborted, and it seems to.
					//Otherwise we would have a more complex tempfile management problem here.
					//4KB buffer chosen due to similarity to .net defaults, and fear of anything larger making hiccups for small systems (we could try asyncing this stuff though...)
					mStream = new FileStream(path, FileMode.Create, System.Security.AccessControl.FileSystemRights.FullControl, FileShare.None, 4*1024, FileOptions.DeleteOnClose);
				}
				else
				{
					var buffer = new byte[capacity];
					mStream = new MemoryStream(buffer);
				}
			}

			public class ListItem
			{
				public ListItem(int _timestamp, long _index, int _length) { this.timestamp = _timestamp; this.index = _index; this.length = _length; }
				public int timestamp;
				public long index;
				public int length;
				public long endExclusive { get { return index + length; } }
			}

			Stream mStream;
			LinkedList<ListItem> mBookmarks = new LinkedList<ListItem>();
			LinkedListNode<ListItem> mHead, mTail;
			long mCapacity, mSize;

			/// <summary>
			/// Returns the amount of the buffer that's used
			/// </summary>
			public long Size { get { return mSize; } }

			/// <summary>
			/// Gets the current fullness ratio (Size/Capacity). Note that this wont reach 100% due to the buffer size not being a multiple of a fixed savestate size.
			/// </summary>
			public float FullnessRatio { get { return (float)((double)Size / (double)mCapacity); } }

			/// <summary>
			/// the number of frames stored here
			/// </summary>
			public int Count { get { return mBookmarks.Count; } }

			/// <summary>
			/// The underlying stream to 
			/// </summary>
			public Stream Stream { get { return mStream; } }

			public void Clear()
			{
				mHead = mTail = null;
				mSize = 0;
				mBookmarks.Clear();
			}

			/// <summary>
			/// The push and pop semantics are for historical reasons and not resemblence to normal definitions
			/// </summary>
			public void Push(ArraySegment<byte> seg)
			{
				var buf = seg.Array;
				int len = seg.Count;
				long offset = Enqueue(0, len);
				mStream.Position = offset;
				mStream.Write(buf, seg.Offset, len);
			}

			/// <summary>
			/// The push and pop semantics are for historical reasons and not resemblence to normal definitions
			/// </summary>
			public MemoryStream PopMemoryStream()
			{
				var item = Pop();
				var buf = new byte[item.length];
				mStream.Position = item.index;
				mStream.Read(buf, 0, item.length);
				var ret = new MemoryStream(buf, 0, item.length, false, true);
				return ret;
			}

			public long Enqueue(int timestamp, int amount)
			{
				mSize += amount;

				if (mHead == null)
				{
					mTail = mHead = mBookmarks.AddFirst(new ListItem(timestamp, 0, amount));
					return 0;
				}

				long target = mHead.Value.endExclusive + amount;
				if (mTail != null && target <= mTail.Value.index)
				{
					//theres room to add a new head before the tail
					mHead = mBookmarks.AddAfter(mHead, new ListItem(timestamp, mHead.Value.endExclusive, amount));
					goto CLEANUP;
				}

				//maybe the tail is earlier than the head
				if (mTail.Value.index < mHead.Value.index)
				{
					if (target <= mCapacity)
					{
						//theres room to add a new head before the end of capacity
						mHead = mBookmarks.AddAfter(mHead, new ListItem(timestamp, mHead.Value.endExclusive, amount));
						goto CLEANUP;
					}
				}
				else
				{
					//nope, tail is after head. we'll have to clobber from the tail
					mHead = mBookmarks.AddAfter(mHead, new ListItem(timestamp, mHead.Value.endExclusive, amount));
					goto CLEANUP;
				}

				//no room before the tail, or before capacity. head needs to wrap around.
				mHead = mBookmarks.AddAfter(mHead, new ListItem(timestamp, 0, amount));

			CLEANUP:
				//while the head impinges on tail items, discard them
				for (; ; )
				{
					if (mTail == null) break;
					if (mHead.Value.endExclusive > mTail.Value.index && mHead.Value.index <= mTail.Value.index)
					{
						LinkedListNode<ListItem> nextTail = mTail.Next;
						mSize -= mTail.Value.length;
						mBookmarks.Remove(mTail);
						mTail = nextTail;
					}
					else break;
				}

				return mHead.Value.index;
			}

			public ListItem Pop()
			{
				if (mHead == null) throw new InvalidOperationException("Attempted to pop from an empty data structure");
				var ret = mHead.Value;
				mSize -= ret.length;
				LinkedListNode<ListItem> nextHead = mHead.Previous;
				mBookmarks.Remove(mHead);
				if (mHead == mTail)
					mTail = null;
				mHead = nextHead;
				if (mHead == null)
					mHead = mBookmarks.Last;
				return ret;
			}

			public ListItem Dequeue()
			{
				if (mTail == null) throw new InvalidOperationException("Attempted to dequeue from an empty data structure");
				var ret = mTail.Value;
				mSize -= ret.length;
				LinkedListNode<ListItem> nextTail = mTail.Next;
				mBookmarks.Remove(mTail);
				if (mTail == mHead)
					mHead = null;
				mTail = nextTail;
				if (mTail == null)
					mTail = mBookmarks.First;
				return ret;
			}

			//-------- tests ---------
			public void AssertMonotonic()
			{
				if (mTail == null) return;
				int ts = mTail.Value.timestamp;
				LinkedListNode<ListItem> curr = mTail;
				for (; ; )
				{
					if (curr == null)
						curr = mBookmarks.First;
					if (curr == null) break;
					System.Diagnostics.Debug.Assert(curr.Value.timestamp >= ts);
					if (curr == mHead) return;
					ts = curr.Value.timestamp;
					curr = curr.Next;
				}
			}

			void Test()
			{
				var sbb = new StreamBlobDatabase(false, Global.Config.Rewind_BufferSize * 1024 * 1024);
				var rand = new Random(0);
				int timestamp = 0;
				for (; ; )
				{
					long test = sbb.Enqueue(timestamp, rand.Next(100 * 1024));
					if (rand.Next(10) == 0)
						if (sbb.Count != 0) sbb.Dequeue();
					if (rand.Next(10) == 0)
						if (sbb.Count != 0) sbb.Pop();
					if (rand.Next(50) == 1)
					{
						while (sbb.Count != 0)
						{
							Console.WriteLine("ZAM!!!");
							sbb.Dequeue();
						}
					}
					sbb.AssertMonotonic();
					timestamp++;
					Console.WriteLine("{0}, {1}", test, sbb.Count);
				}
			}
		} //class StreamBlobDatabase

		class RewindThreader : IDisposable
		{
			//adelikat: tweak this to test performance with threading or not with threading
			public static bool IsThreaded = true;

			MainForm mf;

			public RewindThreader(MainForm mf, bool isThreaded)
			{
				IsThreaded = isThreaded;
				this.mf = mf;

				if (IsThreaded)
				{
					ewh = new EventWaitHandle(false, EventResetMode.AutoReset);
					ewh2 = new EventWaitHandle(false, EventResetMode.AutoReset);
					thread = new Thread(ThreadProc);
					thread.IsBackground = true;
					thread.Start();
				}
			}

			public void Dispose()
			{
				if (!IsThreaded)
					return;

				var job = new Job();
				job.Type = JobType.Abort;
				Jobs.Enqueue(job);
				ewh.Set();

				thread.Join();
				ewh.Dispose();
				ewh2.Dispose();
			}

			void ThreadProc()
			{
				for (; ; )
				{
					ewh.WaitOne();
					while (Jobs.Count != 0)
					{
						Job job = null;
						if (Jobs.TryDequeue(out job))
						{
							if (job.Type == JobType.Abort)
								return;
							if (job.Type == JobType.Capture)
							{
								mf._RunCapture(job.CoreState);
							}
							if (job.Type == JobType.Rewind)
							{
								mf._RunRewind(job.Frames);
								ewh2.Set();
							}
						}
					}
				}
			}

			EventWaitHandle ewh, ewh2;
			Thread thread;	

			public void Rewind(int frames)
			{
				if (!IsThreaded)
				{
					mf._RunRewind(frames);
					return;
				}

				var job = new Job();
				job.Type = JobType.Rewind;
				job.Frames = frames;
				Jobs.Enqueue(job);
				ewh.Set();
				ewh2.WaitOne();
			}

			void DoSafeEnqueue(Job job)
			{
				Jobs.Enqueue(job);
				ewh.Set();

				//just in case... we're getting really behind.. slow it down here
				//if this gets backed up too much, then the rewind will seem to malfunction since it requires all the captures in the queue to complete first
				while (Jobs.Count > 15)
				{
					Thread.Sleep(0);
				}
			}

			public void Capture(byte[] coreSavestate)
			{
				if (!IsThreaded)
				{
					mf._RunCapture(coreSavestate);
					return;
				}
				var job = new Job();
				job.Type = JobType.Capture;
				job.CoreState = coreSavestate;
				DoSafeEnqueue(job);
			}

			enum JobType
			{
				Capture, Rewind, Abort
			}

			class Job
			{
				public JobType Type;
				public byte[] CoreState;
				public int Frames;
			}

			ConcurrentQueue<Job> Jobs = new ConcurrentQueue<Job>();
		}

		private void CaptureRewindState()
		{
			if (RewindImpossible)
				return;

			if (LastState == null)
			{
				DoRewindSettings();
			}

		
			//log a frame
			if (LastState != null && Global.Emulator.Frame % RewindFrequency == 0)
			{
				byte[] CurrentState = Global.Emulator.SaveStateBinary();
				RewindThread.Capture(CurrentState);
			}
		}

		void SetRewindParams(bool enabled, int frequency)
		{
            if (RewindActive != enabled)
            {
                Global.OSD.AddMessage("Rewind " + (enabled ? "Enabled" : "Disabled"));
            }

            if (RewindFrequency != frequency && enabled)
            {
                Global.OSD.AddMessage("Rewind frequency set to " + frequency);
            }

			RewindActive = enabled;
			RewindFrequency = frequency;

			if(!RewindActive)
				LastState = null;
		}

		public void DoRewindSettings()
		{
			long cap = Global.Config.Rewind_BufferSize * (long)1024 * (long)1024;
			RewindBuf = new StreamBlobDatabase(Global.Config.Rewind_OnDisk, cap);
			if (RewindThread != null)
				RewindThread.Dispose();
			RewindThread = new RewindThreader(this, Global.Config.Rewind_IsThreaded);
			
			// This is the first frame. Capture the state, and put it in LastState for future deltas to be compared against.
			LastState = Global.Emulator.SaveStateBinary();

			if (LastState.Length >= Global.Config.Rewind_LargeStateSize)
			{
				SetRewindParams(Global.Config.RewindEnabledLarge, Global.Config.RewindFrequencyLarge);
			}
			else if (LastState.Length >= Global.Config.Rewind_MediumStateSize)
			{
				SetRewindParams(Global.Config.RewindEnabledMedium, Global.Config.RewindFrequencyMedium);
			}
			else
			{
				SetRewindParams(Global.Config.RewindEnabledSmall, Global.Config.RewindFrequencySmall);
			}

			RewindDeltaEnable = Global.Config.Rewind_UseDelta;
		}

		void CaptureRewindStateNonDelta(byte[] CurrentState)
		{
			long offset = RewindBuf.Enqueue(0, CurrentState.Length + 1);
			Stream stream = RewindBuf.Stream;
			stream.Position = offset;

			//write the header for a non-delta frame
			stream.WriteByte(1); //i.e. true
			stream.Write(CurrentState, 0, CurrentState.Length);
		}

		byte[] TempBuf = new byte[0];
		void CaptureRewindStateDelta(byte[] CurrentState, bool isSmall)
		{
			//in case the state sizes mismatch, capture a full state rather than trying to do anything clever
			if (CurrentState.Length != LastState.Length)
			{
				CaptureRewindStateNonDelta(CurrentState);
				return;
			}

			int beginChangeSequence = -1;
			bool inChangeSequence = false;
			MemoryStream ms;

			//try to set up the buffer in advance so we dont ever have exceptions in here
			if(TempBuf.Length < CurrentState.Length)
				TempBuf = new byte[CurrentState.Length*2];

			ms = new MemoryStream(TempBuf, 0, TempBuf.Length, true, true); 
		RETRY:
			try
			{
				var writer = new BinaryWriter(ms);
				writer.Write(false); // delta state
				for (int i = 0; i < CurrentState.Length; i++)
				{
					if (inChangeSequence == false)
					{
						if (i >= LastState.Length)
							continue;
						if (CurrentState[i] == LastState[i])
							continue;

						inChangeSequence = true;
						beginChangeSequence = i;
						continue;
					}

					if (i - beginChangeSequence == 254 || i == CurrentState.Length - 1)
					{
						writer.Write((byte)(i - beginChangeSequence + 1));
						if (isSmall) writer.Write((ushort)beginChangeSequence);
						else writer.Write(beginChangeSequence);
						writer.Write(LastState, beginChangeSequence, i - beginChangeSequence + 1);
						inChangeSequence = false;
						continue;
					}

					if (CurrentState[i] == LastState[i])
					{
						writer.Write((byte)(i - beginChangeSequence));
						if (isSmall) writer.Write((ushort)beginChangeSequence);
						else writer.Write(beginChangeSequence);
						writer.Write(LastState, beginChangeSequence, i - beginChangeSequence);
						inChangeSequence = false;
					}
				}
			}
			catch (NotSupportedException)
			{
				//ok... we had an exception after all
				//if we did actually run out of room in the memorystream, then try it again with a bigger buffer
				TempBuf = new byte[TempBuf.Length * 2];
				goto RETRY;
			}
			
			LastState = CurrentState;
			var seg = new ArraySegment<byte>(TempBuf, 0, (int)ms.Position);
			RewindBuf.Push(seg);
		}

		void RewindLarge() { RewindDelta(false); }
		void Rewind64K() { RewindDelta(true); }
		void RewindDelta(bool isSmall)
		{
			var ms = RewindBuf.PopMemoryStream();
			var reader = new BinaryReader(ms);
			bool fullstate = reader.ReadBoolean();
			if (fullstate)
			{
				Global.Emulator.LoadStateBinary(reader);
			}
			else
			{
				var output = new MemoryStream(LastState);
				while (ms.Position < ms.Length - 1)
				{
					byte len = reader.ReadByte();
					int offset;
					if(isSmall)
						offset = reader.ReadUInt16();
					else offset = reader.ReadInt32();
					output.Position = offset;
					output.Write(ms.GetBuffer(), (int)ms.Position, len);
					ms.Position += len;
				}
				reader.Close();
				output.Position = 0;
				Global.Emulator.LoadStateBinary(new BinaryReader(output));
			}
		}

		public void Rewind(int frames)
		{
			RewindThread.Rewind(frames);
		}

		void _RunRewind(int frames)
		{
			for (int i = 0; i < frames; i++)
			{
				if (RewindBuf.Count == 0 || (Global.MovieSession.Movie.Loaded && 0 == Global.MovieSession.Movie.Frames))
					return;

				if (LastState.Length < 0x10000)
					Rewind64K();
				else
					RewindLarge();
			}
		}

		void _RunCapture(byte[] coreSavestate)
		{
			if (RewindDeltaEnable)
			{
				if (LastState.Length <= 0x10000)
					CaptureRewindStateDelta(coreSavestate, true);
				else
					CaptureRewindStateDelta(coreSavestate, false);
			}
			else CaptureRewindStateNonDelta(coreSavestate);
		}

		public void ResetRewindBuffer()
		{
			RewindBuf.Clear();
			RewindImpossible = false;
			LastState = null;
		}

		public int RewindBufferCount()
		{
			return RewindBuf.Count;
		}
	}
}
