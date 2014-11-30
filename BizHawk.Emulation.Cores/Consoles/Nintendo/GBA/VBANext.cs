using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using Newtonsoft.Json;
using System.ComponentModel;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	[CoreAttributes("VBA-Next", "many authors", true, true, "cd508312a29ed8c29dacac1b11c2dce56c338a54", "https://github.com/libretro/vba-next")]
	public class VBANext : IEmulator, IVideoProvider, ISyncSoundProvider, IInputPollable,
		IGBAGPUViewable, IMemoryDomains, ISaveRam, IStatable, IDebuggable, ISettable<object, VBANext.SyncSettings>
	{
		IntPtr Core;

		[CoreConstructor("GBA")]
		public VBANext(byte[] file, CoreComm comm, GameInfo game, bool deterministic, object syncsettings)
		{
			CoreComm = comm;
			byte[] biosfile = CoreComm.CoreFileProvider.GetFirmware("GBA", "Bios", true, "GBA bios file is mandatory.");
			if (file.Length > 32 * 1024 * 1024)
				throw new ArgumentException("ROM is too big to be a GBA ROM!");
			if (biosfile.Length != 16 * 1024)
				throw new ArgumentException("BIOS file is not exactly 16K!");

			LibVBANext.FrontEndSettings FES = new LibVBANext.FrontEndSettings();
			FES.saveType = (LibVBANext.FrontEndSettings.SaveType)game.GetInt("saveType", 0);
			FES.flashSize = (LibVBANext.FrontEndSettings.FlashSize)game.GetInt("flashSize", 0x10000);
			FES.enableRtc = game.GetInt("rtcEnabled", 0) != 0;
			FES.mirroringEnable = game.GetInt("mirroringEnabled", 0) != 0;

			Console.WriteLine("GameDB loaded settings: saveType={0}, flashSize={1}, rtcEnabled={2}, mirroringEnabled={3}",
				FES.saveType, FES.flashSize, FES.enableRtc, FES.mirroringEnable);

			_SyncSettings = (SyncSettings)syncsettings ?? new SyncSettings();
			DeterministicEmulation = deterministic;

			FES.skipBios = _SyncSettings.SkipBios;
			FES.RTCUseRealTime = _SyncSettings.RTCUseRealTime;
			FES.RTCwday = (int)_SyncSettings.RTCInitialDay;
			FES.RTCyear = _SyncSettings.RTCInitialTime.Year % 100;
			FES.RTCmonth = _SyncSettings.RTCInitialTime.Month - 1;
			FES.RTCmday = _SyncSettings.RTCInitialTime.Day;
			FES.RTChour = _SyncSettings.RTCInitialTime.Hour;
			FES.RTCmin = _SyncSettings.RTCInitialTime.Minute;
			FES.RTCsec = _SyncSettings.RTCInitialTime.Second;
			if (DeterministicEmulation)
			{
				// FES.skipBios = false; // this is OK; it is deterministic and probably accurate
				FES.RTCUseRealTime = false;
			}

			Core = LibVBANext.Create();
			if (Core == IntPtr.Zero)
				throw new InvalidOperationException("Create() returned nullptr!");
			try
			{
				if (!LibVBANext.LoadRom(Core, file, (uint)file.Length, biosfile, (uint)biosfile.Length, FES))
					throw new InvalidOperationException("LoadRom() returned false!");

				CoreComm.VsyncNum = 262144;
				CoreComm.VsyncDen = 4389;
				CoreComm.NominalWidth = 240;
				CoreComm.NominalHeight = 160;

				GameCode = Encoding.ASCII.GetString(file, 0xac, 4);
				Console.WriteLine("Game code \"{0}\"", GameCode);

				savebuff = new byte[LibVBANext.BinStateSize(Core)];
				savebuff2 = new byte[savebuff.Length + 13];
				InitMemoryDomains();
				InitRegisters();
				InitCallbacks();

				CoreComm.CpuTraceAvailable = true;

				// todo: hook me up as a setting
				SetupColors();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			Frame++;

			if (Controller["Power"])
				LibVBANext.Reset(Core);

			SyncCallbacks();

			IsLagFrame = LibVBANext.FrameAdvance(Core, GetButtons(), videobuff, soundbuff, out numsamp, videopalette);

			if (IsLagFrame)
				LagCount++;
		}

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; private set; }

		public string SystemId { get { return "GBA"; } }

		public bool DeterministicEmulation { get; private set; }

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public string BoardName { get { return null; } }
		/// <summary>
		/// set in the ROM internal header
		/// </summary>
		public string GameCode { get; private set; }

		public CoreComm CoreComm { get; private set; }

		public void Dispose()
		{
			if (Core != IntPtr.Zero)
			{
				LibVBANext.Destroy(Core);
				Core = IntPtr.Zero;
			}
		}

		#region SaveRam

		public byte[] CloneSaveRam()
		{
			byte[] data = new byte[LibVBANext.SaveRamSize(Core)];
			if (!LibVBANext.SaveRamSave(Core, data, data.Length))
				throw new InvalidOperationException("SaveRamSave() failed!");
			return data;
		}

		public void StoreSaveRam(byte[] data)
		{
			// internally, we try to salvage bad-sized saverams
			if (!LibVBANext.SaveRamLoad(Core, data, data.Length))
				throw new InvalidOperationException("SaveRamLoad() failed!");
		}

		public bool SaveRamModified
		{
			get
			{
				return LibVBANext.SaveRamSize(Core) != 0;
			}
		}

		#endregion

		#region SaveStates

		JsonSerializer ser = new JsonSerializer() { Formatting = Formatting.Indented };
		byte[] savebuff;
		byte[] savebuff2;

		class TextStateData
		{
			public int Frame;
			public int LagCount;
			public bool IsLagFrame;
		}

		public void SaveStateText(TextWriter writer)
		{
			var s = new TextState<TextStateData>();
			s.Prepare();
			var ff = s.GetFunctionPointersSave();
			LibVBANext.TxtStateSave(Core, ref ff);
			s.ExtraData.IsLagFrame = IsLagFrame;
			s.ExtraData.LagCount = LagCount;
			s.ExtraData.Frame = Frame;

			ser.Serialize(writer, s);
			// write extra copy of stuff we don't use
			writer.WriteLine();
			writer.WriteLine("Frame {0}", Frame);

			//Console.WriteLine(BizHawk.Common.BufferExtensions.BufferExtensions.HashSHA1(SaveStateBinary()));
		}

		public void LoadStateText(TextReader reader)
		{
			var s = (TextState<TextStateData>)ser.Deserialize(reader, typeof(TextState<TextStateData>));
			s.Prepare();
			var ff = s.GetFunctionPointersLoad();
			LibVBANext.TxtStateLoad(Core, ref ff);
			IsLagFrame = s.ExtraData.IsLagFrame;
			LagCount = s.ExtraData.LagCount;
			Frame = s.ExtraData.Frame;
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			if (!LibVBANext.BinStateSave(Core, savebuff, savebuff.Length))
				throw new InvalidOperationException("Core's BinStateSave() returned false!");
			writer.Write(savebuff.Length);
			writer.Write(savebuff);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			int length = reader.ReadInt32();
			if (length != savebuff.Length)
				throw new InvalidOperationException("Save buffer size mismatch!");
			reader.Read(savebuff, 0, length);
			if (!LibVBANext.BinStateLoad(Core, savebuff, savebuff.Length))
				throw new InvalidOperationException("Core's BinStateLoad() returned false!");

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}

		public byte[] SaveStateBinary()
		{
			var ms = new MemoryStream(savebuff2, true);
			var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			if (ms.Position != savebuff2.Length)
				throw new InvalidOperationException();
			ms.Close();
			return savebuff2;
		}

		public bool BinarySaveStatesPreferred
		{
			get { return true; }
		}

		#endregion

		#region Debugging

		LibVBANext.StandardCallback padcb;
		LibVBANext.AddressCallback fetchcb;
		LibVBANext.AddressCallback readcb;
		LibVBANext.AddressCallback writecb;
		LibVBANext.TraceCallback tracecb;

		string Trace(uint addr, uint opcode)
		{
			return
				string.Format("{0:x8} {1} {2}",
				opcode,
				(Emulation.Cores.Components.ARM.Darm.DisassembleStuff(addr, opcode) ?? "").PadRight(30),
				regs.TraceString());
		}

		void InitCallbacks()
		{
			padcb = new LibVBANext.StandardCallback(() => CoreComm.InputCallback.Call());
			fetchcb = new LibVBANext.AddressCallback((addr) => CoreComm.MemoryCallbackSystem.CallExecute(addr));
			readcb = new LibVBANext.AddressCallback((addr) => CoreComm.MemoryCallbackSystem.CallRead(addr));
			writecb = new LibVBANext.AddressCallback((addr) => CoreComm.MemoryCallbackSystem.CallWrite(addr));
			tracecb = new LibVBANext.TraceCallback((addr, opcode) => CoreComm.Tracer.Put(Trace(addr, opcode)));
		}

		void SyncCallbacks()
		{
			LibVBANext.SetPadCallback(Core, CoreComm.InputCallback.Any() ? padcb : null);
			LibVBANext.SetFetchCallback(Core, CoreComm.MemoryCallbackSystem.HasExecutes ? fetchcb : null);
			LibVBANext.SetReadCallback(Core, CoreComm.MemoryCallbackSystem.HasReads ? readcb : null);
			LibVBANext.SetWriteCallback(Core, CoreComm.MemoryCallbackSystem.HasWrites ? writecb : null);
			LibVBANext.SetTraceCallback(Core, CoreComm.Tracer.Enabled ? tracecb : null);
		}

		LibVBANext.StandardCallback scanlinecb;

		GBAGPUMemoryAreas IGBAGPUViewable.GetMemoryAreas()
		{
			var s = new LibVBANext.MemoryAreas();
			LibVBANext.GetMemoryAreas(Core, s);
			return new GBAGPUMemoryAreas
			{
				mmio = s.mmio,
				oam = s.oam,
				palram = s.palram,
				vram = s.vram
			};
		}

		void IGBAGPUViewable.SetScanlineCallback(Action callback, int scanline)
		{
			if (scanline < 0 || scanline > 227)
			{
				throw new ArgumentOutOfRangeException("Scanline must be in [0, 227]!");
			}
			if (callback == null)
			{
				scanlinecb = null;
				LibVBANext.SetScanlineCallback(Core, scanlinecb, 0);
			}
			else
			{
				scanlinecb = new LibVBANext.StandardCallback(callback);
				LibVBANext.SetScanlineCallback(Core, scanlinecb, scanline);
			}
		}

		void InitMemoryDomains()
		{
			var mm = new List<MemoryDomain>();
			var s = new LibVBANext.MemoryAreas();
			var l = MemoryDomain.Endian.Little;
			LibVBANext.GetMemoryAreas(Core, s);
			mm.Add(MemoryDomain.FromIntPtr("IWRAM", 32 * 1024, l, s.iwram));
			mm.Add(MemoryDomain.FromIntPtr("EWRAM", 256 * 1024, l, s.ewram));
			mm.Add(MemoryDomain.FromIntPtr("BIOS", 16 * 1024, l, s.bios, false));
			mm.Add(MemoryDomain.FromIntPtr("PALRAM", 1024, l, s.palram, false));
			mm.Add(MemoryDomain.FromIntPtr("VRAM", 96 * 1024, l, s.vram));
			mm.Add(MemoryDomain.FromIntPtr("OAM", 1024, l, s.oam));
			mm.Add(MemoryDomain.FromIntPtr("ROM", 32 * 1024 * 1024, l, s.rom));

			mm.Add(new MemoryDomain("BUS", 0x10000000, l,
				delegate(int addr)
				{
					if (addr < 0 || addr >= 0x10000000)
						throw new ArgumentOutOfRangeException();
					return LibVBANext.SystemBusRead(Core, addr);
				},
				delegate(int addr, byte val)
				{
					if (addr < 0 || addr >= 0x10000000)
						throw new ArgumentOutOfRangeException();
					LibVBANext.SystemBusWrite(Core, addr, val);
				}));
			// special combined ram memory domain
			{
				var ew = mm[1];
				var iw = mm[0];
				MemoryDomain cr = new MemoryDomain("Combined WRAM", (256 + 32) * 1024, MemoryDomain.Endian.Little,
					delegate(int addr)
					{
						if (addr < 0 || addr >= (256 + 32) * 1024)
							throw new IndexOutOfRangeException();
						if (addr >= 256 * 1024)
							return iw.PeekByte(addr & 32767);
						else
							return ew.PeekByte(addr);
					},
					delegate(int addr, byte val)
					{
						if (addr < 0 || addr >= (256 + 32) * 1024)
							throw new IndexOutOfRangeException();
						if (addr >= 256 * 1024)
							iw.PokeByte(addr & 32767, val);
						else
							ew.PokeByte(addr, val);
					});
				mm.Add(cr);
			}
			MemoryDomains = new MemoryDomainList(mm, 0);
		}

		public MemoryDomainList MemoryDomains { get; private set; }

		VBARegisterHelper regs;

		void InitRegisters()
		{
			regs = new VBARegisterHelper(Core);
		}

		public IDictionary<string, int> GetCpuFlagsAndRegisters()
		{
			return regs.GetAllRegisters();
		}

		public void SetCpuRegister(string register, int value)
		{
			regs.SetRegister(register, value);
		}

		#endregion

		#region Settings

		public object GetSettings()
		{
			return null;
		}

		public SyncSettings GetSyncSettings()
		{
			return _SyncSettings.Clone();
		}

		SyncSettings _SyncSettings;


		public bool PutSettings(object o)
		{
			return false;
		}

		public bool PutSyncSettings(SyncSettings o)
		{
			bool ret = SyncSettings.NeedsReboot(o, _SyncSettings);
			_SyncSettings = o;
			return ret;
		}

		public class SyncSettings
		{
			[DisplayName("Skip BIOS")]
			[Description("Skips the BIOS intro.  A BIOS file is still required.")]
			[DefaultValue(true)]
			public bool SkipBios { get; set; }
			[DisplayName("RTC Use Real Time")]
			[Description("Causes the internal clock to reflect your system clock.  Only relevant when a game has an RTC chip.  Forced to false for movie recording.")]
			[DefaultValue(true)]
			public bool RTCUseRealTime { get; set; }

			[DisplayName("RTC Initial Time")]
			[Description("The initial time of emulation.  Only relevant when a game has an RTC chip and \"RTC Use Real Time\" is false.")]
			[DefaultValue(typeof(DateTime), "2010-01-01")]
			public DateTime RTCInitialTime { get; set; }

			public enum DayOfWeek
			{
				Sunday = 0,
				Monday,
				Tuesday,
				Wednesday,
				Thursday,
				Friday,
				Saturday
			}

			[DisplayName("RTC Initial Day")]
			[Description("The day of the week to go with \"RTC Initial Time\".  Due to peculiarities in the RTC chip, this can be set indepedently of the year, month, and day of month.")]
			[DefaultValue(DayOfWeek.Friday)]
			public DayOfWeek RTCInitialDay { get; set; }

			public SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}

			public SyncSettings Clone()
			{
				return (SyncSettings)MemberwiseClone();
			}
		}

		#endregion

		#region Controller

		public ControllerDefinition ControllerDefinition { get { return GBA.GBAController; } }
		public IController Controller { get; set; }

		private LibVBANext.Buttons GetButtons()
		{
			LibVBANext.Buttons ret = 0;
			foreach (string s in Enum.GetNames(typeof(LibVBANext.Buttons)))
			{
				if (Controller[s])
					ret |= (LibVBANext.Buttons)Enum.Parse(typeof(LibVBANext.Buttons), s);
			}
			return ret;
		}

		#endregion

		#region VideoProvider

		int[] videobuff = new int[240 * 160];
		int[] videopalette = new int[65536];

		public IVideoProvider VideoProvider { get { return this; } }
		public int[] GetVideoBuffer() { return videobuff; }
		public int VirtualWidth { get { return 240; } }
		public int VirtualHeight { get { return 160; } }
		public int BufferWidth { get { return 240; } }
		public int BufferHeight { get { return 160; } }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		void SetupColors()
		{
			int[] tmp = BizHawk.Emulation.Cores.Nintendo.Gameboy.GBColors.GetLut(Gameboy.GBColors.ColorType.vivid);
			// reorder
			for (int i = 0; i < 32768; i++)
			{
				int j = i & 0x3e0 | (i & 0x1f) << 10 | i >> 10 & 0x1f;
				videopalette[i] = tmp[j];
			}
			// duplicate
			Array.Copy(videopalette, 0, videopalette, 32768, 32768);
		}

		#endregion

		#region SoundProvider

		short[] soundbuff = new short[2048];
		int numsamp;

		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return this; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		public void GetSamples(out short[] samples, out int nsamp)
		{
			samples = soundbuff;
			nsamp = numsamp;
		}

		public void DiscardSamples()
		{
		}

		#endregion
	}
}
