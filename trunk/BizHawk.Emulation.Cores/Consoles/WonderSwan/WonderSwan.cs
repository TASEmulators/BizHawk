using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using System.IO;

namespace BizHawk.Emulation.Cores.WonderSwan
{
	[CoreAttributes("Mednafen/Cygne", "Dox", true, false)]
	public class WonderSwan : IEmulator, IVideoProvider, ISyncSoundProvider
	{
		#region Controller

		public static readonly ControllerDefinition WonderSwanController = new ControllerDefinition
		{
			Name = "WonderSwan Controller",
			BoolButtons = { "Up X", "Down X", "Left X", "Right X", "Up Y", "Down Y", "Left Y", "Right Y", "Start", "B", "A", "Power" }
		};
		public ControllerDefinition ControllerDefinition { get { return WonderSwanController; } }
		public IController Controller { get; set; }

		BizSwan.Buttons GetButtons()
		{
			BizSwan.Buttons ret = 0;
			if (Controller["Up X"]) ret |= BizSwan.Buttons.UpX;
			if (Controller["Down X"]) ret |= BizSwan.Buttons.DownX;
			if (Controller["Left X"]) ret |= BizSwan.Buttons.LeftX;
			if (Controller["Right X"]) ret |= BizSwan.Buttons.RightX;
			if (Controller["Up Y"]) ret |= BizSwan.Buttons.UpY;
			if (Controller["Down Y"]) ret |= BizSwan.Buttons.DownY;
			if (Controller["Left Y"]) ret |= BizSwan.Buttons.LeftY;
			if (Controller["Right Y"]) ret |= BizSwan.Buttons.RightY;
			if (Controller["Start"]) ret |= BizSwan.Buttons.Start;
			if (Controller["B"]) ret |= BizSwan.Buttons.B;
			if (Controller["A"]) ret |= BizSwan.Buttons.A;
			return ret;
		}

		#endregion

		public WonderSwan(CoreComm comm, byte[] rom, bool deterministicEmulation)
		{
			this.CoreComm = comm;
			DeterministicEmulation = deterministicEmulation; // when true, remember to force the RTC flag!
			Core = BizSwan.bizswan_new();
			if (Core == IntPtr.Zero)
				throw new InvalidOperationException("bizswan_new() returned NULL!");
			try
			{
				var ss = new BizSwan.SyncSettings
				{
					sex = BizSwan.Gender.Male,
					blood = BizSwan.Bloodtype.A,
					language = BizSwan.Language.Japanese,
					rotateinput = false, // TODO
					bday = 5,
					bmonth = 12,
					byear = 1968
				};
				ss.SetName("LaForge");

				if (!BizSwan.bizswan_load(Core, rom, rom.Length, ref ss))
					throw new InvalidOperationException("bizswan_load() returned FALSE!");

				CoreComm.VsyncNum = 3072000; // master CPU clock, also pixel clock
				CoreComm.VsyncDen = (144 + 15) * (224 + 32); // 144 vislines, 15 vblank lines; 224 vispixels, 32 hblank pixels

				saverambuff = new byte[BizSwan.bizswan_saveramsize(Core)];
			}
			catch
			{
				Dispose();
				throw;
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

			int soundbuffsize = sbuff.Length;
			BizSwan.bizswan_advance(Core, GetButtons(), !render, vbuff, sbuff, ref soundbuffsize);
			if (soundbuffsize == sbuff.Length)
				throw new Exception();
			sbuffcontains = soundbuffsize;

			IsLagFrame = false; // TODO
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

		public byte[] ReadSaveRam()
		{
			if (!BizSwan.bizswan_saveramsave(Core, saverambuff, saverambuff.Length))
				throw new InvalidOperationException("bizswan_saveramsave() returned false!");
			return saverambuff;
		}

		public void StoreSaveRam(byte[] data)
		{
			if (!BizSwan.bizswan_saveramload(Core, data, data.Length))
				throw new InvalidOperationException("bizswan_saveramload() returned false!");
		}

		public void ClearSaveRam()
		{
			throw new InvalidOperationException("A new core starts with a clear saveram.  Instantiate a new core if you want this.");
		}

		public bool SaveRamModified
		{
			get { return BizSwan.bizswan_saveramsize(Core) > 0; }
			set { throw new InvalidOperationException(); }
		}

		#endregion

		#region Savestates

		public void SaveStateText(TextWriter writer)
		{
		}

		public void LoadStateText(TextReader reader)
		{
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
		}

		public void LoadStateBinary(BinaryReader reader)
		{
		}

		public byte[] SaveStateBinary()
		{
			return new byte[0];
		}

		public bool BinarySaveStatesPreferred
		{
			get { return true; }
		}

		#endregion

		#region Debugging

		public MemoryDomainList MemoryDomains
		{
			get { throw new NotImplementedException(); }
		}

		public Dictionary<string, int> GetCpuFlagsAndRegisters()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Settings

		public object GetSettings()
		{
			return null;
		}

		public object GetSyncSettings()
		{
			return null;
		}

		public bool PutSettings(object o)
		{
			return false;
		}

		public bool PutSyncSettings(object o)
		{
			return false;
		}

		#endregion

		#region IVideoProvider

		public IVideoProvider VideoProvider { get { return this; } }

		private int[] vbuff = new int[224 * 144];

		public int[] GetVideoBuffer()
		{
			return vbuff;
		}

		public int VirtualWidth { get { return 224; } }
		public int VirtualHeight { get { return 144; } }
		public int BufferWidth { get { return 224; } }
		public int BufferHeight { get { return 144; } }
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
