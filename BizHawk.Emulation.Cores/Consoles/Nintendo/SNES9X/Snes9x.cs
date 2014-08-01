using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES9X
{
	[CoreAttributes("Snes9x", "FIXME", true, false, "5e0319ab3ef9611250efb18255186d0dc0d7e125", "https://github.com/snes9xgit/snes9x")]
	public class Snes9x : IEmulator, IVideoProvider, ISyncSoundProvider
	{
		#region controller

		public ControllerDefinition ControllerDefinition
		{
			get { return NullEmulator.NullController; }
		}

		public IController Controller { get; set; }

		#endregion

		public void Dispose()
		{
		}

		public Snes9x(CoreComm NextComm, byte[] rom)
		{
			if (!LibSnes9x.debug_init(rom, rom.Length))
				throw new Exception();

			CoreComm = NextComm;
		}

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			Frame++;

			LibSnes9x.debug_advance(_vbuff);

			if (IsLagFrame)
				LagCount++;
		}

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get { return true; } }

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
		}

		public string SystemId { get { return "SNES"; } }
		public bool DeterministicEmulation { get { return true; } }
		public string BoardName { get { return null; } }
		public CoreComm CoreComm { get; private set; }

		#region saveram

		public byte[] ReadSaveRam()
		{
			return new byte[0];
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
				throw new NotImplementedException();
			}
		}

		#endregion

		#region savestates

		public void SaveStateText(System.IO.TextWriter writer)
		{
		}

		public void LoadStateText(System.IO.TextReader reader)
		{
		}

		public void SaveStateBinary(System.IO.BinaryWriter writer)
		{
		}

		public void LoadStateBinary(System.IO.BinaryReader reader)
		{
		}

		public byte[] SaveStateBinary()
		{
			return new byte[0];
		}

		public bool BinarySaveStatesPreferred { get { return true; } }

		#endregion

		#region debugging

		public MemoryDomainList MemoryDomains
		{
			get { throw new NotImplementedException(); }
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

		#region settings

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

		private int[] _vbuff = new int[512 * 480];
		public IVideoProvider VideoProvider { get { return this; } }
		public int[] GetVideoBuffer() { return _vbuff; }
		public int VirtualWidth
		{ get { return BufferWidth; } }
		public int VirtualHeight { get { return BufferHeight; } }
		public int BufferWidth { get { return 256; } }
		public int BufferHeight { get { return 224; } }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		#endregion

		#region ISyncSoundProvider

		private short[] _sbuff = new short[2048];
		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return this; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		public void GetSamples(out short[] samples, out int nsamp)
		{
			samples = _sbuff;
			nsamp = 735;
		}

		public void DiscardSamples()
		{
		}

		#endregion
	}
}
