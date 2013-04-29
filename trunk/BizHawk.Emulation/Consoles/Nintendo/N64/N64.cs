using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace BizHawk.Emulation.Consoles.Nintendo.N64
{
	public class N64 : IEmulator, IVideoProvider, ISoundProvider
	{
		public string SystemId { get { return "N64"; } }

		public CoreComm CoreComm { get; private set; }
		public byte[] rom;
		public GameInfo game;

		public IVideoProvider VideoProvider { get { return this; } }
		public int[] frameBuffer = new int[640 * 480];
		public int[] GetVideoBuffer() { return frameBuffer; }
		public int VirtualWidth { get { return 640; } }
		public int BufferWidth { get { return 640; } }
		public int BufferHeight { get { return 480; } }
		public int BackgroundColor { get { return 0; } }

		public ISoundProvider SoundProvider { get { return this; } }
		public void GetSamples(short[] samples) { }
		public void DiscardSamples() { }
		public int MaxVolume { get; set; }
		public ISyncSoundProvider SyncSoundProvider { get { return null; } }
		public bool StartAsyncSound() { return true; }
		public void EndAsyncSound() { }

		public ControllerDefinition ControllerDefinition { get { return N64ControllerDefinition; } }
		public IController Controller { get; set; }
		public static readonly ControllerDefinition N64ControllerDefinition = new ControllerDefinition
		{
			Name = "Nintento 64 Controller",
			BoolButtons =
			{
				"DPad R", "DPad L", "DPad D", "DPad U", "Start", "Z", "B", "A", "C Right", "C Left", "C Down", "C Up", "R", "L"
			}
		};

		public int Frame { get; set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get { return true; } }
		public void ResetFrameCounter() { }
		public void FrameAdvance(bool render, bool rendersound) { Frame++; }

		public bool DeterministicEmulation { get; set; }

		public byte[] ReadSaveRam() { return null; }
		public void StoreSaveRam(byte[] data) { }
		public void ClearSaveRam() { }
		public bool SaveRamModified { get; set; }

		void SyncState(Serializer ser)
		{
			ser.BeginSection("N64");
			ser.EndSection();
		}

		public void SaveStateText(TextWriter writer) { SyncState(Serializer.CreateTextWriter(writer)); }
		public void LoadStateText(TextReader reader) { SyncState(Serializer.CreateTextReader(reader)); }
		public void SaveStateBinary(BinaryWriter bw) { SyncState(Serializer.CreateBinaryWriter(bw)); }
		public void LoadStateBinary(BinaryReader br) { SyncState(Serializer.CreateBinaryReader(br)); }
		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		public IList<MemoryDomain> MemoryDomains { get { return null; } }
		public MemoryDomain MainMemory { get { return null; } }

		public void Dispose() { }

		public N64(CoreComm comm, GameInfo game, byte[] rom)
		{
			CoreComm = comm;
			this.rom = rom;
			this.game = game;
		}
	}
}
