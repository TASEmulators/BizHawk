using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using System.IO;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.WonderSwan
{
	[CoreAttributes("Cygne/Mednafen", "Dox", true, true, "0.9.36.5", "http://mednafen.sourceforge.net/")]
	public class WonderSwan : IEmulator, IVideoProvider, ISyncSoundProvider, IMemoryDomains, ISaveRam, IStatable,
		IInputPollable, IDebuggable, ISettable<WonderSwan.Settings, WonderSwan.SyncSettings>
	{
		#region Controller

		public static readonly ControllerDefinition WonderSwanController = new ControllerDefinition
		{
			Name = "WonderSwan Controller",
			BoolButtons =
			{
				"P1 X1",
				"P1 X2",
				"P1 X3",
				"P1 X4",
				"P1 Y1",
				"P1 Y2",
				"P1 Y3",
				"P1 Y4",
				"P1 Start",
				"P1 B",
				"P1 A",

				"P2 X1",
				"P2 X2",
				"P2 X3",
				"P2 X4",
				"P2 Y1",
				"P2 Y2",
				"P2 Y3",
				"P2 Y4",
				"P2 Start",
				"P2 B",
				"P2 A",

				"Power",				
				"Rotate"
			}
		};
		public ControllerDefinition ControllerDefinition { get { return WonderSwanController; } }
		public IController Controller { get; set; }

		BizSwan.Buttons GetButtons()
		{
			BizSwan.Buttons ret = 0;
			if (Controller["P1 X1"]) ret |= BizSwan.Buttons.X1;
			if (Controller["P1 X2"]) ret |= BizSwan.Buttons.X2;
			if (Controller["P1 X3"]) ret |= BizSwan.Buttons.X3;
			if (Controller["P1 X4"]) ret |= BizSwan.Buttons.X4;
			if (Controller["P1 Y1"]) ret |= BizSwan.Buttons.Y1;
			if (Controller["P1 Y2"]) ret |= BizSwan.Buttons.Y2;
			if (Controller["P1 Y3"]) ret |= BizSwan.Buttons.Y3;
			if (Controller["P1 Y4"]) ret |= BizSwan.Buttons.Y4;
			if (Controller["P1 Start"]) ret |= BizSwan.Buttons.Start;
			if (Controller["P1 B"]) ret |= BizSwan.Buttons.B;
			if (Controller["P1 A"]) ret |= BizSwan.Buttons.A;

			if (Controller["P2 X1"]) ret |= BizSwan.Buttons.R_X1;
			if (Controller["P2 X2"]) ret |= BizSwan.Buttons.R_X2;
			if (Controller["P2 X3"]) ret |= BizSwan.Buttons.R_X3;
			if (Controller["P2 X4"]) ret |= BizSwan.Buttons.R_X4;
			if (Controller["P2 Y1"]) ret |= BizSwan.Buttons.R_Y1;
			if (Controller["P2 Y2"]) ret |= BizSwan.Buttons.R_Y2;
			if (Controller["P2 Y3"]) ret |= BizSwan.Buttons.R_Y3;
			if (Controller["P2 Y4"]) ret |= BizSwan.Buttons.R_Y4;
			if (Controller["P2 Start"]) ret |= BizSwan.Buttons.R_Start;
			if (Controller["P2 B"]) ret |= BizSwan.Buttons.R_B;
			if (Controller["P2 A"]) ret |= BizSwan.Buttons.R_A;

			if (Controller["Rotate"]) ret |= BizSwan.Buttons.Rotate;
			return ret;
		}

		#endregion

		[CoreConstructor("WSWAN")]
		[ServiceNotApplicable(typeof(IDriveLight))]
		public WonderSwan(CoreComm comm, byte[] file, bool deterministic, object Settings, object SyncSettings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			CoreComm = comm;
			_Settings = (Settings)Settings ?? new Settings();
			_SyncSettings = (SyncSettings)SyncSettings ?? new SyncSettings();
			
			DeterministicEmulation = deterministic; // when true, remember to force the RTC flag!
			Core = BizSwan.bizswan_new();
			if (Core == IntPtr.Zero)
				throw new InvalidOperationException("bizswan_new() returned NULL!");
			try
			{
				var ss = _SyncSettings.GetNativeSettings();
				if (deterministic)
					ss.userealtime = false;

				bool rotate = false;

				if (!BizSwan.bizswan_load(Core, file, file.Length, ref ss, ref rotate))
					throw new InvalidOperationException("bizswan_load() returned FALSE!");

				CoreComm.VsyncNum = 3072000; // master CPU clock, also pixel clock
				CoreComm.VsyncDen = (144 + 15) * (224 + 32); // 144 vislines, 15 vblank lines; 224 vispixels, 32 hblank pixels

				saverambuff = new byte[BizSwan.bizswan_saveramsize(Core)];

				InitVideo(rotate);
				PutSettings(_Settings);
				SetMemoryDomains();

				savebuff = new byte[BizSwan.bizswan_binstatesize(Core)];
				savebuff2 = new byte[savebuff.Length + 13];

				InitDebugCallbacks();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public ITracer Tracer
		{
			[FeatureNotImplemented]
			get
			{
				throw new NotImplementedException();
			}
		}

		public void Dispose()
		{
			if (Core != IntPtr.Zero)
			{
				BizSwan.bizswan_delete(Core);
				Core = IntPtr.Zero;
			}
		}

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			Frame++;
			IsLagFrame = true;

			if (Controller["Power"])
				BizSwan.bizswan_reset(Core);

			bool rotate = false;
			int soundbuffsize = sbuff.Length;
			IsLagFrame = BizSwan.bizswan_advance(Core, GetButtons(), !render, vbuff, sbuff, ref soundbuffsize, ref rotate);
			if (soundbuffsize == sbuff.Length)
				throw new Exception();
			sbuffcontains = soundbuffsize;
			InitVideo(rotate);

			if (IsLagFrame)
				LagCount++;
		}

		public CoreComm CoreComm { get; private set; }

		public void ResetCounters()
		{
			Frame = 0;
			IsLagFrame = false;
			LagCount = 0;
		}

		IntPtr Core;

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; private set; }

		public string SystemId { get { return "WSWAN"; } }
		public bool DeterministicEmulation { get; private set; }
		public string BoardName { get { return null; } }

		#region SaveRam

		byte[] saverambuff;

		public byte[] CloneSaveRam()
		{
			if (!BizSwan.bizswan_saveramsave(Core, saverambuff, saverambuff.Length))
				throw new InvalidOperationException("bizswan_saveramsave() returned false!");
			return (byte[])saverambuff.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			if (!BizSwan.bizswan_saveramload(Core, data, data.Length))
				throw new InvalidOperationException("bizswan_saveramload() returned false!");
		}

		public bool SaveRamModified
		{
			get { return BizSwan.bizswan_saveramsize(Core) > 0; }
		}

		#endregion

		#region Savestates

		JsonSerializer ser = new JsonSerializer() { Formatting = Formatting.Indented };

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
			BizSwan.bizswan_txtstatesave(Core, ref ff);
			s.ExtraData.IsLagFrame = IsLagFrame;
			s.ExtraData.LagCount = LagCount;
			s.ExtraData.Frame = Frame;

			ser.Serialize(writer, s);
			// write extra copy of stuff we don't use
			writer.WriteLine();
			writer.WriteLine("Frame {0}", Frame);
		}

		public void LoadStateText(TextReader reader)
		{
			var s = (TextState<TextStateData>)ser.Deserialize(reader, typeof(TextState<TextStateData>));
			s.Prepare();
			var ff = s.GetFunctionPointersLoad();
			BizSwan.bizswan_txtstateload(Core, ref ff);
			IsLagFrame = s.ExtraData.IsLagFrame;
			LagCount = s.ExtraData.LagCount;
			Frame = s.ExtraData.Frame;
		}

		byte[] savebuff;
		byte[] savebuff2;

		public void SaveStateBinary(BinaryWriter writer)
		{
			if (!BizSwan.bizswan_binstatesave(Core, savebuff, savebuff.Length))
				throw new InvalidOperationException("bizswan_binstatesave() returned false!");
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
			if (!BizSwan.bizswan_binstateload(Core, savebuff, savebuff.Length))
				throw new InvalidOperationException("bizswan_binstateload() returned false!");

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

		unsafe void SetMemoryDomains()
		{
			var mmd = new List<MemoryDomain>();
			for (int i = 0; ; i++)
			{
				IntPtr name;
				int size;
				IntPtr data;
				if (!BizSwan.bizswan_getmemoryarea(Core, i, out name, out size, out data))
					break;
				if (size == 0)
					continue;
				string sname = Marshal.PtrToStringAnsi(name);
				mmd.Add(MemoryDomain.FromIntPtr(sname, size, MemoryDomain.Endian.Little, data));
			}
			MemoryDomains = new MemoryDomainList(mmd, 0);
		}

		private readonly InputCallbackSystem _inputCallbacks = new InputCallbackSystem();
		public IInputCallbackSystem InputCallbacks { get { return _inputCallbacks; } }

		private readonly MemoryCallbackSystem _memorycallbacks = new MemoryCallbackSystem();
		public IMemoryCallbackSystem MemoryCallbacks { get { return _memorycallbacks; } }

		public MemoryDomainList MemoryDomains { get; private set; }
	
		public IDictionary<string, int> GetCpuFlagsAndRegisters()
		{
			var ret = new Dictionary<string, int>();
			for (int i = (int)BizSwan.NecRegsMin; i <= (int)BizSwan.NecRegsMax; i++)
			{
				BizSwan.NecRegs en = (BizSwan.NecRegs)i;
				uint val = BizSwan.bizswan_getnecreg(Core, en);
				ret[Enum.GetName(typeof(BizSwan.NecRegs), en)] = (int)val;
			}
			return ret;
		}

		[FeatureNotImplemented]
		public void SetCpuRegister(string register, int value)
		{
			throw new NotImplementedException();
		}

		BizSwan.MemoryCallback ReadCallbackD;
		BizSwan.MemoryCallback WriteCallbackD;
		BizSwan.MemoryCallback ExecCallbackD;
		BizSwan.ButtonCallback ButtonCallbackD;

		void ReadCallback(uint addr)
		{
			MemoryCallbacks.CallReads(addr);
		}
		void WriteCallback(uint addr)
		{
			MemoryCallbacks.CallWrites(addr);
		}
		void ExecCallback(uint addr)
		{
			MemoryCallbacks.CallExecutes(addr);
		}
		void ButtonCallback()
		{
			InputCallbacks.Call();
		}

		void InitDebugCallbacks()
		{
			ReadCallbackD = new BizSwan.MemoryCallback(ReadCallback);
			WriteCallbackD = new BizSwan.MemoryCallback(WriteCallback);
			ExecCallbackD = new BizSwan.MemoryCallback(ExecCallback);
			ButtonCallbackD = new BizSwan.ButtonCallback(ButtonCallback);
			_inputCallbacks.ActiveChanged += SetInputCallback;
			_memorycallbacks.ActiveChanged += SetMemoryCallbacks;
		}

		void SetInputCallback()
		{
			BizSwan.bizswan_setbuttoncallback(Core, InputCallbacks.Any() ? ButtonCallbackD : null);
		}

		void SetMemoryCallbacks()
		{
			BizSwan.bizswan_setmemorycallbacks(Core,
				MemoryCallbacks.HasReads ? ReadCallbackD : null,
				MemoryCallbacks.HasWrites ? WriteCallbackD : null,
				MemoryCallbacks.HasExecutes ? ExecCallbackD : null);
		}

		#endregion

		#region Settings

		Settings _Settings;
		SyncSettings _SyncSettings;

		public class Settings
		{
			[DisplayName("Background Layer")]
			[Description("True to display the selected layer.")]
			[DefaultValue(true)]
			public bool EnableBG { get; set; }

			[DisplayName("Foreground Layer")]
			[Description("True to display the selected layer.")]
			[DefaultValue(true)]
			public bool EnableFG { get; set; }

			[DisplayName("Sprites Layer")]
			[Description("True to display the selected layer.")]
			[DefaultValue(true)]
			public bool EnableSprites { get; set; }

			public BizSwan.Settings GetNativeSettings()
			{
				var ret = new BizSwan.Settings();
				if (EnableBG) ret.LayerMask |= BizSwan.LayerFlags.BG;
				if (EnableFG) ret.LayerMask |= BizSwan.LayerFlags.FG;
				if (EnableSprites) ret.LayerMask |= BizSwan.LayerFlags.Sprite;
				return ret;
			}

			public Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public Settings Clone()
			{
				return (Settings)MemberwiseClone();
			}
		}

		public class SyncSettings
		{
			[DisplayName("Initial Time")]
			[Description("Initial time of emulation.  Only relevant when UseRealTime is false.")]
			[DefaultValue(typeof(DateTime), "2010-01-01")]
			public DateTime InitialTime { get; set; }
			
			[Description("Your birthdate.  Stored in EEPROM and used by some games.")]
			[DefaultValue(typeof(DateTime), "1968-05-13")]
			public DateTime BirthDate { get; set; }

			[Description("True to emulate a color system.")]
			[DefaultValue(true)]
			public bool Color { get; set; }

			[DisplayName("Use RealTime")]
			[Description("If true, RTC clock will be based off of real time instead of emulated time.  Ignored (set to false) when recording a movie.")]
			[DefaultValue(false)]
			public bool UseRealTime { get; set; }

			[Description("Your gender.  Stored in EEPROM and used by some games.")]
			[DefaultValue(BizSwan.Gender.Female)]
			public BizSwan.Gender Gender { get; set; }

			[Description("Language to play games in.  Most games ignore this.")]
			[DefaultValue(BizSwan.Language.Japanese)]
			public BizSwan.Language Language { get; set; }

			[DisplayName("Blood Type")]
			[Description("Your blood type.  Stored in EEPROM and used by some games.")]
			[DefaultValue(BizSwan.Bloodtype.AB)]
			public BizSwan.Bloodtype BloodType { get; set; }

			[Description("Your name.  Stored in EEPROM and used by some games.  Maximum of 16 characters")]
			[DefaultValue("Lady Ashelia")]
			public string Name { get; set; }

			public BizSwan.SyncSettings GetNativeSettings()
			{
				var ret = new BizSwan.SyncSettings
				{
					color = Color,
					userealtime = UseRealTime,
					sex = Gender,
					language = Language,
					blood = BloodType
				};
				ret.SetName(Name);
				ret.bday = (uint)BirthDate.Day;
				ret.bmonth = (uint)BirthDate.Month;
				ret.byear = (uint)BirthDate.Year;
				ret.initialtime = (ulong)((InitialTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
				return ret;
			}

			public SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public SyncSettings Clone()
			{
				return (SyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}

		public Settings GetSettings()
		{
			return _Settings.Clone();
		}

		public SyncSettings GetSyncSettings()
		{
			return _SyncSettings.Clone();
		}

		public bool PutSettings(Settings o)
		{
			_Settings = o;
			var native = _Settings.GetNativeSettings();
			BizSwan.bizswan_putsettings(Core, ref native);
			return false;
		}

		public bool PutSyncSettings(SyncSettings o)
		{
			bool ret = SyncSettings.NeedsReboot(o, _SyncSettings);
			_SyncSettings = o;
			return ret;
		}

		#endregion

		#region IVideoProvider

		public IVideoProvider VideoProvider { get { return this; } }

		void InitVideo(bool rotate)
		{
			if (rotate)
			{
				BufferWidth = 144;
				BufferHeight = 224;
			}
			else
			{
				BufferWidth = 224;
				BufferHeight = 144;
			}
		}

		private int[] vbuff = new int[224 * 144];

		public int[] GetVideoBuffer()
		{
			return vbuff;
		}

		public int VirtualWidth { get { return BufferWidth; } }
		public int VirtualHeight { get { return BufferHeight; } }
		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		#endregion

		#region ISoundProvider

		private short[] sbuff = new short[1536];
		private int sbuffcontains = 0;

		public ISoundProvider SoundProvider { get { throw new InvalidOperationException(); } }
		public ISyncSoundProvider SyncSoundProvider { get { return this; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		public void GetSamples(out short[] samples, out int nsamp)
		{
			samples = sbuff;
			nsamp = sbuffcontains;
		}

		public void DiscardSamples()
		{
			sbuffcontains = 0;
		}

		#endregion
	}
}
