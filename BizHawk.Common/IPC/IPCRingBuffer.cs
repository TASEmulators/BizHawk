using System;
using System.Runtime.InteropServices; //I know.... the p/invoke in here. Lets get rid of it
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;


namespace BizHawk.Common
{
	/// <summary>
	/// a ring buffer suitable for IPC. It uses a spinlock to control access, so overhead can be kept to a minimum. 
	/// you'll probably need to use this in pairs, so it will occupy two threads.
	/// </summary>
	public unsafe class IPCRingBuffer : IDisposable
	{
		MemoryMappedFile mmf;
		MemoryMappedViewAccessor mmva;

		byte* mmvaPtr;
		volatile byte* begin;
		volatile int* head, tail;
		int bufsize;

		public string Id;
		public bool Owner;

		/// <summary>
		/// note that a few bytes of the size will be used for a management area
		/// </summary>
		/// <param name="size"></param>
		public void Allocate(int size)
		{
			Owner = true;
			Id = SuperGloballyUniqueID.Next();
			mmf = MemoryMappedFile.CreateNew(Id, size);
			Setup(size);
		}

		public void Open(string id)
		{
			Id = id;
			mmf = MemoryMappedFile.OpenExisting(id);
			Setup(-1);
		}

		void Setup(int size)
		{
			bool init = size != -1;

			mmva = mmf.CreateViewAccessor();
			byte* tempPtr = null;
			mmva.SafeMemoryMappedViewHandle.AcquirePointer(ref tempPtr);
			mmvaPtr = tempPtr;

			//setup management area
			head = (int*)mmvaPtr;
			tail = (int*)mmvaPtr + 1;
			int* bufsizeptr = (int*)mmvaPtr + 2;
			begin = mmvaPtr + 12;

			if (init)
				*bufsizeptr = bufsize = size - 12;
			else bufsize = *bufsizeptr;
		}

		public void Dispose()
		{
			if (mmf == null) return;
			mmva.Dispose();
			mmf.Dispose();
			mmf = null;
		}

		void WaitForWriteCapacity(int amt)
		{
			for (; ; )
			{
				//dont return when available == amt because then we would consume the buffer and be unable to distinguish between full and empty
				if (Available > amt)
					return;
				//this is a greedy spinlock.
				Thread.Yield();
			}
		}

		public int Available
		{
			get
			{
				return bufsize - Size;
			}
		}


		public int Size
		{
			get
			{
				int h = *head;
				int t = *tail;
				int size = h - t;
				if (size < 0) size += bufsize;
				else if (size >= bufsize)
				{
					//shouldnt be possible for size to be anything but bufsize here, but just in case...
					if (size > bufsize)
						throw new InvalidOperationException("Critical error code pickpocket panda! This MUST be reported to the developers!");
					size = 0;
				}
				return size;
			}
		}

		int WaitForSomethingToRead()
		{
			for (; ; )
			{
				int available = Size;
				if (available > 0)
					return available;
				//this is a greedy spinlock.
				Thread.Yield();
			}
		}

		[DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
		static unsafe extern void* CopyMemory(void* dest, void* src, ulong count);

		public void Write(IntPtr ptr, int amt)
		{
			byte* bptr = (byte*)ptr;
			int ofs = 0;
			while (amt > 0)
			{
				int todo = amt;

				//make sure we don't write a big chunk beyond the end of the buffer
				int remain = bufsize - *head;
				if (todo > remain) todo = remain;

				//dont request the entire buffer. we would never get that much available, because we never completely fill up the buffer
				if (todo > bufsize - 1) todo = bufsize - 1;

				//a super efficient approach would chunk this several times maybe instead of waiting for the buffer to be emptied before writing again. but who cares
				WaitForWriteCapacity(todo);

				//messages are likely to be small. we should probably just loop to copy in here. but for now..
				CopyMemory(begin + *head, bptr + ofs, (ulong)todo);

				amt -= todo;
				ofs += todo;
				*head += todo;
				if (*head >= bufsize) *head -= bufsize;
			}
		}

		public void Read(IntPtr ptr, int amt)
		{
			byte* bptr = (byte*)ptr;
			int ofs = 0;
			while (amt > 0)
			{
				int available = WaitForSomethingToRead();
				int todo = amt;
				if (todo > available) todo = available;

				//make sure we don't read a big chunk beyond the end of the buffer
				int remain = bufsize - *tail;
				if (todo > remain) todo = remain;

				//messages are likely to be small. we should probably just loop to copy in here. but for now..
				CopyMemory(bptr + ofs, begin + *tail, (ulong)todo);

				amt -= todo;
				ofs += todo;
				*tail += todo;
				if (*tail >= bufsize) *tail -= bufsize;
			}
		}

		public class Tester
		{
			private readonly Queue<byte> shazam = new Queue<byte>();
			string bufid;

			unsafe void a()
			{
				var buf = new IPCRingBuffer();
				buf.Allocate(1024);
				bufid = buf.Id;

				int ctr = 0;
				for (; ; )
				{
					Random r = new Random(ctr);
					ctr++;
					Console.WriteLine("Writing: {0}", ctr);

					byte[] temp = new byte[r.Next(2048) + 1];
					r.NextBytes(temp);
					for (int i = 0; i < temp.Length; i++)
						lock (shazam) shazam.Enqueue(temp[i]);
					fixed (byte* tempptr = &temp[0])
						buf.Write((IntPtr)tempptr, temp.Length);
					//Console.WriteLine("wrote {0}; ringbufsize={1}", temp.Length, buf.Size);
				}
			}

			unsafe void b()
			{
				var buf = new IPCRingBuffer();
				buf.Open(bufid);

				int ctr = 0;
				for (; ; )
				{
					Random r = new Random(ctr + 1000);
					ctr++;
					Console.WriteLine("Reading : {0}", ctr);

					int tryRead = r.Next(2048) + 1;
					byte[] temp = new byte[tryRead];
					fixed (byte* tempptr = &temp[0])
						buf.Read((IntPtr)tempptr, tryRead);
					//Console.WriteLine("read {0}; ringbufsize={1}", temp.Length, buf.Size);
					for (int i = 0; i < temp.Length; i++)
					{
						byte b;
						lock (shazam) b = shazam.Dequeue();
						Debug.Assert(b == temp[i]);
					}
				}
			}

			public void Test()
			{
				var ta = new System.Threading.Thread(a);
				var tb = new System.Threading.Thread(b);
				ta.Start();
				while (bufid == null) { }
				tb.Start();
			}
		}
	} //class IPCRingBuffer 

	/// <summary>
	/// A stream on top of an IPCRingBuffer
	/// </summary>
	public unsafe class IPCRingBufferStream : Stream
	{
		private readonly IPCRingBuffer buf;

		public IPCRingBufferStream(IPCRingBuffer buf)
		{
			this.buf = buf;
		}

		public override bool CanRead { get { return true; } }
		public override bool CanSeek { get { return false; } }
		public override bool CanWrite { get { return true; } }
		public override void Flush() { }
		public override long Length { get { throw new NotImplementedException(); } }
		public override long Position { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
		public override long Seek(long offset, SeekOrigin origin) { throw new NotImplementedException(); }
		public override void SetLength(long value) { throw new NotImplementedException(); }
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (buffer.Length < offset + count) throw new IndexOutOfRangeException();
			if (buffer.Length != 0)
				fixed (byte* pbuffer = &buffer[offset])
					buf.Read((IntPtr)pbuffer, count);
			return count;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			if (buffer.Length < offset + count) throw new IndexOutOfRangeException();
			if (buffer.Length != 0)
				fixed (byte* pbuffer = &buffer[offset])
					buf.Write((IntPtr)pbuffer, count);
		}
	} //class IPCRingBufferStream

} //namespace BizHawk.Common