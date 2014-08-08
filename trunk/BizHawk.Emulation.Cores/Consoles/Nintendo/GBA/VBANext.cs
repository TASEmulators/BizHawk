using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	[CoreAttributes("VBA-Next", "TODO", true, false, "cd508312a29ed8c29dacac1b11c2dce56c338a54", "https://github.com/libretro/vba-next")]
	public class VBANext : IEmulator, IVideoProvider, ISyncSoundProvider
	{
		IntPtr Core;

		public VBANext(byte[] romfile, CoreComm nextComm)
		{
			CoreComm = nextComm;
			byte[] biosfile = CoreComm.CoreFileProvider.GetFirmware("GBA", "Bios", true, "GBA bios file is mandatory.");

			if (romfile.Length > 32 * 1024 * 1024)
				throw new ArgumentException("ROM is too big to be a GBA ROM!");
			if (biosfile.Length != 16 * 1024)
				throw new ArgumentException("BIOS file is not exactly 16K!");

			Core = LibVBANext.Create();
			if (Core == IntPtr.Zero)
				throw new InvalidOperationException("Create() returned nullptr!");
			try
			{
				if (!LibVBANext.LoadRom(Core, romfile, (uint)romfile.Length, biosfile, (uint)biosfile.Length))
					throw new InvalidOperationException("LoadRom() returned false!");
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

			IsLagFrame = LibVBANext.FrameAdvance(Core, GetButtons(), videobuff, soundbuff, out numsamp);

			if (IsLagFrame)
				LagCount++;
		}

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; private set; }

		public string SystemId { get { return "GBA"; } }

		public bool DeterministicEmulation { get { return true; } }

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public string BoardName
		{
			// could we do something useful here?
			get { return "BAMBOOZLED!"; }
		}

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

		public byte[] ReadSaveRam()
		{
			return new byte[16];
		}

		public void StoreSaveRam(byte[] data)
		{
		}

		public void ClearSaveRam()
		{
		}

		public bool SaveRamModified
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		#endregion

		#region SaveStates

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
			return new byte[16];
		}

		public bool BinarySaveStatesPreferred
		{
			get { return true; }
		}

		#endregion

		#region Debugging

		public MemoryDomainList MemoryDomains
		{
			get { return MemoryDomainList.GetDummyList(); }
		}

		public Dictionary<string, int> GetCpuFlagsAndRegisters()
		{
			throw new NotImplementedException();
		}

		public void SetCpuRegister(string register, int value)
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

		public IVideoProvider VideoProvider { get { return this; } }
		public int[] GetVideoBuffer() { return videobuff; }
		public int VirtualWidth { get { return 240; } }
		public int VirtualHeight { get { return 160; } }
		public int BufferWidth { get { return 240; } }
		public int BufferHeight { get { return 160; } }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

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
