//controls whether the new shared memory ring buffer communication system is used
//on the whole it seems to boost performance slightly for me, at the cost of exacerbating spikes
//not sure if we should keep it
#define USE_BUFIO

using System;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;

namespace BizHawk.Emulation.Consoles.Nintendo.SNES
{

	/// <summary>
	/// a ring buffer suitable for IPC. It uses a spinlock to control access, so overhead can be kept to a minimum. 
	/// you'll probably need to use this in pairs, so it will occupy two threads and degrade entirely if there is less than one processor.
	/// </summary>
	unsafe class IPCRingBuffer : IDisposable
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
			Queue<byte> shazam = new Queue<byte>();
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
	}

	unsafe class IPCRingBufferStream : Stream
	{
		public IPCRingBufferStream(IPCRingBuffer buf)
		{
			this.buf = buf;
		}
		IPCRingBuffer buf;
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
			fixed (byte* pbuffer = &buffer[offset])
				buf.Read((IntPtr)pbuffer, count);
			return count;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			if (buffer.Length < offset + count) throw new IndexOutOfRangeException();
			fixed (byte* pbuffer = &buffer[offset])
				buf.Write((IntPtr)pbuffer, count);
		}
	}

	class SuperGloballyUniqueID
	{
		public static string Next()
		{
			int myctr;
			lock (typeof(SuperGloballyUniqueID))
				myctr = ctr++;
			return staticPart + "-" + myctr;
		}

		static SuperGloballyUniqueID()
		{
			staticPart = "bizhawk-" + System.Diagnostics.Process.GetCurrentProcess().Id.ToString() + "-" + Guid.NewGuid().ToString();
		}

		static int ctr;
		static string staticPart;
	}

	public unsafe class LibsnesApi : IDisposable
	{
		//this wouldve been the ideal situation to learn protocol buffers, but since the number of messages here is so limited, it took less time to roll it by hand.
		//todo - could optimize a lot of the apis once we decide to commit to this. will we? then we wont be able to debug bsnes as well
		//        well, we could refactor it a lot and let the debuggable static dll version be the one that does annoying workarounds
		//todo - more intelligent use of buffers to avoid so many copies (especially framebuffer from bsnes? supply framebuffer to-be-used to libsnes? same for audiobuffer)
		//todo - refactor to use a smarter set of pipe reader and pipe writer classes
		//todo - combine messages / tracecallbacks into one system with a channel number enum additionally
		//todo - consider refactoring bsnes to allocate memory blocks through the interface, and set ours up to allocate from a large arena of shared memory.
		//        this is a lot of work, but it will be some decent speedups. who wouldve ever thought to make an emulator this way? I will, from now on...
		//todo - use a reader/writer ring buffer for communication instead of pipe
		//todo - when exe wrapper is fully baked, put it into mingw so we can just have libsneshawk.exe without a separate dll. it hardly needs any debugging presently, it should be easy to maintain.
		
		//space optimizations to deploy later (only if people complain about so many files)
		//todo - put executables in zipfiles and search for them there; dearchive to a .cache folder. check timestamps to know when to freshen. this is weird.....

		//speedups to deploy later:
		//todo - convey rom data faster than pipe blob (use shared memory) (WARNING: right now our general purpose shared memory is only 1MB. maybe wait until ring buffer IPC)
		//todo - collapse input messages to one IPC operation. right now theresl ike 30 of them
		//todo - collect all memory block names whenever a memory block is alloc/dealloced. that way we avoid the overhead when using them for gui stuff (gfx debugger, hex editor)

		string InstanceName;
		Process process;
		NamedPipeServerStream pipe;
		BinaryWriter bwPipe;
		BinaryReader brPipe;
		MemoryMappedFile mmf;
		MemoryMappedViewAccessor mmva;
		byte* mmvaPtr;
		IPCRingBuffer rbuf, wbuf;
		IPCRingBufferStream rbufstr, wbufstr;
		SwitcherStream rstream, wstream;
		bool bufio;

		public enum eMessage : int
		{
			eMessage_Complete,

			eMessage_snes_library_id,
			eMessage_snes_library_revision_major,
			eMessage_snes_library_revision_minor,

			eMessage_snes_init,
			eMessage_snes_power,
			eMessage_snes_reset,
			eMessage_snes_run,
			eMessage_snes_term,
			eMessage_snes_unload_cartridge,

			//snes_set_cartridge_basename, //not used

			eMessage_snes_load_cartridge_normal,
			eMessage_snes_load_cartridge_super_game_boy,

			//incoming from bsnes
			eMessage_snes_cb_video_refresh,
			eMessage_snes_cb_input_poll,
			eMessage_snes_cb_input_state,
			eMessage_snes_cb_input_notify,
			eMessage_snes_cb_audio_sample,
			eMessage_snes_cb_scanlineStart,
			eMessage_snes_cb_path_request,
			eMessage_snes_cb_trace_callback,

			eMessage_snes_get_region,

			eMessage_snes_get_memory_size,
			eMessage_snes_get_memory_data,
			eMessage_peek,
			eMessage_poke,

			eMessage_snes_serialize_size,

			eMessage_snes_serialize,
			eMessage_snes_unserialize,

			eMessage_snes_poll_message,
			eMessage_snes_dequeue_message,

			eMessage_snes_set_color_lut,

			eMessage_snes_enable_trace,
			eMessage_snes_enable_scanline,
			eMessage_snes_enable_audio,
			eMessage_snes_set_layer_enable,
			eMessage_snes_set_backdropColor,
			eMessage_snes_peek_logical_register,

			eMessage_snes_allocSharedMemory,
			eMessage_snes_freeSharedMemory,
			eMessage_GetMemoryIdName,

			eMessage_SetBuffer,
			eMessage_BeginBufferIO,
			eMessage_EndBufferIO
		};

		static bool DryRun(string exePath)
		{
			ProcessStartInfo oInfo = new ProcessStartInfo(exePath, "Bongizong");
			oInfo.WorkingDirectory = Path.GetDirectoryName(exePath);
			oInfo.UseShellExecute = false;
			oInfo.CreateNoWindow = true;
			oInfo.RedirectStandardOutput = true;
			oInfo.RedirectStandardError = true;

			Process proc = System.Diagnostics.Process.Start(oInfo);
			string result = proc.StandardError.ReadToEnd();
			proc.WaitForExit();

			//yongou chonganong nongo tong rongeadong
			//pongigong chong hongi nonge songe
			if (result == "Honga Wongkong" && proc.ExitCode == 0x16817)
				return true;

			return false;
		}

		static HashSet<string> okExes = new HashSet<string>();
		public LibsnesApi(string exePath)
		{
			//make sure we've checked this exe for OKness.. the dry run should keep us from freezing up or crashing weirdly if the external process isnt correct
			if (!okExes.Contains(exePath))
			{
				bool ok = DryRun(exePath);
				if (!ok)
					throw new InvalidOperationException(string.Format("Couldn't launch {0} to run SNES core. Not sure why this would have happened. Try redownloading BizHawk first.", Path.GetFileName(exePath)));
				okExes.Add(exePath);
			}

			InstanceName = "libsneshawk_" + Guid.NewGuid().ToString();

			//use this to get a debug console with libsnes output
			//InstanceName = "console-" + InstanceName;

			var pipeName = InstanceName;

			mmf = MemoryMappedFile.CreateNew(pipeName, 1024 * 1024);
			mmva = mmf.CreateViewAccessor();
			mmva.SafeMemoryMappedViewHandle.AcquirePointer(ref mmvaPtr);

			pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, 1024 * 1024, 1024);

			process = new Process();
			process.StartInfo.WorkingDirectory = Path.GetDirectoryName(exePath);
			process.StartInfo.FileName = exePath;
			process.StartInfo.Arguments = pipeName;
			process.StartInfo.ErrorDialog = true;
			process.Start();

			//TODO - start a thread to wait for process to exit and gracefully handle errors? how about the pipe?

			pipe.WaitForConnection();

			rbuf = new IPCRingBuffer();
			wbuf = new IPCRingBuffer();
			rbuf.Allocate(1024);
			wbuf.Allocate(1024);
			rbufstr = new IPCRingBufferStream(rbuf);
			wbufstr = new IPCRingBufferStream(wbuf);

			rstream = new SwitcherStream();
			wstream = new SwitcherStream();

			rstream.SetCurrStream(pipe);
			wstream.SetCurrStream(pipe);

			brPipe = new BinaryReader(rstream);
			bwPipe = new BinaryWriter(wstream);

			WritePipeMessage(eMessage.eMessage_SetBuffer);
			bwPipe.Write(1);
			WritePipeString(rbuf.Id);
			WritePipeMessage(eMessage.eMessage_SetBuffer);
			bwPipe.Write(0);
			WritePipeString(wbuf.Id);
			bwPipe.Flush();
		}

		class SwitcherStream : Stream
		{
			//switchstream method? flush old stream?
			Stream CurrStream = null;

			public void SetCurrStream(Stream str) { CurrStream = str; }

			public SwitcherStream()
			{
			}

			public override bool CanRead { get { return CurrStream.CanRead; } }
			public override bool CanSeek { get { return CurrStream.CanSeek; } }
			public override bool CanWrite { get { return CurrStream.CanWrite; } }
			public override void Flush() 
			{
				CurrStream.Flush();
			}

			public override long Length { get { return CurrStream.Length; } }

			public override long Position
			{
				get
				{
					return CurrStream.Position;
				}
				set
				{
					CurrStream.Position = Position;
				}
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				return CurrStream.Read(buffer, offset, count);
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				return CurrStream.Seek(offset, origin);
			}

			public override void SetLength(long value)
			{
				CurrStream.SetLength(value);
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				CurrStream.Write(buffer, offset, count);
			}
		}

		public void Dispose()
		{
			process.Kill();
			process.Dispose();
			process = null;
			pipe.Dispose();
			mmva.Dispose();
			mmf.Dispose();
			rbuf.Dispose();
			wbuf.Dispose();
		}

		public void BeginBufferIO()
		{
#if USE_BUFIO
			bufio = true;
			WritePipeMessage(eMessage.eMessage_BeginBufferIO);
			rstream.SetCurrStream(rbufstr);
			wstream.SetCurrStream(wbufstr);
#endif
		}

		public void EndBufferIO()
		{
#if USE_BUFIO
			bufio = false;
			WritePipeMessage(eMessage.eMessage_EndBufferIO);
			rstream.SetCurrStream(pipe);
			wstream.SetCurrStream(pipe);
#endif
		}

		void WritePipeString(string str)
		{
			WritePipeBlob(System.Text.Encoding.ASCII.GetBytes(str));
		}

		byte[] ReadPipeBlob()
		{
			int len = brPipe.ReadInt32();
			var ret = new byte[len];
			brPipe.Read(ret, 0, len);
			return ret;
		}

		void WritePipeBlob(byte[] blob)
		{
			bwPipe.Write(blob.Length);
			bwPipe.Write(blob);
			bwPipe.Flush();
		}

		public int MessageCounter;

		void WritePipeMessage(eMessage msg)
		{
			if(!bufio) MessageCounter++;
			bwPipe.Write((int)msg);
			bwPipe.Flush();
		}

		eMessage ReadPipeMessage()
		{
			return (eMessage)brPipe.ReadInt32();
		}

		string ReadPipeString()
		{
			int len = brPipe.ReadInt32();
			var bytes = brPipe.ReadBytes(len);
			return System.Text.ASCIIEncoding.ASCII.GetString(bytes);
		}

		public string snes_library_id()
		{
			WritePipeMessage(eMessage.eMessage_snes_library_id);
			return ReadPipeString();
		}


		public uint snes_library_revision_major()
		{
			WritePipeMessage(eMessage.eMessage_snes_library_revision_major);
			return brPipe.ReadUInt32();
		}

		public uint snes_library_revision_minor()
		{
			WritePipeMessage(eMessage.eMessage_snes_library_revision_minor);
			return brPipe.ReadUInt32();
		}

		public void snes_init()
		{
			WritePipeMessage(eMessage.eMessage_snes_init);
			WaitForCompletion();
		}
		public void snes_power() { WritePipeMessage(eMessage.eMessage_snes_power); }
		public void snes_reset() { WritePipeMessage(eMessage.eMessage_snes_reset); }
		public void snes_run()
		{
			WritePipeMessage(eMessage.eMessage_snes_run);
			WaitForCompletion();
		}
		public void snes_term() { WritePipeMessage(eMessage.eMessage_snes_term); }
		public void snes_unload_cartridge() { WritePipeMessage(eMessage.eMessage_snes_unload_cartridge); }

		public bool snes_load_cartridge_super_game_boy(string rom_xml, byte[] rom_data, uint rom_size, string dmg_xml, byte[] dmg_data, uint dmg_size)
		{
			WritePipeMessage(eMessage.eMessage_snes_load_cartridge_super_game_boy);
			WritePipeString(rom_xml ?? "");
			WritePipeBlob(rom_data);
			WritePipeString(rom_xml ?? "");
			WritePipeBlob(dmg_data);
			//not a very obvious order.. because we do tons of work immediately after the last param goes down and need to answer messages
			WaitForCompletion();
			bool ret = brPipe.ReadBoolean();
			return ret;
		}

		public bool snes_load_cartridge_normal(string rom_xml, byte[] rom_data)
		{
			WritePipeMessage(eMessage.eMessage_snes_load_cartridge_normal);
			WritePipeString(rom_xml ?? "");
			WritePipeBlob(rom_data);
			//not a very obvious order.. because we do tons of work immediately after the last param goes down and need to answer messages
			WaitForCompletion();
			bool ret = brPipe.ReadBoolean();
			return ret;
		}

		public SNES_REGION snes_get_region()
		{
			WritePipeMessage(eMessage.eMessage_snes_get_region);
			return (SNES_REGION)brPipe.ReadByte();
		}

		public int snes_get_memory_size(SNES_MEMORY id)
		{
			WritePipeMessage(eMessage.eMessage_snes_get_memory_size);
			bwPipe.Write((int)id);
			bwPipe.Flush();
			return brPipe.ReadInt32();
		}

		string MemoryNameForId(SNES_MEMORY id)
		{
			WritePipeMessage(eMessage.eMessage_GetMemoryIdName);
			bwPipe.Write((uint)id);
			bwPipe.Flush();
			return ReadPipeString();
		}

		public byte* snes_get_memory_data(SNES_MEMORY id)
		{
			string name = MemoryNameForId(id);
			var smb = SharedMemoryBlocks[name];
			return (byte*)smb.Ptr;
		}

		public byte peek(SNES_MEMORY id, uint addr)
		{
			WritePipeMessage(eMessage.eMessage_peek);
			bwPipe.Write((uint)id);
			bwPipe.Write(addr);
			bwPipe.Flush();
			return brPipe.ReadByte();
		}
		public void poke(SNES_MEMORY id, uint addr, byte val)
		{
			WritePipeMessage(eMessage.eMessage_poke);
			bwPipe.Write((uint)id);
			bwPipe.Write(addr);
			bwPipe.Write(val);
			bwPipe.Flush();
		}

		public int snes_serialize_size()
		{
			WritePipeMessage(eMessage.eMessage_snes_serialize_size);
			return brPipe.ReadInt32();
		}

		[DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
		public static unsafe extern void* CopyMemory(void* dest, void* src, ulong count);

		public bool snes_serialize(IntPtr data, int size)
		{
			WritePipeMessage(eMessage.eMessage_snes_serialize);
			bwPipe.Write(size);
			bwPipe.Write(0); //mapped memory location to serialize to
			bwPipe.Flush();
			WaitForCompletion(); //serialize/unserialize can cause traces to get called (because serialize can cause execution?)
			bool ret = brPipe.ReadBoolean();
			if (ret)
			{
				CopyMemory(data.ToPointer(), mmvaPtr, (ulong)size);
			}
			return ret;
		}

		public bool snes_unserialize(IntPtr data, int size)
		{
			WritePipeMessage(eMessage.eMessage_snes_unserialize);
			CopyMemory(mmvaPtr, data.ToPointer(), (ulong)size);
			bwPipe.Write(size);
			bwPipe.Write(0); //mapped memory location to serialize from
			bwPipe.Flush();
			WaitForCompletion(); //serialize/unserialize can cause traces to get called (because serialize can cause execution?)
			bool ret = brPipe.ReadBoolean();
			return ret;
		}

		int snes_poll_message()
		{
			WritePipeMessage(eMessage.eMessage_snes_poll_message);
			return brPipe.ReadInt32();
		}

		public bool HasMessage { get { return snes_poll_message() != -1; } }


		public string DequeueMessage()
		{
			WritePipeMessage(eMessage.eMessage_snes_dequeue_message);
			return ReadPipeString();
		}

		public void snes_set_color_lut(IntPtr colors)
		{
			int len = 4 * 16 * 32768;
			byte[] buf = new byte[len];
			Marshal.Copy(colors, buf, 0, len);

			WritePipeMessage(eMessage.eMessage_snes_set_color_lut);
			WritePipeBlob(buf);
		}

		public void snes_set_layer_enable(int layer, int priority, bool enable)
		{
			WritePipeMessage(eMessage.eMessage_snes_set_layer_enable);
			bwPipe.Write(layer);
			bwPipe.Write(priority);
			bwPipe.Write(enable);
			bwPipe.Flush();
		}

		public void snes_set_backdropColor(int backdropColor)
		{
			WritePipeMessage(eMessage.eMessage_snes_set_backdropColor);
			bwPipe.Write(backdropColor);
			bwPipe.Flush();
		}

		public int snes_peek_logical_register(SNES_REG reg)
		{
			WritePipeMessage(eMessage.eMessage_snes_peek_logical_register);
			bwPipe.Write((int)reg);
			bwPipe.Flush();
			return brPipe.ReadInt32();
		}

		void WaitForCompletion()
		{
			for (; ; )
			{
				var msg = ReadPipeMessage();
				if (!bufio) MessageCounter++;
				//Console.WriteLine(msg);
				switch (msg)
				{
					case eMessage.eMessage_Complete:
						return;

					case eMessage.eMessage_snes_cb_video_refresh:
						{
							int width = brPipe.ReadInt32();
							int height = brPipe.ReadInt32();
							bwPipe.Write(0); //offset in mapped memory buffer
							bwPipe.Flush();
							brPipe.ReadBoolean(); //dummy synchronization
							if (video_refresh != null)
							{
								video_refresh((int*)mmvaPtr, width, height);
							}
							break;
						}
					case eMessage.eMessage_snes_cb_input_poll:
						break;
					case eMessage.eMessage_snes_cb_input_state:
						{
							int port = brPipe.ReadInt32();
							int device = brPipe.ReadInt32();
							int index = brPipe.ReadInt32();
							int id = brPipe.ReadInt32();
							ushort ret = 0;
							if (input_state != null)
								ret = input_state(port, device, index, id);
							bwPipe.Write(ret);
							bwPipe.Flush();
							break;
						}
					case eMessage.eMessage_snes_cb_input_notify:
						{
							int index = brPipe.ReadInt32();
							if (input_notify != null)
								input_notify(index);
							break;
						}
					case eMessage.eMessage_snes_cb_audio_sample:
						{
							int nsamples = brPipe.ReadInt32();
							bwPipe.Write(0); //location to store audio buffer in
							bwPipe.Flush();
							brPipe.ReadInt32(); //dummy synchronization

							if (audio_sample != null)
							{
								ushort* audiobuffer = ((ushort*)mmvaPtr);
								for (int i = 0; i < nsamples; )
								{
									ushort left = audiobuffer[i++];
									ushort right = audiobuffer[i++];
									audio_sample(left, right);
								}
							}

							bwPipe.Write(0); //dummy synchronization
							bwPipe.Flush();
							brPipe.ReadInt32();  //dummy synchronization
							break;
						}
					case eMessage.eMessage_snes_cb_scanlineStart:
						{
							int line = brPipe.ReadInt32();
							if (scanlineStart != null)
								scanlineStart(line);

							//we have to notify the unmanaged process that we're done peeking thruogh its memory and whatnot so it can proceed with emulation
							WritePipeMessage(eMessage.eMessage_Complete);
							break;
						}
					case eMessage.eMessage_snes_cb_path_request:
						{
							int slot = brPipe.ReadInt32();
							string hint = ReadPipeString();
							string ret = hint;
							if (pathRequest != null)
								hint = pathRequest(slot, hint);
							WritePipeString(hint);
							break;
						}
					case eMessage.eMessage_snes_cb_trace_callback:
						{
							var trace = ReadPipeString();
							if (traceCallback != null)
								traceCallback(trace);
							break;
						}
					case eMessage.eMessage_snes_allocSharedMemory:
						{
							var smb = new SharedMemoryBlock();
							smb.Name = ReadPipeString();
							smb.Size = brPipe.ReadInt32();
							smb.BlockName = InstanceName + smb.Name;
							smb.Allocate();
							if (SharedMemoryBlocks.ContainsKey(smb.Name))
							{
								throw new InvalidOperationException("Re-defined a shared memory block. Check bsnes init/shutdown code. Block name: " + smb.Name);
							}

							SharedMemoryBlocks[smb.Name] = smb;
							WritePipeString(smb.BlockName);
							break;
						}
					case eMessage.eMessage_snes_freeSharedMemory:
						{
							string name = ReadPipeString();
							var smb = SharedMemoryBlocks[name];
							smb.Dispose();
							SharedMemoryBlocks.Remove(name);
							break;
						}
				}
			}
		} //WaitForCompletion()

		class SharedMemoryBlock : IDisposable
		{
			public string Name;
			public string BlockName;
			public int Size;
			public MemoryMappedFile mmf;
			public MemoryMappedViewAccessor mmva;
			public byte* Ptr;

			public void Allocate()
			{
				mmf = MemoryMappedFile.CreateNew(BlockName, Size);
				mmva = mmf.CreateViewAccessor();
				mmva.SafeMemoryMappedViewHandle.AcquirePointer(ref Ptr);
			}

			public void Dispose()
			{
				if (mmf == null) return;
				mmva.Dispose();
				mmf.Dispose();
				mmf = null;
			}
		}

		Dictionary<string, SharedMemoryBlock> SharedMemoryBlocks = new Dictionary<string, SharedMemoryBlock>();

		snes_video_refresh_t video_refresh;
		snes_input_poll_t input_poll;
		snes_input_state_t input_state;
		snes_input_notify_t input_notify;
		snes_audio_sample_t audio_sample;
		snes_scanlineStart_t scanlineStart;
		snes_path_request_t pathRequest;
		snes_trace_t traceCallback;

		public void snes_set_video_refresh(snes_video_refresh_t video_refresh) { this.video_refresh = video_refresh; }
		public void snes_set_input_poll(snes_input_poll_t input_poll) { this.input_poll = input_poll; }
		public void snes_set_input_state(snes_input_state_t input_state) { this.input_state = input_state; }
		public void snes_set_input_notify(snes_input_notify_t input_notify) { this.input_notify = input_notify; }
		public void snes_set_audio_sample(snes_audio_sample_t audio_sample)
		{
			this.audio_sample = audio_sample;
			WritePipeMessage(eMessage.eMessage_snes_enable_audio);
			bwPipe.Write(audio_sample != null);
			bwPipe.Flush();
		}
		public void snes_set_path_request(snes_path_request_t pathRequest) { this.pathRequest = pathRequest; }
		public void snes_set_scanlineStart(snes_scanlineStart_t scanlineStart)
		{
			this.scanlineStart = scanlineStart;
			WritePipeMessage(eMessage.eMessage_snes_enable_scanline);
			bwPipe.Write(scanlineStart != null);
			bwPipe.Flush();
		}
		public void snes_set_trace_callback(snes_trace_t callback)
		{
			this.traceCallback = callback;
			WritePipeMessage(eMessage.eMessage_snes_enable_trace);
			bwPipe.Write(callback != null);
			bwPipe.Flush();
		}

		public delegate void snes_video_refresh_t(int* data, int width, int height);
		public delegate void snes_input_poll_t();
		public delegate ushort snes_input_state_t(int port, int device, int index, int id);
		public delegate void snes_input_notify_t(int index);
		public delegate void snes_audio_sample_t(ushort left, ushort right);
		public delegate void snes_scanlineStart_t(int line);
		public delegate string snes_path_request_t(int slot, string hint);
		public delegate void snes_trace_t(string msg);

		public enum SNES_REG : int
		{
			//$2105
			BG_MODE = 0,
			BG3_PRIORITY = 1,
			BG1_TILESIZE = 2,
			BG2_TILESIZE = 3,
			BG3_TILESIZE = 4,
			BG4_TILESIZE = 5,
			//$2107
			BG1_SCADDR = 10,
			BG1_SCSIZE = 11,
			//$2108
			BG2_SCADDR = 12,
			BG2_SCSIZE = 13,
			//$2109
			BG3_SCADDR = 14,
			BG3_SCSIZE = 15,
			//$210A
			BG4_SCADDR = 16,
			BG4_SCSIZE = 17,
			//$210B
			BG1_TDADDR = 20,
			BG2_TDADDR = 21,
			//$210C
			BG3_TDADDR = 22,
			BG4_TDADDR = 23,
			//$2133 SETINI
			SETINI_MODE7_EXTBG = 30,
			SETINI_HIRES = 31,
			SETINI_OVERSCAN = 32,
			SETINI_OBJ_INTERLACE = 33,
			SETINI_SCREEN_INTERLACE = 34,
			//$2130 CGWSEL
			CGWSEL_COLORMASK = 40,
			CGWSEL_COLORSUBMASK = 41,
			CGWSEL_ADDSUBMODE = 42,
			CGWSEL_DIRECTCOLOR = 43,
			//$2101 OBSEL
			OBSEL_NAMEBASE = 50,
			OBSEL_NAMESEL = 51,
			OBSEL_SIZE = 52,
			//$2131 CGADSUB
			CGADSUB_MODE = 60,
			CGADSUB_HALF = 61,
			CGADSUB_BG4 = 62,
			CGADSUB_BG3 = 63,
			CGADSUB_BG2 = 64,
			CGADSUB_BG1 = 65,
			CGADSUB_OBJ = 66,
			CGADSUB_BACKDROP = 67,
			//$212C TM
			TM_BG1 = 70,
			TM_BG2 = 71,
			TM_BG3 = 72,
			TM_BG4 = 73,
			TM_OBJ = 74,
			//$212D TM
			TS_BG1 = 80,
			TS_BG2 = 81,
			TS_BG3 = 82,
			TS_BG4 = 83,
			TS_OBJ = 84,
			//Mode7 regs
			M7SEL_REPEAT = 90,
			M7SEL_HFLIP = 91,
			M7SEL_VFLIP = 92,
			M7A = 93,
			M7B = 94,
			M7C = 95,
			M7D = 96,
			M7X = 97,
			M7Y = 98,
			//BG scroll regs
			BG1HOFS = 100,
			BG1VOFS = 101,
			BG2HOFS = 102,
			BG2VOFS = 103,
			BG3HOFS = 104,
			BG3VOFS = 105,
			BG4HOFS = 106,
			BG4VOFS = 107,
			M7HOFS = 108,
			M7VOFS = 109,
		}

		public enum SNES_MEMORY : uint
		{
			CARTRIDGE_RAM = 0,
			CARTRIDGE_RTC = 1,
			BSX_RAM = 2,
			BSX_PRAM = 3,
			SUFAMI_TURBO_A_RAM = 4,
			SUFAMI_TURBO_B_RAM = 5,
			GAME_BOY_RAM = 6,
			GAME_BOY_RTC = 7,

			WRAM = 100,
			APURAM = 101,
			VRAM = 102,
			OAM = 103,
			CGRAM = 104,

			SYSBUS = 200,
			LOGICAL_REGS = 201
		}

		public enum SNES_REGION : byte
		{
			NTSC = 0,
			PAL = 1,
		}

		public enum SNES_DEVICE : uint
		{
			NONE = 0,
			JOYPAD = 1,
			MULTITAP = 2,
			MOUSE = 3,
			SUPER_SCOPE = 4,
			JUSTIFIER = 5,
			JUSTIFIERS = 6,
			SERIAL_CABLE = 7
		}

		public enum SNES_DEVICE_ID : uint
		{
			JOYPAD_B = 0,
			JOYPAD_Y = 1,
			JOYPAD_SELECT = 2,
			JOYPAD_START = 3,
			JOYPAD_UP = 4,
			JOYPAD_DOWN = 5,
			JOYPAD_LEFT = 6,
			JOYPAD_RIGHT = 7,
			JOYPAD_A = 8,
			JOYPAD_X = 9,
			JOYPAD_L = 10,
			JOYPAD_R = 11
		}
	}

}
