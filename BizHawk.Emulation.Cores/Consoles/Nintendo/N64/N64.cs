using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.N64;
using BizHawk.Emulation.Cores.Nintendo.N64.NativeApi;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	[CoreAttributes(
		"Mupen64Plus",
		"Richard Goedeken",
		isPorted: true,
		isReleased: true
		)]
	public class N64 : IEmulator
	{
		public Dictionary<string, int> GetCpuFlagsAndRegisters()
		{
			//note: the approach this code takes is highly bug-prone

			var ret = new Dictionary<string, int>();
			byte[] data = new byte[32 * 8 + 4 + 4 + 8 + 8 + 4 + 4 + 32 * 4 + 32 * 8];
			api.getRegisters(data);

			for (int i = 0; i < 32; i++)
			{
				long reg = BitConverter.ToInt64(data, i * 8);
				ret.Add("REG" + i + "_lo", (int)(reg));
				ret.Add("REG" + i + "_hi", (int)(reg>>32));
			}

			UInt32 PC = BitConverter.ToUInt32(data, 32 * 8);
			ret.Add("PC", (int)PC);

			ret.Add("LL", BitConverter.ToInt32(data, 32 * 8 + 4));

			long Lo = BitConverter.ToInt64(data, 32 * 8 + 4 + 4);
			ret.Add("LO_lo", (int)Lo);
			ret.Add("LO_hi", (int)(Lo>>32));

			long Hi = BitConverter.ToInt64(data, 32 * 8 + 4 + 4 + 8);
			ret.Add("HI_lo", (int)Hi);
			ret.Add("HI_hi", (int)(Hi>>32));

			ret.Add("FCR0", BitConverter.ToInt32(data, 32 * 8 + 4 + 4 + 8 + 8));
			ret.Add("FCR31", BitConverter.ToInt32(data, 32 * 8 + 4 + 4 + 8 + 8 + 4));

			for (int i = 0; i < 32; i++)
			{
				uint reg_cop0 = BitConverter.ToUInt32(data, 32 * 8 + 4 + 4 + 8 + 8 + 4 + 4 + i * 4);
				ret.Add("CP0 REG" + i, (int)reg_cop0);
			}

			for (int i = 0; i < 32; i++)
			{
				long reg_cop1_fgr_64 = BitConverter.ToInt64(data, 32 * 8 + 4 + 4 + 8 + 8 + 4 + 4 + 32 * 4 + i * 8);
				ret.Add("CP1 FGR REG" + i + "_lo", (int)reg_cop1_fgr_64);
				ret.Add("CP1 FGR REG" + i + "_hi", (int)(reg_cop1_fgr_64>>32));
			}

			return ret;
		}

		public string SystemId { get { return "N64"; } }

		public string BoardName { get { return null; } }

		public CoreComm CoreComm { get; private set; }

		private N64VideoProvider videoProvider;
		public IVideoProvider VideoProvider { get { return videoProvider; } }
		
		private DisplayType _display_type = DisplayType.NTSC;
		public DisplayType DisplayType { get { return _display_type; } }

		private N64Audio audioProvider;
		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return audioProvider.Resampler; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		private N64Input inputProvider;
		public ControllerDefinition ControllerDefinition { get { return inputProvider.ControllerDefinition; } }
		public IController Controller
		{
			get { return inputProvider.Controller; }
			set { inputProvider.Controller = value; }
		}

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame {
			get { return !inputProvider.LastFrameInputPolled; }
			set { inputProvider.LastFrameInputPolled = !value; }
		}
		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		bool PendingThreadTerminate;
		Action PendingThreadAction;
		EventWaitHandle PendingThreadEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
		EventWaitHandle CompleteThreadEvent = new EventWaitHandle(false, EventResetMode.AutoReset);

		void ThreadLoop()
		{
			for (; ; )
			{
				PendingThreadEvent.WaitOne();
				PendingThreadAction();
				if (PendingThreadTerminate)
					break;
				CompleteThreadEvent.Set();
			}
			PendingThreadTerminate = false;
			CompleteThreadEvent.Set();
		}

		void RunThreadAction(Action action)
		{
			PendingThreadAction = action;
			PendingThreadEvent.Set();
			CompleteThreadEvent.WaitOne();
		}

		void StartThreadLoop()
		{
			new Thread(ThreadLoop).Start();
		}

		void EndThreadLoop()
		{
			RunThreadAction(() => { PendingThreadTerminate = true; });
		}

		public void FrameAdvance(bool render, bool rendersound) 
		{
			audioProvider.RenderSound = rendersound;

			//RunThreadAction(() =>
			{
				if (Controller["Reset"])
				{
					api.soft_reset();
				}
				if (Controller["Power"])
				{
					api.hard_reset();
				}

				api.frame_advance();
			}
			//);

			if (IsLagFrame) LagCount++;
			Frame++;
		}

		public bool DeterministicEmulation { get { return false; } }

		public byte[] ReadSaveRam()
		{
			return api.SaveSaveram();
		}

		public void StoreSaveRam(byte[] data)
		{
			api.LoadSaveram(data);
		}

		public void ClearSaveRam()
		{
			api.InitSaveram();
		}

		public bool SaveRamModified { get { return true; } set { } }

		// these next 5 functions are all exact copy paste from gambatte.
		// if something's wrong here, it's probably wrong there too

		public void SaveStateText(TextWriter writer)
		{
			var temp = SaveStateBinary();
			temp.SaveAsHexFast(writer);
			// write extra copy of stuff we don't use
			writer.WriteLine("Frame {0}", Frame);
		}

		public void LoadStateText(TextReader reader)
		{
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			state.ReadFromHexFast(hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
		}

		byte[] SaveStatePrivateBuff = new byte[16788288 + 1024];
		public void SaveStateBinary(BinaryWriter writer)
		{
			byte[] data = SaveStatePrivateBuff;
			int bytes_used = api.SaveState(data);

			writer.Write(data.Length);
			writer.Write(data);

			byte[] saveram = api.SaveSaveram();
			writer.Write(saveram);
			if (saveram.Length != mupen64plusApi.kSaveramSize)
				throw new InvalidOperationException("Unexpected N64 SaveRam size");

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			int length = reader.ReadInt32();
			reader.Read(SaveStatePrivateBuff, 0, length);
			byte[] data = SaveStatePrivateBuff;

			api.LoadState(data);

			reader.Read(SaveStatePrivateBuff, 0, mupen64plusApi.kSaveramSize);
			api.LoadSaveram(SaveStatePrivateBuff);

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}

		byte[] SaveStateBinaryPrivateBuff = new byte[0];

		public byte[] SaveStateBinary()
		{
			// WELCOME TO THE HACK ZONE
			byte[] saveram = api.SaveSaveram();

			int lenwant = 4 + SaveStatePrivateBuff.Length + saveram.Length + 1 + 4 + 4;
			if (SaveStateBinaryPrivateBuff.Length != lenwant)
			{
				Console.WriteLine("Allocating new N64 private buffer size {0}", lenwant);
				SaveStateBinaryPrivateBuff = new byte[lenwant];
			}

			MemoryStream ms = new MemoryStream(SaveStateBinaryPrivateBuff);
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();

			if (ms.Length != SaveStateBinaryPrivateBuff.Length)
				throw new Exception("Unexpected Length");

			return SaveStateBinaryPrivateBuff;// ms.ToArray();
		}

		public bool BinarySaveStatesPreferred { get { return true; } }

		#region memorycallback

		mupen64plusApi.MemoryCallback readcb;
		mupen64plusApi.MemoryCallback writecb;

		void RefreshMemoryCallbacks()
		{
			var mcs = CoreComm.MemoryCallbackSystem;

			// we RefreshMemoryCallbacks() after the triggers in case the trigger turns itself off at that point

			if (mcs.HasReads)
				readcb = delegate(uint addr) { mcs.CallRead(addr); };
			else
				readcb = null;
			if (mcs.HasWrites)
				writecb = delegate(uint addr) { mcs.CallWrite(addr); };
			else
				writecb = null;

			api.setReadCallback(readcb);
			api.setWriteCallback(writecb);
		}

		#endregion

		#region memorydomains

		private MemoryDomain MakeMemoryDomain(string name, mupen64plusApi.N64_MEMORY id, MemoryDomain.Endian endian)
		{
			int size = api.get_memory_size(id);

			//if this type of memory isnt available, dont make the memory domain
			if (size == 0)
				return null;

			IntPtr memPtr = api.get_memory_ptr(id);

			MemoryDomain md = new MemoryDomain(
				name,
				size,
				endian,
				delegate(int addr)
				{
					if (addr < 0 || addr >= size)
						throw new ArgumentOutOfRangeException();
					return Marshal.ReadByte(memPtr, addr);
				},
				delegate(int addr, byte val)
				{
					if (addr < 0 || addr >= size)
						throw new ArgumentOutOfRangeException();
					Marshal.WriteByte(memPtr + addr, val);
				});

			memoryDomains.Add(md);

			return md;
		}

		void InitMemoryDomains()
		{
			MakeMemoryDomain("RDRAM", mupen64plusApi.N64_MEMORY.RDRAM, MemoryDomain.Endian.Little);
			MakeMemoryDomain("PI Register", mupen64plusApi.N64_MEMORY.PI_REG, MemoryDomain.Endian.Little);
			MakeMemoryDomain("SI Register", mupen64plusApi.N64_MEMORY.SI_REG, MemoryDomain.Endian.Little);
			MakeMemoryDomain("VI Register", mupen64plusApi.N64_MEMORY.VI_REG, MemoryDomain.Endian.Little);
			MakeMemoryDomain("RI Register", mupen64plusApi.N64_MEMORY.RI_REG, MemoryDomain.Endian.Little);
			MakeMemoryDomain("AI Register", mupen64plusApi.N64_MEMORY.AI_REG, MemoryDomain.Endian.Little);

			MakeMemoryDomain("EEPROM", mupen64plusApi.N64_MEMORY.EEPROM, MemoryDomain.Endian.Little);
			MakeMemoryDomain("Mempak 1", mupen64plusApi.N64_MEMORY.MEMPAK1, MemoryDomain.Endian.Little);
			MakeMemoryDomain("Mempak 2", mupen64plusApi.N64_MEMORY.MEMPAK2, MemoryDomain.Endian.Little);
			MakeMemoryDomain("Mempak 3", mupen64plusApi.N64_MEMORY.MEMPAK3, MemoryDomain.Endian.Little);
			MakeMemoryDomain("Mempak 4", mupen64plusApi.N64_MEMORY.MEMPAK4, MemoryDomain.Endian.Little);

			MemoryDomains = new MemoryDomainList(memoryDomains);
		}

		private List<MemoryDomain> memoryDomains = new List<MemoryDomain>();
		public MemoryDomainList MemoryDomains { get; private set; }

		#endregion

		public void Dispose()
		{
			RunThreadAction(() =>
			{
				videoProvider.Dispose();
				audioProvider.Dispose();
				api.Dispose();
			});
			EndThreadLoop();
		}

		// mupen64plus DLL Api
		private mupen64plusApi api;

		/// <summary>
		/// Create mupen64plus Emulator
		/// </summary>
		/// <param name="comm">Core communication object</param>
		/// <param name="game">Game information of game to load</param>
		/// <param name="rom">Rom that should be loaded</param>
		/// <param name="SyncSettings">N64SyncSettings object</param>
		public N64(CoreComm comm, GameInfo game, byte[] rom, object SyncSettings)
		{
			int SaveType = 0;
			if (game.OptionValue("SaveType") == "EEPROM_16K")
				SaveType = 1;

			CoreComm = comm;

			this.SyncSettings = (N64SyncSettings)SyncSettings ?? new N64SyncSettings();

			byte country_code = rom[0x3E];
			switch (country_code)
			{
				// PAL codes
				case 0x44:
				case 0x46:
				case 0x49:
				case 0x50:
				case 0x53:
				case 0x55:
				case 0x58:
				case 0x59:
					_display_type = DisplayType.PAL;
					break;

				// NTSC codes
				case 0x37:
				case 0x41:
				case 0x45:
				case 0x4a:
				default: // Fallback for unknown codes
					_display_type = DisplayType.NTSC;
					break;
			}
			switch (DisplayType)
			{
				case DisplayType.NTSC:
					comm.VsyncNum = 60000;
					comm.VsyncDen = 1001;
					break;
				case DisplayType.PAL:
				case DisplayType.DENDY:
				default:
					comm.VsyncNum = 50;
					comm.VsyncDen = 1;
					break;
			}

			StartThreadLoop();

			var videosettings = this.SyncSettings.GetVPS(game);

			//zero 19-apr-2014 - added this to solve problem with SDL initialization corrupting the main thread (I think) and breaking subsequent emulators (for example, NES)
			//not sure why this works... if we put the plugin initializations in here, we get deadlocks in some SDL initialization. doesnt make sense to me...
			RunThreadAction(() =>
			{
				api = new mupen64plusApi(this, rom, videosettings, SaveType);
			});

			// Order is important because the register with the mupen core
			videoProvider = new N64VideoProvider(api, videosettings);
			audioProvider = new N64Audio(api);
			inputProvider = new N64Input(api, comm, this.SyncSettings.Controllers);
			api.AttachPlugin(mupen64plusApi.m64p_plugin_type.M64PLUGIN_RSP,
				"mupen64plus-rsp-hle.dll");

			InitMemoryDomains();
			RefreshMemoryCallbacks();

			api.AsyncExecuteEmulator();
		}

		N64SyncSettings SyncSettings;

		public object GetSettings() { return null; }
		public object GetSyncSettings() { return SyncSettings.Clone(); }
		public bool PutSettings(object o) { return false; }
		public bool PutSyncSettings(object o) { SyncSettings = (N64SyncSettings)o; return true; }
	}
}
