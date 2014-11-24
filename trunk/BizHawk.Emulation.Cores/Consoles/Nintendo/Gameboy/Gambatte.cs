using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Common;

using Newtonsoft.Json;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	/// <summary>
	/// a gameboy/gameboy color emulator wrapped around native C++ libgambatte
	/// </summary>
	[CoreAttributes(
		"Gambatte",
		"",
		isPorted: true,
		isReleased: true,
		portedVersion: "SVN 344",
		portedUrl: "http://gambatte.sourceforge.net/"
		)]
	public class Gameboy : IEmulator, IVideoProvider, ISyncSoundProvider,
		IMemoryDomains, IDebuggable, ISettable<Gameboy.GambatteSettings, Gameboy.GambatteSyncSettings>
	{
		#region ALL SAVESTATEABLE STATE GOES HERE

		/// <summary>
		/// internal gambatte state
		/// </summary>
		internal IntPtr GambatteState = IntPtr.Zero;

		public int Frame { get; set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; private set; }

		// all cycle counts are relative to a 2*1024*1024 mhz refclock

		/// <summary>
		/// total cycles actually executed
		/// </summary>
		private ulong _cycleCount = 0;

		/// <summary>
		/// number of extra cycles we overran in the last frame
		/// </summary>
		private uint frameOverflow = 0;
		public ulong CycleCount { get { return _cycleCount; } }

		#endregion

		/// <summary>
		/// the nominal length of one frame
		/// </summary>
		private const uint TICKSINFRAME = 35112;

		/// <summary>
		/// number of ticks per second
		/// </summary>
		private const uint TICKSPERSECOND = 2097152;

		/// <summary>
		/// keep a copy of the input callback delegate so it doesn't get GCed
		/// </summary>
		LibGambatte.InputGetter InputCallback;

		/// <summary>
		/// whatever keys are currently depressed
		/// </summary>
		LibGambatte.Buttons CurrentButtons = 0;

		#region RTC

		/// <summary>
		/// RTC time when emulation begins.
		/// </summary>
		uint zerotime = 0;

		/// <summary>
		/// if true, RTC will run off of real elapsed time
		/// </summary>
		bool real_rtc_time = false;

		LibGambatte.RTCCallback TimeCallback;

		static long GetUnixNow()
		{
			// because internally the RTC works off of relative time, we don't need to base
			// this off of any particular canonical epoch.
			return DateTime.UtcNow.Ticks / 10000000L - 60000000000L;
		}

		uint GetCurrentTime()
		{
			if (real_rtc_time)
			{
				return (uint)GetUnixNow();
			}
			else
			{
				ulong fn = (ulong)Frame;
				// as we're exactly tracking cpu cycles, this can be pretty accurate
				fn *= 4389;
				fn /= 262144;
				fn += zerotime;
				return (uint)fn;
			}
		}

		uint GetInitialTime()
		{
			if (real_rtc_time)
				return (uint)GetUnixNow();
			else
				// setting the initial boot time to 0 will cause our zerotime
				// to function as an initial offset, which is what we want
				return 0;
		}

		#endregion

		[CoreConstructor("GB", "GBC")]
		public Gameboy(CoreComm comm, GameInfo game, byte[] file, object Settings, object SyncSettings, bool deterministic)
		{
			CoreComm = comm;

			comm.VsyncNum = 262144;
			comm.VsyncDen = 4389;
			comm.RomStatusAnnotation = null;
			comm.RomStatusDetails = null;
			comm.CpuTraceAvailable = true;
			comm.NominalWidth = 160;
			comm.NominalHeight = 144;

			ThrowExceptionForBadRom(file);
			BoardName = MapperName(file);

			DeterministicEmulation = deterministic;

			GambatteState = LibGambatte.gambatte_create();

			if (GambatteState == IntPtr.Zero)
				throw new InvalidOperationException("gambatte_create() returned null???");

			try
			{
				this._SyncSettings = (GambatteSyncSettings)SyncSettings ?? new GambatteSyncSettings();
				// copy over non-loadflag syncsettings now; they won't take effect if changed later
				zerotime = (uint)this._SyncSettings.RTCInitialTime;
				real_rtc_time = DeterministicEmulation ? false : this._SyncSettings.RealTimeRTC;

				LibGambatte.LoadFlags flags = 0;

				if (this._SyncSettings.ForceDMG)
					flags |= LibGambatte.LoadFlags.FORCE_DMG;
				if (this._SyncSettings.GBACGB)
					flags |= LibGambatte.LoadFlags.GBA_CGB;
				if (this._SyncSettings.MulticartCompat)
					flags |= LibGambatte.LoadFlags.MULTICART_COMPAT;

				if (LibGambatte.gambatte_load(GambatteState, file, (uint)file.Length, GetCurrentTime(), flags) != 0)
					throw new InvalidOperationException("gambatte_load() returned non-zero (is this not a gb or gbc rom?)");

				// set real default colors (before anyone mucks with them at all)
				PutSettings((GambatteSettings)Settings ?? new GambatteSettings());

				InitSound();

				Frame = 0;
				LagCount = 0;
				IsLagFrame = false;

				InputCallback = new LibGambatte.InputGetter(ControllerCallback);

				LibGambatte.gambatte_setinputgetter(GambatteState, InputCallback);

				InitMemoryDomains();

				CoreComm.RomStatusDetails = string.Format("{0}\r\nSHA1:{1}\r\nMD5:{2}\r\n",
					game.Name,
					file.HashSHA1(),
					file.HashMD5());

				{
					byte[] buff = new byte[32];
					LibGambatte.gambatte_romtitle(GambatteState, buff);
					string romname = System.Text.Encoding.ASCII.GetString(buff);
					Console.WriteLine("Core reported rom name: {0}", romname);
				}

				TimeCallback = new LibGambatte.RTCCallback(GetCurrentTime);
				LibGambatte.gambatte_setrtccallback(GambatteState, TimeCallback);

				NewSaveCoreSetBuff();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		#region controller

		public static readonly ControllerDefinition GbController = new ControllerDefinition
		{
			Name = "Gameboy Controller",
			BoolButtons =
			{
				"Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "Power"
			}
		};

		public ControllerDefinition ControllerDefinition
		{
			get { return GbController; }
		}

		public IController Controller { get; set; }

		LibGambatte.Buttons ControllerCallback()
		{
			CoreComm.InputCallback.Call();
			IsLagFrame = false;
			return CurrentButtons;
		}

		#endregion

		#region debug

		public IDictionary<string, int> GetCpuFlagsAndRegisters()
		{
			int[] data = new int[10];
			LibGambatte.gambatte_getregs(GambatteState, data);

			return new Dictionary<string, int>
			{
				{ "PC", data[(int)LibGambatte.RegIndicies.PC] & 0xffff },
				{ "SP", data[(int)LibGambatte.RegIndicies.SP] & 0xffff },
				{ "A", data[(int)LibGambatte.RegIndicies.A] & 0xff },
				{ "B", data[(int)LibGambatte.RegIndicies.B] & 0xff },
				{ "C", data[(int)LibGambatte.RegIndicies.C] & 0xff },
				{ "D", data[(int)LibGambatte.RegIndicies.D] & 0xff },
				{ "E", data[(int)LibGambatte.RegIndicies.E] & 0xff },
				{ "F", data[(int)LibGambatte.RegIndicies.F] & 0xff },
				{ "H", data[(int)LibGambatte.RegIndicies.H] & 0xff },
				{ "L", data[(int)LibGambatte.RegIndicies.L] & 0xff }
			};
		}

		[FeatureNotImplemented]
		public void SetCpuRegister(string register, int value)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// true if the emulator is currently emulating CGB
		/// </summary>
		/// <returns></returns>
		public bool IsCGBMode()
		{
			return (LibGambatte.gambatte_iscgb(GambatteState));
		}

		#endregion

		internal void FrameAdvancePrep()
		{
			Frame++;

			// update our local copy of the controller data
			CurrentButtons = 0;

			if (Controller["Up"])
				CurrentButtons |= LibGambatte.Buttons.UP;
			if (Controller["Down"])
				CurrentButtons |= LibGambatte.Buttons.DOWN;
			if (Controller["Left"])
				CurrentButtons |= LibGambatte.Buttons.LEFT;
			if (Controller["Right"])
				CurrentButtons |= LibGambatte.Buttons.RIGHT;
			if (Controller["A"])
				CurrentButtons |= LibGambatte.Buttons.A;
			if (Controller["B"])
				CurrentButtons |= LibGambatte.Buttons.B;
			if (Controller["Select"])
				CurrentButtons |= LibGambatte.Buttons.SELECT;
			if (Controller["Start"])
				CurrentButtons |= LibGambatte.Buttons.START;

			// the controller callback will set this to false if it actually gets called during the frame
			IsLagFrame = true;

			if (Controller["Power"])
				LibGambatte.gambatte_reset(GambatteState, GetCurrentTime());

			RefreshMemoryCallbacks();
			if (CoreComm.Tracer.Enabled)
				tracecb = MakeTrace;
			else
				tracecb = null;
			LibGambatte.gambatte_settracecallback(GambatteState, tracecb);
		}

		internal void FrameAdvancePost()
		{
			if (IsLagFrame)
				LagCount++;

			if (endofframecallback != null)
				endofframecallback(LibGambatte.gambatte_cpuread(GambatteState, 0xff40));
		}

		public void FrameAdvance(bool render, bool rendersound)
		{
			FrameAdvancePrep();
			if (_SyncSettings.EqualLengthFrames)
			{
				while (true)
				{
					// target number of samples to emit: length of 1 frame minus whatever overflow
					uint samplesEmitted = TICKSINFRAME - frameOverflow;
					System.Diagnostics.Debug.Assert(samplesEmitted * 2 <= soundbuff.Length);
					if (LibGambatte.gambatte_runfor(GambatteState, soundbuff, ref samplesEmitted) > 0)
						LibGambatte.gambatte_blitto(GambatteState, VideoBuffer, 160);

					// account for actual number of samples emitted
					_cycleCount += (ulong)samplesEmitted;
					frameOverflow += samplesEmitted;

					if (rendersound && !Muted)
					{
						ProcessSound((int)samplesEmitted);
					}

					if (frameOverflow >= TICKSINFRAME)
					{
						frameOverflow -= TICKSINFRAME;
						break;
					}
				}
			}
			else
			{
				// target number of samples to emit: always 59.7fps
				// runfor() always ends after creating a video frame, so sync-up is guaranteed
				// when the display has been off, some frames can be markedly shorter than expected
				uint samplesEmitted = TICKSINFRAME;
				if (LibGambatte.gambatte_runfor(GambatteState, soundbuff, ref samplesEmitted) > 0)
					LibGambatte.gambatte_blitto(GambatteState, VideoBuffer, 160);

				_cycleCount += (ulong)samplesEmitted;
				frameOverflow = 0;
				if (rendersound && !Muted)
				{
					ProcessSound((int)samplesEmitted);
				}
			}

			if (rendersound && !Muted)
				ProcessSoundEnd();

			FrameAdvancePost();
		}

		static string MapperName(byte[] romdata)
		{
			switch (romdata[0x147])
			{
				case 0x00: return "Plain ROM"; // = PLAIN; break;
				case 0x01: return "MBC1 ROM"; // = MBC1; break;
				case 0x02: return "MBC1 ROM+RAM"; // = MBC1; break;
				case 0x03: return "MBC1 ROM+RAM+BATTERY"; // = MBC1; break;
				case 0x05: return "MBC2 ROM"; // = MBC2; break;
				case 0x06: return "MBC2 ROM+BATTERY"; // = MBC2; break;
				case 0x08: return "Plain ROM+RAM"; // = PLAIN; break;
				case 0x09: return "Plain ROM+RAM+BATTERY"; // = PLAIN; break;
				case 0x0F: return "MBC3 ROM+TIMER+BATTERY"; // = MBC3; break;
				case 0x10: return "MBC3 ROM+TIMER+RAM+BATTERY"; // = MBC3; break;
				case 0x11: return "MBC3 ROM"; // = MBC3; break;
				case 0x12: return "MBC3 ROM+RAM"; // = MBC3; break;
				case 0x13: return "MBC3 ROM+RAM+BATTERY"; // = MBC3; break;
				case 0x19: return "MBC5 ROM"; // = MBC5; break;
				case 0x1A: return "MBC5 ROM+RAM"; // = MBC5; break;
				case 0x1B: return "MBC5 ROM+RAM+BATTERY"; // = MBC5; break;
				case 0x1C: return "MBC5 ROM+RUMBLE"; // = MBC5; break;
				case 0x1D: return "MBC5 ROM+RUMBLE+RAM"; // = MBC5; break;
				case 0x1E: return "MBC5 ROM+RUMBLE+RAM+BATTERY"; // = MBC5; break;
				case 0xFF: return "HuC1 ROM+RAM+BATTERY"; // = HUC1; break;
				default: return "UNKNOWN";
			}
		}

		/// <summary>
		/// throw exception with intelligible message on some kinds of bad rom
		/// </summary>
		/// <param name="romdata"></param>
		static void ThrowExceptionForBadRom(byte[] romdata)
		{
			if (romdata.Length < 0x148)
				throw new ArgumentException("ROM is far too small to be a valid GB\\GBC rom!");

			switch (romdata[0x147])
			{
				case 0x00: break;
				case 0x01: break;
				case 0x02: break;
				case 0x03: break;
				case 0x05: break;
				case 0x06: break;
				case 0x08: break;
				case 0x09: break;

				case 0x0b: throw new UnsupportedGameException("\"MM01\" Mapper not supported!");
				case 0x0c: throw new UnsupportedGameException("\"MM01\" Mapper not supported!");
				case 0x0d: throw new UnsupportedGameException("\"MM01\" Mapper not supported!");

				case 0x0f: break;
				case 0x10: break;
				case 0x11: break;
				case 0x12: break;
				case 0x13: break;

				case 0x15: throw new UnsupportedGameException("\"MBC4\" Mapper not supported!");
				case 0x16: throw new UnsupportedGameException("\"MBC4\" Mapper not supported!");
				case 0x17: throw new UnsupportedGameException("\"MBC4\" Mapper not supported!");

				case 0x19: break;
				case 0x1a: break;
				case 0x1b: break;
				case 0x1c: break; // rumble
				case 0x1d: break; // rumble
				case 0x1e: break; // rumble

				case 0x20: throw new UnsupportedGameException("\"MBC6\" Mapper not supported!");
				case 0x22: throw new UnsupportedGameException("\"MBC7\" Mapper not supported!");

				case 0xfc: throw new UnsupportedGameException("\"Pocket Camera\" Mapper not supported!");
				case 0xfd: throw new UnsupportedGameException("\"Bandai TAMA5\" Mapper not supported!");
				case 0xfe: throw new UnsupportedGameException("\"HuC3\" Mapper not supported!");
				case 0xff: break;
				default: throw new UnsupportedGameException(string.Format("Unknown mapper: {0:x2}", romdata[0x147]));
			}
			return;
		}

		public string SystemId { get { return "GB"; } }

		public string BoardName { get; private set; }

		public bool DeterministicEmulation { get; private set; }

		#region saveram

		public byte[] CloneSaveRam()
		{
			int length = LibGambatte.gambatte_savesavedatalength(GambatteState);

			if (length > 0)
			{
				byte[] ret = new byte[length];
				LibGambatte.gambatte_savesavedata(GambatteState, ret);
				return ret;
			}
			else
				return new byte[0];
		}

		public void StoreSaveRam(byte[] data)
		{
			if (data.Length != LibGambatte.gambatte_savesavedatalength(GambatteState))
				throw new ArgumentException("Size of saveram data does not match expected!");
			LibGambatte.gambatte_loadsavedata(GambatteState, data);
		}

		/// <summary>
		/// reset cart save ram, if any, to initial state
		/// </summary>
		public void ClearSaveRam()
		{
			int length = LibGambatte.gambatte_savesavedatalength(GambatteState);
			if (length == 0)
				return;

			byte[] clear = new byte[length];
			for (int i = 0; i < clear.Length; i++)
				clear[i] = 0xff; // this exactly matches what gambatte core does

			StoreSaveRam(clear);
		}


		public bool SaveRamModified
		{
			get
			{
				if (LibGambatte.gambatte_savesavedatalength(GambatteState) == 0)
					return false;
				else
					return true; // need to wire more stuff into the core to actually know this
			}
			set { }
		}

		#endregion

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
			// reset frame counters is meant to "re-zero" emulation time wherever it was
			// so these should be reset as well
			_cycleCount = 0;
			frameOverflow = 0;
		}

		#region savestates

		byte[] savebuff;
		byte[] savebuff2;

		void NewSaveCoreSetBuff()
		{
			savebuff = new byte[LibGambatte.gambatte_newstatelen(GambatteState)];
			savebuff2 = new byte[savebuff.Length + 4 + 21];
		}

		JsonSerializer ser = new JsonSerializer { Formatting = Formatting.Indented };

		// other data in the text state besides core
		internal class TextStateData
		{
			public int Frame;
			public int LagCount;
			public bool IsLagFrame;
			public ulong _cycleCount;
			public uint frameOverflow;
		}

		internal TextState<TextStateData> SaveState()
		{
			var s = new TextState<TextStateData>();
			s.Prepare();
			var ff = s.GetFunctionPointersSave();
			LibGambatte.gambatte_newstatesave_ex(GambatteState, ref ff);
			s.ExtraData.IsLagFrame = IsLagFrame;
			s.ExtraData.LagCount = LagCount;
			s.ExtraData.Frame = Frame;
			s.ExtraData.frameOverflow = frameOverflow;
			s.ExtraData._cycleCount = _cycleCount;
			return s;
		}

		internal void LoadState(TextState<TextStateData> s)
		{
			s.Prepare();
			var ff = s.GetFunctionPointersLoad();
			LibGambatte.gambatte_newstateload_ex(GambatteState, ref ff);
			IsLagFrame = s.ExtraData.IsLagFrame;
			LagCount = s.ExtraData.LagCount;
			Frame = s.ExtraData.Frame;
			frameOverflow = s.ExtraData.frameOverflow;
			_cycleCount = s.ExtraData._cycleCount;
		}

		public void SaveStateText(System.IO.TextWriter writer)
		{
			var s = SaveState();
			ser.Serialize(writer, s);
			// write extra copy of stuff we don't use
			writer.WriteLine();
			writer.WriteLine("Frame {0}", Frame);
		}

		public void LoadStateText(System.IO.TextReader reader)
		{
			var s = (TextState<TextStateData>)ser.Deserialize(reader, typeof(TextState<TextStateData>));
			LoadState(s);
		}

		public void SaveStateBinary(System.IO.BinaryWriter writer)
		{
			if (!LibGambatte.gambatte_newstatesave(GambatteState, savebuff, savebuff.Length))
				throw new Exception("gambatte_newstatesave() returned false");

			writer.Write(savebuff.Length);
			writer.Write(savebuff);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
			writer.Write(frameOverflow);
			writer.Write(_cycleCount);
		}

		public void LoadStateBinary(System.IO.BinaryReader reader)
		{
			int length = reader.ReadInt32();
			if (length != savebuff.Length)
				throw new InvalidOperationException("Savestate buffer size mismatch!");

			reader.Read(savebuff, 0, savebuff.Length);

			if (!LibGambatte.gambatte_newstateload(GambatteState, savebuff, savebuff.Length))
				throw new Exception("gambatte_newstateload() returned false");

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
			frameOverflow = reader.ReadUInt32();
			_cycleCount = reader.ReadUInt64();
		}

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream(savebuff2);
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			if (ms.Position != savebuff2.Length)
				throw new InvalidOperationException();
			ms.Close();
			return savebuff2;
		}

		public bool BinarySaveStatesPreferred { get { return true; } }

		#endregion

		#region memorycallback

		LibGambatte.MemoryCallback readcb;
		LibGambatte.MemoryCallback writecb;
		LibGambatte.MemoryCallback execcb;

		void RefreshMemoryCallbacks()
		{
			var mcs = CoreComm.MemoryCallbackSystem;

			// we RefreshMemoryCallbacks() after the triggers in case the trigger turns itself off at that point

			if (mcs.HasReads)
				readcb = delegate(uint addr) { mcs.CallRead(addr); RefreshMemoryCallbacks(); };
			else
				readcb = null;
			if (mcs.HasWrites)
				writecb = delegate(uint addr) { mcs.CallWrite(addr); RefreshMemoryCallbacks(); };
			else
				writecb = null;
			if (mcs.HasExecutes)
				execcb = delegate(uint addr) { mcs.CallExecute(addr); RefreshMemoryCallbacks(); };
			else
				execcb = null;

			LibGambatte.gambatte_setreadcallback(GambatteState, readcb);
			LibGambatte.gambatte_setwritecallback(GambatteState, writecb);
			LibGambatte.gambatte_setexeccallback(GambatteState, execcb);
		}

		#endregion

		public CoreComm CoreComm { get; set; }

		LibGambatte.TraceCallback tracecb;

		void MakeTrace(IntPtr _s)
		{
			int[] s = new int[13];
			System.Runtime.InteropServices.Marshal.Copy(_s, s, 0, 13);
			ushort unused;

			CoreComm.Tracer.Put(string.Format(
				"{13} SP:{2:x2} A:{3:x2} B:{4:x2} C:{5:x2} D:{6:x2} E:{7:x2} F:{8:x2} H:{9:x2} L:{10:x2} {11} Cy:{0}",
				s[0],
				s[1] & 0xffff,
				s[2] & 0xffff,
				s[3] & 0xff,
				s[4] & 0xff,
				s[5] & 0xff,
				s[6] & 0xff,
				s[7] & 0xff,
				s[8] & 0xff,
				s[9] & 0xff,
				s[10] & 0xff,
				s[11] != 0 ? "skip" : "",
				s[12] & 0xff,
				Common.Components.Z80GB.NewDisassembler.Disassemble((ushort)s[1], (addr) => LibGambatte.gambatte_cpuread(GambatteState, addr), out unused).PadRight(30)
			));
		}

		#region MemoryDomains

		void CreateMemoryDomain(LibGambatte.MemoryAreas which, string name)
		{
			IntPtr data = IntPtr.Zero;
			int length = 0;

			if (!LibGambatte.gambatte_getmemoryarea(GambatteState, which, ref data, ref length))
				throw new Exception("gambatte_getmemoryarea() failed!");

			// if length == 0, it's an empty block; (usually rambank on some carts); that's ok
			if (data != IntPtr.Zero && length > 0)
				_MemoryDomains.Add(MemoryDomain.FromIntPtr(name, length, MemoryDomain.Endian.Little, data));
		}

		void InitMemoryDomains()
		{
			CreateMemoryDomain(LibGambatte.MemoryAreas.wram, "WRAM");
			CreateMemoryDomain(LibGambatte.MemoryAreas.rom, "ROM");
			CreateMemoryDomain(LibGambatte.MemoryAreas.vram, "VRAM");
			CreateMemoryDomain(LibGambatte.MemoryAreas.cartram, "Cart RAM");
			CreateMemoryDomain(LibGambatte.MemoryAreas.oam, "OAM");
			CreateMemoryDomain(LibGambatte.MemoryAreas.hram, "HRAM");

			// also add a special memory domain for the system bus, where calls get sent directly to the core each time

			_MemoryDomains.Add(new MemoryDomain("System Bus", 65536, MemoryDomain.Endian.Little,
				delegate(int addr)
				{
					if (addr < 0 || addr >= 65536)
						throw new ArgumentOutOfRangeException();
					return LibGambatte.gambatte_cpuread(GambatteState, (ushort)addr);
				},
				delegate(int addr, byte val)
				{
					if (addr < 0 || addr >= 65536)
						throw new ArgumentOutOfRangeException();
					LibGambatte.gambatte_cpuwrite(GambatteState, (ushort)addr, val);
				}));

			MemoryDomains = new MemoryDomainList(_MemoryDomains);
		}

		private List<MemoryDomain> _MemoryDomains = new List<MemoryDomain>();
		public MemoryDomainList MemoryDomains { get; private set; }

		#endregion

		#region ppudebug

		public bool GetGPUMemoryAreas(out IntPtr vram, out IntPtr bgpal, out IntPtr sppal, out IntPtr oam)
		{
			IntPtr _vram = IntPtr.Zero;
			IntPtr _bgpal = IntPtr.Zero;
			IntPtr _sppal = IntPtr.Zero;
			IntPtr _oam = IntPtr.Zero;
			int unused = 0;
			if (!LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.vram, ref _vram, ref unused)
				|| !LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.bgpal, ref _bgpal, ref unused)
				|| !LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.sppal, ref _sppal, ref unused)
				|| !LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.oam, ref _oam, ref unused))
			{
				vram = IntPtr.Zero;
				bgpal = IntPtr.Zero;
				sppal = IntPtr.Zero;
				oam = IntPtr.Zero;
				return false;
			}
			vram = _vram;
			bgpal = _bgpal;
			sppal = _sppal;
			oam = _oam;
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="lcdc">current value of register $ff40 (LCDC)</param>
		public delegate void ScanlineCallback(int lcdc);

		/// <summary>
		/// set up callback
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="line">scanline. -1 = end of frame, -2 = RIGHT NOW</param>
		public void SetScanlineCallback(ScanlineCallback callback, int line)
		{
			if (GambatteState == IntPtr.Zero)
				// not sure how this is being reached.  tried the debugger...
				return;
			endofframecallback = null;
			if (callback == null || line == -1 || line == -2)
			{
				scanlinecb = null;
				LibGambatte.gambatte_setscanlinecallback(GambatteState, null, 0);
				if (line == -1)
					endofframecallback = callback;
				else if (line == -2)
					callback(LibGambatte.gambatte_cpuread(GambatteState, 0xff40));
			}
			else if (line >= 0 && line <= 153)
			{
				scanlinecb = delegate()
				{
					callback(LibGambatte.gambatte_cpuread(GambatteState, 0xff40));
				};
				LibGambatte.gambatte_setscanlinecallback(GambatteState, scanlinecb, line);
			}
			else
				throw new ArgumentOutOfRangeException("line must be in [0, 153]");
		}

		LibGambatte.ScanlineCallback scanlinecb;
		ScanlineCallback endofframecallback;

		#endregion

		public void Dispose()
		{
			if (GambatteState != IntPtr.Zero)
			{
				LibGambatte.gambatte_destroy(GambatteState);
				GambatteState = IntPtr.Zero;
			}
			DisposeSound();
		}

		#region IVideoProvider

		public IVideoProvider VideoProvider
		{
			get { return this; }
		}

		/// <summary>
		/// stored image of most recent frame
		/// </summary>
		int[] VideoBuffer = new int[160 * 144];

		public int[] GetVideoBuffer()
		{
			return VideoBuffer;
		}

		public int VirtualWidth
		{
			// only sgb changes this, which we don't emulate here
			get { return 160; }
		}

		public int VirtualHeight
		{
			get { return 144; }
		}

		public int BufferWidth
		{
			get { return 160; }
		}

		public int BufferHeight
		{
			get { return 144; }
		}

		public int BackgroundColor
		{
			get { return 0; }
		}

		#endregion

		#region palette

		/// <summary>
		/// update gambatte core's internal colors
		/// </summary>
		public void ChangeDMGColors(int[] colors)
		{
			for (int i = 0; i < 12; i++)
				LibGambatte.gambatte_setdmgpalettecolor(GambatteState, (LibGambatte.PalType)(i / 4), (uint)i % 4, (uint)colors[i]);
		}

		public void SetCGBColors(GBColors.ColorType type)
		{
			int[] lut = GBColors.GetLut(type);
			LibGambatte.gambatte_setcgbpalette(GambatteState, lut);
		}

		#endregion

		#region ISoundProvider

		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return this; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		/// <summary>
		/// sample pairs before resampling
		/// </summary>
		short[] soundbuff = new short[(35112 + 2064) * 2];

		int soundoutbuffcontains = 0;

		short[] soundoutbuff = new short[2048];

		int latchL = 0;
		int latchR = 0;

		BlipBuffer blipL, blipR;
		uint blipAccumulate;

		private void ProcessSound(int nsamp)
		{
			for (uint i = 0; i < nsamp; i++)
			{
				int curr = soundbuff[i * 2];

				if (curr != latchL)
				{
					int diff = latchL - curr;
					latchL = curr;
					blipL.AddDelta(blipAccumulate, diff);
				}
				curr = soundbuff[i * 2 + 1];

				if (curr != latchR)
				{
					int diff = latchR - curr;
					latchR = curr;
					blipR.AddDelta(blipAccumulate, diff);
				}

				blipAccumulate++;
			}
		}

		private void ProcessSoundEnd()
		{
			blipL.EndFrame(blipAccumulate);
			blipR.EndFrame(blipAccumulate);
			blipAccumulate = 0;

			soundoutbuffcontains = blipL.SamplesAvailable();
			if (soundoutbuffcontains != blipR.SamplesAvailable())
				throw new InvalidOperationException("Audio processing error");

			blipL.ReadSamplesLeft(soundoutbuff, soundoutbuffcontains);
			blipR.ReadSamplesRight(soundoutbuff, soundoutbuffcontains);
		}

		void InitSound()
		{
			blipL = new BlipBuffer(1024);
			blipL.SetRates(TICKSPERSECOND, 44100);
			blipR = new BlipBuffer(1024);
			blipR.SetRates(TICKSPERSECOND, 44100);
		}

		void DisposeSound()
		{
			if (blipL != null)
			{
				blipL.Dispose();
				blipL = null;
			}
			if (blipR != null)
			{
				blipR.Dispose();
				blipR = null;
			}
		}

		public void DiscardSamples()
		{
			soundoutbuffcontains = 0;
		}

		public void GetSamples(out short[] samples, out int nsamp)
		{
			samples = soundoutbuff;
			nsamp = soundoutbuffcontains;
		}

		public bool Muted { get { return _Settings.Muted; } }

		#endregion

		#region Settings

		GambatteSettings _Settings;
		GambatteSyncSettings _SyncSettings;

		public GambatteSettings GetSettings() { return _Settings.Clone(); }
		public GambatteSyncSettings GetSyncSettings() { return _SyncSettings.Clone(); }
		public bool PutSettings(GambatteSettings o)
		{
			_Settings = o;
			if (IsCGBMode())
				SetCGBColors(_Settings.CGBColors);
			else
				ChangeDMGColors(_Settings.GBPalette);
			return false;
		}

		public bool PutSyncSettings(GambatteSyncSettings o)
		{
			bool ret = GambatteSyncSettings.NeedsReboot(_SyncSettings, o);
			_SyncSettings = o;
			return ret;
		}

		public class GambatteSettings
		{
			private static readonly int[] DefaultPalette = new[]
				{
					10798341, 8956165, 1922333, 337157,
					10798341, 8956165, 1922333, 337157,
					10798341, 8956165, 1922333, 337157
				};

			public int[] GBPalette;
			public GBColors.ColorType CGBColors;
			/// <summary>
			/// true to mute all audio
			/// </summary>
			public bool Muted;

			public GambatteSettings()
			{
				GBPalette = (int[])DefaultPalette.Clone();
				CGBColors = GBColors.ColorType.gambatte;
			}

			public GambatteSettings Clone()
			{
				var ret = (GambatteSettings)MemberwiseClone();
				ret.GBPalette = (int[])GBPalette.Clone();
				return ret;
			}
		}

		public class GambatteSyncSettings
		{
			[DisplayName("Force DMG Mode")]
			[Description("Force the game to run on DMG hardware, even if it's detected as a CGB game.  Relevant for games that are \"CGB Enhanced\" but do not require CGB.")]
			[DefaultValue(false)]
			public bool ForceDMG { get; set; }

			[DisplayName("CGB in GBA")]
			[Description("Emulate GBA hardware running a CGB game, instead of CGB hardware.  Relevant only for titles that detect the presense of a GBA, such as Shantae.")]
			[DefaultValue(false)]
			public bool GBACGB { get; set; }

			[DisplayName("Multicart Compatibility")]
			[Description("Use special compatibility hacks for certain multicart games.  Relevant only for specific multicarts.")]
			[DefaultValue(false)]
			public bool MulticartCompat { get; set; }

			[DisplayName("Realtime RTC")]
			[Description("If true, the real time clock in MBC3 games will reflect real time, instead of emulated time.  Ignored (treated as false) when a movie is recording.")]
			[DefaultValue(false)]
			public bool RealTimeRTC { get; set; }

			[DisplayName("RTC Initial Time")]
			[Description("Set the initial RTC time in terms of elapsed seconds.  Only used when RealTimeRTC is false.")]
			[DefaultValue(0)]
			public int RTCInitialTime
			{
				get { return _RTCInitialTime; }
				set { _RTCInitialTime = Math.Max(0, Math.Min(1024 * 24 * 60 * 60, value)); }
			}
			[JsonIgnore]
			int _RTCInitialTime;

			[DisplayName("Equal Length Frames")]
			[Description("When false, emulation frames sync to vblank.  Only useful for high level TASing.")]
			[DefaultValue(true)]
			public bool EqualLengthFrames { get { return _EqualLengthFrames; } set { _EqualLengthFrames = value; } }
			[JsonIgnore]
			[DeepEqualsIgnore]
			private bool _EqualLengthFrames;

			public GambatteSyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public GambatteSyncSettings Clone()
			{
				return (GambatteSyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(GambatteSyncSettings x, GambatteSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}

		#endregion
	}
}
