using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Newtonsoft.Json;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	[CoreAttributes(
		"QuickNes",
		"",
		isPorted: true,
		isReleased: true,
		portedVersion: "0.7.0",
		portedUrl: "https://github.com/kode54/QuickNES"
		)]
	public class QuickNES : IEmulator, IVideoProvider, ISyncSoundProvider, IMemoryDomains,
		IDebuggable, ISettable<QuickNES.QuickNESSettings, QuickNES.QuickNESSyncSettings>
	{
		#region FPU precision

		private class FPCtrl : IDisposable
		{
			[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern uint _control87(uint @new, uint mask);

			public static void PrintCurrentFP()
			{
				uint curr = _control87(0, 0);
				Console.WriteLine("Current FP word: 0x{0:x8}", curr);
			}

			uint cw;

			public IDisposable Save()
			{
				cw = _control87(0, 0);
				_control87(0x00000, 0x30000);
				return this;
			}
			public void Dispose()
			{
				_control87(cw, 0x30000);
			}
		}

		FPCtrl FP = new FPCtrl();

		#endregion

		static QuickNES()
		{
			LibQuickNES.qn_setup_mappers();
		}

		[CoreConstructor("NES")]
		public QuickNES(CoreComm comm, byte[] file, object Settings)
		{
			using (FP.Save())
			{
				CoreComm = comm;

				Context = LibQuickNES.qn_new();
				if (Context == IntPtr.Zero)
					throw new InvalidOperationException("qn_new() returned NULL");
				try
				{
					LibQuickNES.ThrowStringError(LibQuickNES.qn_loadines(Context, file, file.Length));

					InitSaveRamBuff();
					InitSaveStateBuff();
					InitAudio();
					InitMemoryDomains();

					int mapper = 0;
					string mappername = Marshal.PtrToStringAnsi(LibQuickNES.qn_get_mapper(Context, ref mapper));
					Console.WriteLine("QuickNES: Booted with Mapper #{0} \"{1}\"", mapper, mappername);
					BoardName = mappername;
					CoreComm.VsyncNum = 39375000;
					CoreComm.VsyncDen = 655171;
					PutSettings((QuickNESSettings)Settings ?? new QuickNESSettings());

					ComputeBootGod();
				}
				catch
				{
					Dispose();
					throw;
				}
			}
		}

		#region Controller

		public ControllerDefinition ControllerDefinition { get { return Emulation.Cores.Nintendo.NES.NES.NESController; } }
		public IController Controller { get; set; }

		void SetPads(out int j1, out int j2)
		{
			j1 = 0;
			j2 = 0;
			if (Controller["P1 A"])
				j1 |= 1;
			if (Controller["P1 B"])
				j1 |= 2;
			if (Controller["P1 Select"])
				j1 |= 4;
			if (Controller["P1 Start"])
				j1 |= 8;
			if (Controller["P1 Up"])
				j1 |= 16;
			if (Controller["P1 Down"])
				j1 |= 32;
			if (Controller["P1 Left"])
				j1 |= 64;
			if (Controller["P1 Right"])
				j1 |= 128;
			if (Controller["P2 A"])
				j2 |= 1;
			if (Controller["P2 B"])
				j2 |= 2;
			if (Controller["P2 Select"])
				j2 |= 4;
			if (Controller["P2 Start"])
				j2 |= 8;
			if (Controller["P2 Up"])
				j2 |= 16;
			if (Controller["P2 Down"])
				j2 |= 32;
			if (Controller["P2 Left"])
				j2 |= 64;
			if (Controller["P2 Right"])
				j2 |= 128;
		}

		#endregion

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			using (FP.Save())
			{
				if (Controller["Power"])
					LibQuickNES.qn_reset(Context, true);
				if (Controller["Reset"])
					LibQuickNES.qn_reset(Context, false);

				int j1, j2;
				SetPads(out j1, out j2);

				Frame++;
				LibQuickNES.ThrowStringError(LibQuickNES.qn_emulate_frame(Context, j1, j2));
				IsLagFrame = LibQuickNES.qn_get_joypad_read_count(Context) == 0;
				if (IsLagFrame)
					LagCount++;

				if (render)
					Blit();
				if (rendersound)
					DrainAudio();
			}
		}

		#region state

		IntPtr Context;

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; private set; }

		#endregion

		public string SystemId { get { return "NES"; } }
		public bool DeterministicEmulation { get { return true; } }
		public string BoardName { get; private set; }

		#region saveram

		byte[] SaveRamBuff;

		void InitSaveRamBuff()
		{
			int size = 0;
			LibQuickNES.ThrowStringError(LibQuickNES.qn_battery_ram_size(Context, ref size));
			SaveRamBuff = new byte[size];
		}

		public byte[] CloneSaveRam()
		{
			LibQuickNES.ThrowStringError(LibQuickNES.qn_battery_ram_save(Context, SaveRamBuff, SaveRamBuff.Length));
			return (byte[])SaveRamBuff.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			LibQuickNES.ThrowStringError(LibQuickNES.qn_battery_ram_load(Context, data, data.Length));
		}

		public void ClearSaveRam()
		{
			LibQuickNES.ThrowStringError(LibQuickNES.qn_battery_ram_clear(Context));
		}

		public bool SaveRamModified
		{
			get
			{
				return LibQuickNES.qn_has_battery_ram(Context);
			}
			set
			{
				throw new Exception();
			}
		}

		#endregion

		public void ResetCounters()
		{
			Frame = 0;
			IsLagFrame = false;
			LagCount = 0;
		}

		#region savestates

		byte[] SaveStateBuff;
		byte[] SaveStateBuff2;

		void InitSaveStateBuff()
		{
			int size = 0;
			LibQuickNES.ThrowStringError(LibQuickNES.qn_state_size(Context, ref size));
			SaveStateBuff = new byte[size];
			SaveStateBuff2 = new byte[size + 13];
		}

		public void SaveStateText(System.IO.TextWriter writer)
		{
			var temp = SaveStateBinary();
			temp.SaveAsHexFast(writer);
			// write extra copy of stuff we don't use
			writer.WriteLine("Frame {0}", Frame);
		}

		public void LoadStateText(System.IO.TextReader reader)
		{
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			state.ReadFromHexFast(hex);
			LoadStateBinary(new System.IO.BinaryReader(new System.IO.MemoryStream(state)));
		}

		public void SaveStateBinary(System.IO.BinaryWriter writer)
		{
			LibQuickNES.ThrowStringError(LibQuickNES.qn_state_save(Context, SaveStateBuff, SaveStateBuff.Length));
			writer.Write(SaveStateBuff.Length);
			writer.Write(SaveStateBuff);
			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(System.IO.BinaryReader reader)
		{
			int len = reader.ReadInt32();
			if (len != SaveStateBuff.Length)
				throw new InvalidOperationException("Unexpected savestate buffer length!");
			reader.Read(SaveStateBuff, 0, SaveStateBuff.Length);
			LibQuickNES.ThrowStringError(LibQuickNES.qn_state_load(Context, SaveStateBuff, SaveStateBuff.Length));
			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}

		public byte[] SaveStateBinary()
		{
			var ms = new System.IO.MemoryStream(SaveStateBuff2, true);
			var bw = new System.IO.BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			if (ms.Position != SaveStateBuff2.Length)
				throw new InvalidOperationException("Unexpected savestate length!");
			bw.Close();
			return SaveStateBuff2;
		}

		public bool BinarySaveStatesPreferred { get { return true; } }

		#endregion

		public CoreComm CoreComm
		{
			get;
			private set;
		}

		#region debugging

		unsafe void InitMemoryDomains()
		{
			List<MemoryDomain> mm = new List<MemoryDomain>();
			for (int i = 0; ; i++)
			{
				IntPtr data = IntPtr.Zero;
				int size = 0;
				bool writable = false;
				IntPtr name = IntPtr.Zero;

				if (!LibQuickNES.qn_get_memory_area(Context, i, ref data, ref size, ref writable, ref name))
					break;

				if (data != IntPtr.Zero && size > 0 && name != IntPtr.Zero)
				{
					mm.Add(MemoryDomain.FromIntPtr(Marshal.PtrToStringAnsi(name), size, MemoryDomain.Endian.Little, data, writable));
				}
			}
			// add system bus
			mm.Add(new MemoryDomain
			(
				"System Bus",
				0x10000,
				MemoryDomain.Endian.Unknown,
				delegate(int addr)
				{
					if (addr < 0 || addr >= 0x10000)
						throw new ArgumentOutOfRangeException();
					return LibQuickNES.qn_peek_prgbus(Context, addr);
				},
				delegate(int addr, byte val)
				{
					if (addr < 0 || addr >= 0x10000)
						throw new ArgumentOutOfRangeException();
					LibQuickNES.qn_poke_prgbus(Context, addr, val);
				}
			));
			MemoryDomains = new MemoryDomainList(mm, 0);
		}

		public MemoryDomainList MemoryDomains { get; private set; }

		public Dictionary<string, int> GetCpuFlagsAndRegisters()
		{
			int[] regs = new int[6];
			var ret = new Dictionary<string, int>();
			LibQuickNES.qn_get_cpuregs(Context, regs);
			ret["A"] = regs[0];
			ret["X"] = regs[1];
			ret["Y"] = regs[2];
			ret["SP"] = regs[3];
			ret["PC"] = regs[4];
			ret["P"] = regs[5];
			return ret;
		}

		public void SetCpuRegister(string register, int value)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region bootgod

		public RomStatus? BootGodStatus { get; private set; }
		public string BootGodName { get; private set; }

		void ComputeBootGod()
		{
			// inefficient, sloppy, etc etc
			Emulation.Cores.Nintendo.NES.NES.BootGodDB.Initialize(); 
			var chrrom = MemoryDomains["CHR VROM"];
			var prgrom = MemoryDomains["PRG ROM"];

			var ms = new System.IO.MemoryStream();
			for (int i = 0; i < prgrom.Size; i++)
				ms.WriteByte(prgrom.PeekByte(i));
			if (chrrom != null)
				for (int i = 0; i < chrrom.Size; i++)
					ms.WriteByte(chrrom.PeekByte(i));

			string sha1 = BizHawk.Common.BufferExtensions.BufferExtensions.HashSHA1(ms.ToArray());
			Console.WriteLine("Hash for BootGod: {0}", sha1);
			sha1 = "sha1:" + sha1; // huh?
			var carts = Emulation.Cores.Nintendo.NES.NES.BootGodDB.Instance.Identify(sha1);
			if (carts.Count > 0)
			{
				Console.WriteLine("BootGod entry found: {0}", carts[0].name);
				switch (carts[0].system)
				{
					case "NES-PAL":
					case "NES-PAL-A":
					case "NES-PAL-B":
					case "Dendy":
						Console.WriteLine("Bad region {0}! Failing over...", carts[0].system);
						throw new UnsupportedGameException("Unsupported region!");
					default:
						break;
				}
				BootGodStatus = RomStatus.GoodDump;
				BootGodName = carts[0].name;
			}
			else
			{
				Console.WriteLine("No BootGod entry found.");
				BootGodStatus = null;
				BootGodName = null;
			}
		}

		#endregion

		#region settings

		public class QuickNESSettings
		{
			[DefaultValue(8)]
			[Description("Set the number of sprites visible per line.  0 hides all sprites, 8 behaves like a normal NES, and 64 is maximum.")]
			[DisplayName("Visible Sprites")]
			public int NumSprites
			{
				get { return _NumSprites; }
				set { _NumSprites = Math.Min(64, Math.Max(0, value)); }
			}
			[JsonIgnore]
			private int _NumSprites;

			[DefaultValue(false)]
			[Description("Clip the left and right 8 pixels of the display, which sometimes contain nametable garbage.")]
			[DisplayName("Clip Left and Right")]
			public bool ClipLeftAndRight { get; set; }

			[DefaultValue(false)]
			[Description("Clip the top and bottom 8 pixels of the display, which sometimes contain nametable garbage.")]
			[DisplayName("Clip Top and Bottom")]
			public bool ClipTopAndBottom { get; set; }

			[Browsable(false)]
			public byte[] Palette
			{
				get { return _Palette; }
				set
				{
					if (value == null)
						throw new ArgumentNullException();
					else if (value.Length == 64 * 8 * 3)
						_Palette = value;
					else
						throw new ArgumentOutOfRangeException();
				}
			}
			[JsonIgnore]
			private byte[] _Palette;

			public QuickNESSettings Clone()
			{
				var ret = (QuickNESSettings)MemberwiseClone();
				ret._Palette = (byte[])_Palette.Clone();
				return ret;
			}
			public QuickNESSettings()
			{
				SettingsUtil.SetDefaultValues(this);
				SetDefaultColors();
			}

			public void SetNesHawkPalette(int[,] pal)
			{
				if (pal.GetLength(0) != 64 || pal.GetLength(1) != 3)
					throw new ArgumentOutOfRangeException();
				for (int c = 0; c < 512; c++)
				{
					int a = c & 63;
					byte[] inp = { (byte)pal[a, 0], (byte)pal[a, 1], (byte)pal[a, 2] };
					byte[] outp = new byte[3];
					Nes_NTSC_Colors.Emphasis(inp, outp, c);
					_Palette[c * 3] = outp[0];
					_Palette[c * 3 + 1] = outp[1];
					_Palette[c * 3 + 2] = outp[2];
				}
			}

			static byte[] GetDefaultColors()
			{
				IntPtr src = LibQuickNES.qn_get_default_colors();
				byte[] ret = new byte[1536];
				Marshal.Copy(src, ret, 0, 1536);
				return ret;
			}

			public void SetDefaultColors()
			{
				_Palette = GetDefaultColors();
			}
		}

		QuickNESSettings _Settings;

		// what is this for?
		public class QuickNESSyncSettings
		{
			public QuickNESSyncSettings Clone()
			{
				return new QuickNESSyncSettings();
			}
		}

		public QuickNESSettings GetSettings()
		{
			return _Settings.Clone();
		}

		public QuickNESSyncSettings GetSyncSettings()
		{
			return new QuickNESSyncSettings();
		}

		public bool PutSettings(QuickNESSettings o)
		{
			_Settings = o;
			LibQuickNES.qn_set_sprite_limit(Context, _Settings.NumSprites);
			RecalculateCrops();
			CalculatePalette();
			return false;
		}

		public bool PutSyncSettings(QuickNESSyncSettings o)
		{
			return false;
		}

		#endregion

		public void Dispose()
		{
			if (Context != IntPtr.Zero)
			{
				LibQuickNES.qn_delete(Context);
				Context = IntPtr.Zero;
			}
		}

		#region VideoProvider

		int[] VideoOutput = new int[256 * 240];
		int[] VideoPalette = new int[512];

		int cropleft = 0;
		int cropright = 0;
		int croptop = 0;
		int cropbottom = 0;

		void RecalculateCrops()
		{
			cropright = cropleft = _Settings.ClipLeftAndRight ? 8 : 0;
			cropbottom = croptop = _Settings.ClipTopAndBottom ? 8 : 0;
			BufferWidth = 256 - cropleft - cropright;
			BufferHeight = 240 - croptop - cropbottom;
		}

		void CalculatePalette()
		{
			for (int i = 0; i < 512; i++)
			{
				VideoPalette[i] =
					_Settings.Palette[i * 3] << 16 |
					_Settings.Palette[i * 3 + 1] << 8 |
					_Settings.Palette[i * 3 + 2] |
					unchecked((int)0xff000000);
			}
		}

		void Blit()
		{
			LibQuickNES.qn_blit(Context, VideoOutput, VideoPalette, cropleft, croptop, cropright, cropbottom);
		}

		public IVideoProvider VideoProvider { get { return this; } }
		public int[] GetVideoBuffer() { return VideoOutput; }
		public int VirtualWidth { get { return (int)(BufferWidth * 1.146); } }
		public int VirtualHeight { get { return BufferHeight; } }
		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		#endregion

		#region SoundProvider

		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return this; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		void InitAudio()
		{
			LibQuickNES.ThrowStringError(LibQuickNES.qn_set_sample_rate(Context, 44100));
		}

		void DrainAudio()
		{
			NumSamples = LibQuickNES.qn_read_audio(Context, MonoBuff, MonoBuff.Length);
			unsafe
			{
				fixed (short* _src = &MonoBuff[0], _dst = &StereoBuff[0])
				{
					short* src = _src;
					short* dst = _dst;
					for (int i = 0; i < NumSamples; i++)
					{
						*dst++ = *src;
						*dst++ = *src++;
					}
				}
			}
		}

		short[] MonoBuff = new short[1024];
		short[] StereoBuff = new short[2048];
		int NumSamples = 0;

		public void GetSamples(out short[] samples, out int nsamp)
		{
			samples = StereoBuff;
			nsamp = NumSamples;
		}

		public void DiscardSamples()
		{
		}

		#endregion
	}
}
