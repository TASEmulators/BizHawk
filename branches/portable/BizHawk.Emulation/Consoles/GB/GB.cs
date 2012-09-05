using System;
using System.Collections.Generic;
using BizHawk.Emulation.CPUs.Z80GB;

/*
This Game Boy core was written using Imran Nazar's "GameBoy Emulation in
Javascript" series (http://imrannazar.com/GameBoy-Emulation-in-JavaScript) and
contains several comments from the articles.
*/
namespace BizHawk.Emulation.Consoles.GB
{
	public partial class GB : IEmulator, IVideoProvider
	{
		private Z80 CPU;
		private int lagCount = 0;
		private bool isLagFrame = false;
		private IList<MemoryDomain> memoryDomains = new List<MemoryDomain>();

		public GB(GameInfo game, byte[] rom, bool skipBIOS)
		{
			inBIOS = !skipBIOS;
			HardReset();
		}
        public int VirtualWidth { get { return 160; } }
		public int BufferWidth { get { return 160; } }
		public int BufferHeight { get { return 144; } }
		public int BackgroundColor { get { return 0; } }
		public CoreInputComm CoreInputComm { get; set; }
		public CoreOutputComm CoreOutputComm { get; private set; }
		public bool DeterministicEmulation { get; set; }
		public void Dispose() { }
		public int Frame { get; set; }

		public void FrameAdvance(bool render)
		{
			throw new NotImplementedException();
		}

		public void HardReset()
		{
			CPU = new CPUs.Z80GB.Z80();
			CPU.ReadMemory = ReadMemory;
			CPU.WriteMemory = WriteMemory;
		}

		public int[] GetVideoBuffer()
		{
			throw new NotImplementedException();
		}

		public IList<MemoryDomain> MemoryDomains { get { return memoryDomains; } }
		public bool IsLagFrame { get { return isLagFrame; } }
		public int LagCount { get { return lagCount; } set { lagCount = value; } }
		public void LoadStateBinary(System.IO.BinaryReader reader)
		{
			throw new NotImplementedException();
		}

		public void LoadStateText(System.IO.TextReader reader)
		{
			throw new NotImplementedException();
		}

		public MemoryDomain MainMemory { get { return memoryDomains[0]; } }

		public void ResetFrameCounter()
		{
			Frame = 0;
		}

		public byte[] ReadSaveRam { get { throw new NotImplementedException(); } }
		public bool SaveRamModified { get { return false; } set { } }

		public void SaveStateBinary(System.IO.BinaryWriter writer)
		{
			throw new NotImplementedException();
		}

		public byte[] SaveStateBinary()
		{
			return new byte[0];
		}

		public void SaveStateText(System.IO.TextWriter writer)
		{
			throw new NotImplementedException();
		}

		public ISoundProvider SoundProvider { get { return new NullEmulator(); } }
		public string SystemId { get { return "GB"; } }
		public IVideoProvider VideoProvider { get { return this; } }
	}
}
