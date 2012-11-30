using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace BizHawk.Emulation.Consoles.Nintendo.GBA
{
	public class GBA : IEmulator, IVideoProvider, ISyncSoundProvider
	{
		public static readonly ControllerDefinition GBAController =
		new ControllerDefinition
		{
			Name = "GBA Controller",
			BoolButtons =
			{					
				"Up", "Down", "Left", "Right", "Select", "Start", "B", "A", "L", "R", "Power"
			}
		};
		public ControllerDefinition ControllerDefinition { get { return GBAController; } }
		public IController Controller { get; set; }

		public void Load(byte[] rom, byte[] bios)
		{
			if (bios.Length != 16384)
				throw new Exception("GBA bios must be exactly 16384 bytes!");
			if (rom.Length > 32 * 1024 * 1024)
				throw new Exception("Rom is too big!");
			Init();
			LibMeteor.libmeteor_hardreset();
			LibMeteor.libmeteor_loadbios(bios, (uint)bios.Length);
			LibMeteor.libmeteor_loadrom(rom, (uint)rom.Length);

			SetUpMemoryDomains();
		}

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			Controller.UpdateControls(Frame++);
			IsLagFrame = true;

			if (Controller["Power"])
				LibMeteor.libmeteor_hardreset();
			// due to the design of the tracing api, we have to poll whether it's active each frame
			LibMeteor.libmeteor_settracecallback(CoreInputComm.Tracer.Enabled ? tracecallback : null);
			if (!coredead)
				LibMeteor.libmeteor_frameadvance();
			if (IsLagFrame)
				LagCount++;
			if (EndOfFrameCallback != null)
				EndOfFrameCallback();
		}

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; private set; }
		public string SystemId { get { return "GBA"; } }
		public bool DeterministicEmulation { get { return true; } }

		public void ResetFrameCounter()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		#region saveram

		public byte[] ReadSaveRam()
		{
			if (!LibMeteor.libmeteor_hassaveram())
				return null;
			IntPtr data = IntPtr.Zero;
			uint size = 0;
			if (!LibMeteor.libmeteor_savesaveram(ref data, ref size))
				throw new Exception("libmeteor_savesaveram() returned false!");
			byte[] ret = new byte[size];
			Marshal.Copy(data, ret, 0, (int)size);
			LibMeteor.libmeteor_savesaveram_destroy(data);
			return ret;
		}

		public void StoreSaveRam(byte[] data)
		{
			if (!LibMeteor.libmeteor_loadsaveram(data, (uint)data.Length))
				throw new Exception("libmeteor_loadsaveram() returned false!");
		}

		public void ClearSaveRam()
		{
			LibMeteor.libmeteor_clearsaveram();
		}

		public bool SaveRamModified
		{ get { return LibMeteor.libmeteor_hassaveram(); } set { } }

		#endregion

		#region savestates

		byte[] SaveCoreBinary()
		{
			IntPtr ndata = IntPtr.Zero;
			uint nsize = 0;
			if (!LibMeteor.libmeteor_savestate(ref ndata, ref nsize))
				throw new Exception("libmeteor_savestate() failed!");
			if (ndata == IntPtr.Zero || nsize == 0)
				throw new Exception("libmeteor_savestate() returned bad!");

			byte[] ret = new byte[nsize];
			Marshal.Copy(ndata, ret, 0, (int)nsize);
			LibMeteor.libmeteor_savestate_destroy(ndata);
			return ret;
		}

		void LoadCoreBinary(byte[] data)
		{
			if (!LibMeteor.libmeteor_loadstate(data, (uint)data.Length))
				throw new Exception("libmeteor_loadstate() failed!");
		}

		public void SaveStateText(System.IO.TextWriter writer)
		{
			var temp = SaveStateBinary();
			temp.SaveAsHex(writer);
			// write extra copy of stuff we don't use
			writer.WriteLine("Frame {0}", Frame);
		}

		public void LoadStateText(System.IO.TextReader reader)
		{
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			state.ReadFromHex(hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
		}

		public void SaveStateBinary(System.IO.BinaryWriter writer)
		{
			byte[] data = SaveCoreBinary();
			writer.Write(data.Length);
			writer.Write(data);
			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(System.IO.BinaryReader reader)
		{
			int length = reader.ReadInt32();
			byte[] data = reader.ReadBytes(length);
			LoadCoreBinary(data);
			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		#endregion

		public CoreInputComm CoreInputComm { get; set; }

		CoreOutputComm _CoreOutputComm = new CoreOutputComm
		{
			VsyncNum = 262144,
			VsyncDen = 4389,
			CpuTraceAvailable = true,
			TraceHeader = "   -Addr--- -Opcode- -Instruction------------------- -R0----- -R1----- -R2----- -R3----- -R4----- -R5----- -R6----- -R7----- -R8----- -R9----- -R10---- -R11---- -R12---- -R13(SP) -R14(LR) -R15(PC) -CPSR--- -SPSR---",
			NominalWidth = 240,
			NominalHeight = 160
		};

		public CoreOutputComm CoreOutputComm { get { return _CoreOutputComm; } }

		#region memorydomains

		List<MemoryDomain> _MemoryDomains = new List<MemoryDomain>();
		public IList<MemoryDomain> MemoryDomains { get { return _MemoryDomains; } }
		public MemoryDomain MainMemory
		{
			// some core tools assume MainMemory == MemoryDomains[0], so do that anyway
			get { return MemoryDomains[0]; }
		}

		void AddMemoryDomain(LibMeteor.MemoryArea which, int size, string name)
		{
			IntPtr data = LibMeteor.libmeteor_getmemoryarea(which);
			if (data == IntPtr.Zero)
				throw new Exception("libmeteor_getmemoryarea() returned NULL??");

			MemoryDomain md = new MemoryDomain(name, size, Endian.Little,
				delegate(int addr)
				{
					unsafe
					{
						byte* d = (byte*)data;
						if (addr < 0 || addr >= size)
							throw new IndexOutOfRangeException();
						return d[addr];
					}
				},
				delegate(int addr, byte val)
				{
					unsafe
					{
						byte* d = (byte*)data;
						if (addr < 0 || addr >= size)
							throw new IndexOutOfRangeException();
						d[addr] = val;
					}
				});
			_MemoryDomains.Add(md);
		}

		void SetUpMemoryDomains()
		{
			_MemoryDomains.Clear();
			// this must be first to coincide with "main memory"
			// note that ewram could also be considered main memory depending on which hairs you split
			AddMemoryDomain(LibMeteor.MemoryArea.iwram, 32 * 1024, "IWRAM");
			AddMemoryDomain(LibMeteor.MemoryArea.ewram, 256 * 1024, "EWRAM");
			AddMemoryDomain(LibMeteor.MemoryArea.bios, 16 * 1024, "BIOS");
			AddMemoryDomain(LibMeteor.MemoryArea.palram, 1024, "PALRAM");
			AddMemoryDomain(LibMeteor.MemoryArea.vram, 96 * 1024, "VRAM");
			AddMemoryDomain(LibMeteor.MemoryArea.oam, 1024, "OAM");
			// even if the rom is less than 32MB, the whole is still valid in meteor
			AddMemoryDomain(LibMeteor.MemoryArea.rom, 32 * 1024 * 1024, "ROM");
			// special domain for system bus
			MemoryDomain sb = new MemoryDomain("BUS", 1 << 28, Endian.Little,
				delegate(int addr)
				{
					return LibMeteor.libmeteor_peekbus((uint)addr);
				},
				delegate(int addr, byte val)
				{
					LibMeteor.libmeteor_writebus((uint)addr, val);
				});
			_MemoryDomains.Add(sb);
		}

		public void GetGPUMemoryAreas(out IntPtr vram, out IntPtr palram, out IntPtr oam, out IntPtr mmio)
		{
			IntPtr _vram = LibMeteor.libmeteor_getmemoryarea(LibMeteor.MemoryArea.vram);
			IntPtr _palram = LibMeteor.libmeteor_getmemoryarea(LibMeteor.MemoryArea.palram);
			IntPtr _oam = LibMeteor.libmeteor_getmemoryarea(LibMeteor.MemoryArea.oam);
			IntPtr _mmio = LibMeteor.libmeteor_getmemoryarea(LibMeteor.MemoryArea.io);

			if (_vram == IntPtr.Zero || _palram == IntPtr.Zero || _oam == IntPtr.Zero || _mmio == IntPtr.Zero)
				throw new Exception("libmeteor_getmemoryarea() failed!");

			vram = _vram;
			palram = _palram;
			oam = _oam;
			mmio = _mmio;
		}

		#endregion

		/// <summary>like libsnes, the library is single-instance</summary>
		static GBA attachedcore;
		/// <summary>hold pointer to message callback so it won't get GCed</summary>
		LibMeteor.MessageCallback messagecallback;
		/// <summary>hold pointer to input callback so it won't get GCed</summary>
		LibMeteor.InputCallback inputcallback;
		/// <summary>true if libmeteor aborted</summary>
		bool coredead = false;
		/// <summary>hold pointer to trace callback so it won't get GCed</summary>
		LibMeteor.TraceCallback tracecallback;

		LibMeteor.Buttons GetInput()
		{
			// libmeteor bitflips everything itself, so 0 == off, 1 == on
			IsLagFrame = false;
			LibMeteor.Buttons ret = 0;
			if (Controller["Up"]) ret |= LibMeteor.Buttons.BTN_UP;
			if (Controller["Down"]) ret |= LibMeteor.Buttons.BTN_DOWN;
			if (Controller["Left"]) ret |= LibMeteor.Buttons.BTN_LEFT;
			if (Controller["Right"]) ret |= LibMeteor.Buttons.BTN_RIGHT;
			if (Controller["Select"]) ret |= LibMeteor.Buttons.BTN_SELECT;
			if (Controller["Start"]) ret |= LibMeteor.Buttons.BTN_START;
			if (Controller["B"]) ret |= LibMeteor.Buttons.BTN_B;
			if (Controller["A"]) ret |= LibMeteor.Buttons.BTN_A;
			if (Controller["L"]) ret |= LibMeteor.Buttons.BTN_L;
			if (Controller["R"]) ret |= LibMeteor.Buttons.BTN_R;
			return ret;
		}

		#region messagecallbacks

		void PrintMessage(string msg, bool abort)
		{
			Console.Write(msg.Replace("\n", "\r\n"));
			if (abort)
				StopCore(msg);
		}

		void StopCore(string msg)
		{
			coredead = true;
			Console.WriteLine("Core stopped.");
			for (int i = 0; i < soundbuffer.Length; i++)
				soundbuffer[i] = 0;

			var gz = new System.IO.Compression.GZipStream(
				new MemoryStream(Convert.FromBase64String(dispfont), false),
				System.IO.Compression.CompressionMode.Decompress);
			byte[] font = new byte[2048];
			gz.Read(font, 0, 2048);
			gz.Dispose();
			
			// cores aren't supposed to have bad dependencies like System.Drawing, right?

			int scx = 0;
			int scy = 0;

			foreach (char c in msg)
			{
				if (scx == 240 || c == '\n')
				{
					scy += 8;
					scx = 0;
				}
				if (scy == 160)
					break;
				if (c == '\r' || c == '\n')
					continue;
				if (c < 256 && c != ' ')
				{
					int fpos = c * 8;
					for (int j = 0; j < 8; j++)
					{
						for (int i = 0; i < 8; i++)
						{
							if ((font[fpos] >> i & 1) != 0)
								videobuffer[(scy + j) * 240 + scx + i] = unchecked((int)0xffff0000);
							else
								videobuffer[(scy + j) * 240 + scx + i] = unchecked((int)0xff000000);
						}
						fpos++;
					}
				}
				scx += 8;
			}
		}

		const string dispfont =
		"H4sICAInrFACAGZvby5yYXcARVU9q9RAFL2gjK8IT+0GDGoh1oGFGHDYQvwL2hoQroXhsdUqGGbxZ/gD" +
		"bKys7BRhIZVYLgurIghvG3ksCPKKJfGcm1nfSTJn750792smWUmIr9++/vjmdYzDZlhuh1guFotpfiRH" +
		"+dQ4n+aLxfOj/MgUR7mID8GLDMN2CftBgj54oEGG5ZuPH98sh93P3afJZHIzqGrw0e+/7LPs+OqVvuu7" +
		"7vTZJb8J223Y+MtZHvLsstwuqlAVt+E1eh+DV0JU+s3mx3q9luCChjoIsVgI7Wg2kAHBQ1mkqPu6EBhk" +
		"feYFcM5F0B0d9A74WtX2QvRtdU0SrBp6kaZpKIJ7XI341oV66sVp4TOtJS/L/IN+k8pnQkCbZb4QPEVB" +
		"nYYhKB16JHZwbsZRDuBEDWsnEnQeTzSIz60CyHWV6cg19LOXjfb1TqKb1pSrzE0VHBUOvIed8ia3dZGb" +
		"c96JM0ZhfgzPBPCbkWEPEs/4j+fO1kd2HM55Q0bf4PdmCW15E/HdFI1M7Dg/Z1xN64InguxqpGn6kkvF" +
		"GaJ0Z32/6jrRkxjntFciMB79mTwPM5NLm0ffWac3iCb7kbx0XbfqzzqhEGBPLe2i9TVKmxGtiGPFIm1N" +
		"tNj+ppMLDDl7Ywh1q62gPEUKlJX1Yw3k1uTo2P9sCseQW3Y80B4QLznrNwaOnbMGUDK9SNOvVgxt9vQH" +
		"gj51IPn7SdlRFDt4MoarIGvKwyoFd6tV34CtAWTLRySiAZF5Oq5DcHvyAvuO8/FtLgZrRNcf9tlp8l/4" +
		"sc64HPuhMnLmR/Z3jA/9cbAzexVj2CU59SPYD+rJyU6VfsiIh5NtL+j+/b7cyzmOu+op1wXrjzHXG2Ow" +
		"Qikba6pbgwL0P7J4y89XDRsY7ZxEXLcmkydP/zz9NVv74P2N4yLVVaT8wIxDNv9NaRtG1pM5kinLVqKY" +
		"ERndzXhOgOicGNe1yPLp5NUXnezAm99//3ymoX0xodQvsMKoE5GH18fr3aPx+v5ivPwFbt1KIx9VffYM" +
		"g30GyUkPbV1zJgGzJpt+sWAxGEWSHwH4izg/hwAeBjEMw0GPweTDfNLyUWzSqdroXN+L9L1z1Gy3tsKe" +
		"7Zbzpj/oOE+9P8iq5j/Nj/HUQK+S4omkuMJIaqD3g5+xQ2KwvIcEKshXE3YJNkfgjbg7/8YNLbV0Lqo6" +
		"AFEaQqJmPlM7n+l9VeDHJTm57wGJPtjRwhg53+LD1DRnMvNFO9q3q9WqFfncnq6+tm7mszbzM4QziERe" +
		"h7+LyO+zz8AYfQGerdf+P27cOBYaeUubt1RNU138q4wg74qiuFeGKjQA5BwOgxABACX8A6+GHm0ACAAA";

		#endregion

		void Trace(string msg)
		{
			CoreInputComm.Tracer.Put(msg);
		}

		Action EndOfFrameCallback = null;
		LibMeteor.ScanlineCallback scanlinecb = null;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="callback">null to cancel</param>
		/// <param name="scanline">0-227, null = end of frame</param>
		public void SetScanlineCallback(Action callback, int? scanline)
		{
			if (callback == null)
			{
				LibMeteor.libmeteor_setscanlinecallback(null, 400);
				EndOfFrameCallback = null;
				scanlinecb = null;
			}
			else if (scanline == null)
			{
				LibMeteor.libmeteor_setscanlinecallback(null, 400);
				EndOfFrameCallback = callback;
				scanlinecb = null;
			}
			else if (scanline >= 0 && scanline <= 227)
			{
				scanlinecb = new LibMeteor.ScanlineCallback(callback);
				LibMeteor.libmeteor_setscanlinecallback(scanlinecb, (int)scanline);
				EndOfFrameCallback = null;
			}
			else
			{
				throw new ArgumentOutOfRangeException("Scanline must be in [0, 227]!");
			}
		}

		void Init()
		{
			if (attachedcore != null)
				attachedcore.Dispose();

			messagecallback = PrintMessage;
			inputcallback = GetInput;
			tracecallback = Trace; // don't set this callback now, only set if enabled
			LibMeteor.libmeteor_setmessagecallback(messagecallback);
			LibMeteor.libmeteor_setkeycallback(inputcallback);

			LibMeteor.libmeteor_init();
			videobuffer = new int[240 * 160];
			videohandle = GCHandle.Alloc(videobuffer, GCHandleType.Pinned);
			soundbuffer = new short[2048]; // nominal length of one frame is something like 1480 shorts?
			soundhandle = GCHandle.Alloc(soundbuffer, GCHandleType.Pinned);

			if (!LibMeteor.libmeteor_setbuffers
				(videohandle.AddrOfPinnedObject(), (uint)(sizeof(int) * videobuffer.Length),
				soundhandle.AddrOfPinnedObject(), (uint)(sizeof(short) * soundbuffer.Length)))
				throw new Exception("libmeteor_setbuffers() returned false??");

			attachedcore = this;
		}

		bool disposed = false;
		public void Dispose()
		{
			if (!disposed)
			{
				disposed = true;
				videohandle.Free();
				soundhandle.Free();
				// guarantee crash if it gets accessed
				LibMeteor.libmeteor_setbuffers(IntPtr.Zero, 240 * 160 * 4, IntPtr.Zero, 4);
				messagecallback = null;
				inputcallback = null;
				tracecallback = null;
				LibMeteor.libmeteor_setmessagecallback(messagecallback);
				LibMeteor.libmeteor_setkeycallback(inputcallback);
				LibMeteor.libmeteor_settracecallback(tracecallback);
				_MemoryDomains.Clear();
			}
		}

		#region IVideoProvider

		public IVideoProvider VideoProvider { get { return this; } }

		int[] videobuffer;
		GCHandle videohandle;

		public int[] GetVideoBuffer() { return videobuffer; }
		public int VirtualWidth { get { return 240; } }
		public int BufferWidth { get { return 240; } }
		public int BufferHeight { get { return 160; } }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		#endregion

		#region ISoundProvider

		short[] soundbuffer;
		GCHandle soundhandle;

		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return this; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		public void GetSamples(out short[] samples, out int nsamp)
		{
			uint nbytes = LibMeteor.libmeteor_emptysound();
			samples = soundbuffer;
			if (!coredead)
				nsamp = (int)(nbytes / 4);
			else
				nsamp = 738;
		}

		public void DiscardSamples()
		{
			LibMeteor.libmeteor_emptysound();
		}

		#endregion
	}
}
