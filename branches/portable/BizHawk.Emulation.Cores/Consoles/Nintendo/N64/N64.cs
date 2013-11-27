using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public class N64 : IEmulator, IVideoProvider
	{
		public List<KeyValuePair<string, int>> GetCpuFlagsAndRegisters()
		{
			//note: the approach this code takes is highly bug-prone

			List<KeyValuePair<string, int>> ret = new List<KeyValuePair<string, int>>();
			byte[] data = new byte[32 * 8 + 4 + 4 + 8 + 8 + 4 + 4 + 32 * 4 + 32 * 8];
			api.getRegisters(data);

			for (int i = 0; i < 32; i++)
			{
				long reg = BitConverter.ToInt64(data, i * 8);
				ret.Add(new KeyValuePair<string, int>("REG" + i + "_lo", (int)(reg)));
				ret.Add(new KeyValuePair<string, int>("REG" + i + "_hi", (int)(reg>>32)));
			}

			UInt32 PC = BitConverter.ToUInt32(data, 32 * 8);
			ret.Add(new KeyValuePair<string, int>("PC", (int)PC));

			ret.Add(new KeyValuePair<string, int>("LL", BitConverter.ToInt32(data, 32 * 8 + 4)));

			long Lo = BitConverter.ToInt64(data, 32 * 8 + 4 + 4);
			ret.Add(new KeyValuePair<string, int>("LO_lo", (int)Lo));
			ret.Add(new KeyValuePair<string, int>("LO_hi", (int)(Lo>>32)));

			long Hi = BitConverter.ToInt64(data, 32 * 8 + 4 + 4 + 8);
			ret.Add(new KeyValuePair<string, int>("HI_lo", (int)Hi));
			ret.Add(new KeyValuePair<string, int>("HI_hi", (int)(Hi>>32)));

			ret.Add(new KeyValuePair<string, int>("FCR0", BitConverter.ToInt32(data, 32 * 8 + 4 + 4 + 8 + 8)));
			ret.Add(new KeyValuePair<string, int>("FCR31", BitConverter.ToInt32(data, 32 * 8 + 4 + 4 + 8 + 8 + 4)));

			for (int i = 0; i < 32; i++)
			{
				uint reg_cop0 = BitConverter.ToUInt32(data, 32 * 8 + 4 + 4 + 8 + 8 + 4 + 4 + i * 4);
				ret.Add(new KeyValuePair<string, int>("CP0 REG" + i, (int)reg_cop0));
			}

			for (int i = 0; i < 32; i++)
			{
				long reg_cop1_fgr_64 = BitConverter.ToInt64(data, 32 * 8 + 4 + 4 + 8 + 8 + 4 + 4 + 32 * 4 + i * 8);
				ret.Add(new KeyValuePair<string, int>("CP1 FGR REG" + i + "_lo", (int)reg_cop1_fgr_64));
				ret.Add(new KeyValuePair<string, int>("CP1 FGR REG" + i + "_hi", (int)(reg_cop1_fgr_64>>32)));
			}

			return ret;
		}

		public string SystemId { get { return "N64"; } }

		public string BoardName { get { return null; } }

		public CoreComm CoreComm { get; private set; }
		public byte[] rom;
		public GameInfo game;

		public IVideoProvider VideoProvider { get { return this; } }
		public int[] frameBuffer;// = new int[800 * 600];
		public int[] GetVideoBuffer() {	return frameBuffer;	}
		public int VirtualWidth { get; set; }
		public int BufferWidth { get; set; }
		public int BufferHeight { get; set; }
		public int BackgroundColor { get { return 0; } }
		
		private DisplayType _display_type = DisplayType.NTSC;
		public DisplayType DisplayType { get { return _display_type; } }

		public SpeexResampler resampler;

		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return resampler; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		public ControllerDefinition ControllerDefinition { get { return N64ControllerDefinition; } }
		public IController Controller { get; set; }
		public static readonly ControllerDefinition N64ControllerDefinition = new ControllerDefinition
		{
			Name = "Nintento 64 Controller",
			BoolButtons =
			{
				"P1 A Up", "P1 A Down", "P1 A Left", "P1 A Right", "P1 DPad U", "P1 DPad D", "P1 DPad L", "P1 DPad R", "P1 Start", "P1 Z", "P1 B", "P1 A", "P1 C Up", "P1 C Down", "P1 C Right", "P1 C Left", "P1 L", "P1 R", 
				"P2 A Up", "P2 A Down", "P2 A Left", "P2 A Right", "P2 DPad U", "P2 DPad D", "P2 DPad L", "P2 DPad R", "P2 Start", "P2 Z", "P2 B", "P2 A", "P2 C Up", "P2 C Down", "P2 C Right", "P2 C Left", "P2 L", "P2 R", 
				"P3 A Up", "P3 A Down", "P3 A Left", "P3 A Right", "P3 DPad U", "P3 DPad D", "P3 DPad L", "P3 DPad R", "P3 Start", "P3 Z", "P3 B", "P3 A", "P3 C Up", "P3 C Down", "P3 C Right", "P3 C Left", "P3 L", "P3 R", 
				"P4 A Up", "P4 A Down", "P4 A Left", "P4 A Right", "P4 DPad U", "P4 DPad D", "P4 DPad L", "P4 DPad R", "P4 Start", "P4 Z", "P4 B", "P4 A", "P4 C Up", "P4 C Down", "P4 C Right", "P4 C Left", "P4 L", "P4 R", 
				"Reset", "Power"
			},
			FloatControls =
			{
				"P1 X Axis", "P1 Y Axis",
				"P2 X Axis", "P2 Y Axis",
				"P3 X Axis", "P3 Y Axis",
				"P4 X Axis", "P4 Y Axis"
			},
			FloatRanges =
			{
				new[] {-128.0f, 0.0f, 127.0f},
				new[] {-128.0f, 0.0f, 127.0f},
				new[] {-128.0f, 0.0f, 127.0f},
				new[] {-128.0f, 0.0f, 127.0f},
				new[] {-128.0f, 0.0f, 127.0f},
				new[] {-128.0f, 0.0f, 127.0f},
				new[] {-128.0f, 0.0f, 127.0f},
				new[] {-128.0f, 0.0f, 127.0f}
			}
		};

		public int Frame { get; set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }
		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}
		public void FrameAdvance(bool render, bool rendersound) 
		{
			RefreshMemoryCallbacks();

			if (Controller["Reset"])
			{
				api.soft_reset();
			}
			if (Controller["Power"])
			{
				api.hard_reset();
			}

			IsLagFrame = true;
			api.frame_advance();
			if (IsLagFrame) LagCount++;
			Frame++; 
		}

		public void setControllers()
		{
			CoreComm.InputCallback.Call();
			IsLagFrame = false;

			// Analog stick right = +X
			// Analog stick up = +Y

			for (int i = 0; i < 4; i++)
			{
				string p = "P" + (i + 1);
				sbyte x;
				if (Controller.IsPressed(p + " A Left")) { x = -127; }
				else if (Controller.IsPressed(p + " A Right")) { x = 127; }
				else { x = (sbyte)Controller.GetFloat(p + " X Axis"); }

				sbyte y;
				if (Controller.IsPressed(p + " A Up")) { y = 127; }
				else if (Controller.IsPressed(p + " A Down")) { y = -127; }
				else { y = (sbyte)Controller.GetFloat(p + " Y Axis"); }

				api.set_buttons(i, ReadController(i+1), x, y);
			}
		}

		public int ReadController(int num)
		{
			int buttons = 0;

			if (Controller["P" + num + " DPad R"]) buttons |= (1 << 0);
			if (Controller["P" + num + " DPad L"]) buttons |= (1 << 1);
			if (Controller["P" + num + " DPad D"]) buttons |= (1 << 2);
			if (Controller["P" + num + " DPad U"]) buttons |= (1 << 3);
			if (Controller["P" + num + " Start"]) buttons |= (1 << 4);
			if (Controller["P" + num + " Z"]) buttons |= (1 << 5);
			if (Controller["P" + num + " B"]) buttons |= (1 << 6);
			if (Controller["P" + num + " A"]) buttons |= (1 << 7);
			if (Controller["P" + num + " C Right"]) buttons |= (1 << 8);
			if (Controller["P" + num + " C Left"]) buttons |= (1 << 9);
			if (Controller["P" + num + " C Down"]) buttons |= (1 << 10);
			if (Controller["P" + num + " C Up"]) buttons |= (1 << 11);
			if (Controller["P" + num + " R"]) buttons |= (1 << 12);
			if (Controller["P" + num + " L"]) buttons |= (1 << 13);

			return buttons;
		}

		public bool DeterministicEmulation { get; set; }

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

		void SyncState(Serializer ser)
		{
			ser.BeginSection("N64");
			ser.EndSection();
		}

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
			if (hex.StartsWith("emuVersion")) // movie save
			{
				do // theoretically, our portion should start right after StartsFromSavestate, maybe...
				{
					hex = reader.ReadLine();
				} while (!hex.StartsWith("StartsFromSavestate"));
				hex = reader.ReadLine();
			}
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
				readcb = delegate(uint addr) { mcs.CallRead(addr); RefreshMemoryCallbacks(); };
			else
				readcb = null;
			if (mcs.HasWrites)
				writecb = delegate(uint addr) { mcs.CallWrite(addr); RefreshMemoryCallbacks(); };
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
			api.Dispose();
		}

		mupen64plusApi api;

		public N64(CoreComm comm, GameInfo game, byte[] rom, VideoPluginSettings video_settings, int SaveType)
		{
			CoreComm = comm;
			this.rom = rom;
			this.game = game;

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

			api = new mupen64plusApi(this, rom, video_settings, SaveType);
			api.SetM64PInputCallback(new mupen64plusApi.InputCallback(setControllers));

			InitMemoryDomains();
		}
	}
}
