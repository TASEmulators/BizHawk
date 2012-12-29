using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BizHawk.Emulation.Consoles.GB
{
	public class GambatteLink : IEmulator, IVideoProvider, ISyncSoundProvider
	{
		Gameboy L;
		Gameboy R;

		Consoles.Nintendo.SNES.LibsnesCore.SnesSaveController LCont = new Nintendo.SNES.LibsnesCore.SnesSaveController(Gameboy.GbController);
		Consoles.Nintendo.SNES.LibsnesCore.SnesSaveController RCont = new Nintendo.SNES.LibsnesCore.SnesSaveController(Gameboy.GbController);

		public GambatteLink(CoreComm comm, GameInfo leftinfo, byte[] leftrom, GameInfo rightinfo, byte[] rightrom)
		{
			CoreComm = comm;
			L = new Gameboy(new CoreComm(), leftinfo, leftrom);
			R = new Gameboy(new CoreComm(), rightinfo, rightrom);

			L.Controller = LCont;
			R.Controller = RCont;

			comm.VsyncNum = L.CoreComm.VsyncNum;
			comm.VsyncDen = L.CoreComm.VsyncDen;
			comm.RomStatusAnnotation = null;
			comm.RomStatusDetails = "LEFT:\r\n" + L.CoreComm.RomStatusDetails + "RIGHT:\r\n" + R.CoreComm.RomStatusDetails;
			comm.CpuTraceAvailable = false; // TODO
			comm.NominalWidth = L.CoreComm.NominalWidth + R.CoreComm.NominalWidth;
			comm.NominalHeight = L.CoreComm.NominalHeight;

			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public IVideoProvider VideoProvider { get { return this; } }
		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return this; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		public static readonly ControllerDefinition DualGbController = new ControllerDefinition
		{
			Name = "Dual Gameboy Controller",
			BoolButtons =
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 A", "P1 B", "P1 Select", "P1 Start", "P1 Power",
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 A", "P2 B", "P2 Select", "P2 Start", "P2 Power",
			}
		};

		public ControllerDefinition ControllerDefinition { get { return DualGbController; } }
		public IController Controller { get; set; }

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			LCont.Clear();
			RCont.Clear();

			foreach (var s in DualGbController.BoolButtons)
			{
				if (Controller[s])
				{
					if (s.Contains("P1 "))
						LCont.Set(s.Replace("P1 ", ""));
					else if (s.Contains("P2 "))
						RCont.Set(s.Replace("P2 ", ""));
				}
			}

			Frame++;
			L.FrameAdvance(render, rendersound);
			R.FrameAdvance(render, rendersound);
			IsLagFrame = L.IsLagFrame && R.IsLagFrame;
			if (IsLagFrame)
				LagCount++;
			BlitFrameBuffer();
		}

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; private set; }
		public string SystemId { get { return "DGB"; } }
		public bool DeterministicEmulation { get { return true; } }

		#region saveram

		public byte[] ReadSaveRam()
		{
			byte[] lb = L.ReadSaveRam();
			byte[] rb = R.ReadSaveRam();
			byte[] ret = new byte[lb.Length + rb.Length];
			Buffer.BlockCopy(lb, 0, ret, 0, lb.Length);
			Buffer.BlockCopy(rb, 0, ret, lb.Length, rb.Length);
			return ret;
		}

		public void StoreSaveRam(byte[] data)
		{
			byte[] lb = new byte[L.ReadSaveRam().Length];
			byte[] rb = new byte[R.ReadSaveRam().Length];
			Buffer.BlockCopy(data, 0, lb, 0, lb.Length);
			Buffer.BlockCopy(data, lb.Length, rb, 0, rb.Length);
			L.StoreSaveRam(lb);
			R.StoreSaveRam(rb);
		}

		public void ClearSaveRam()
		{
			L.ClearSaveRam();
			R.ClearSaveRam();
		}

		public bool SaveRamModified
		{
			get
			{
				return L.SaveRamModified || R.SaveRamModified;
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		#endregion

		public void ResetFrameCounter()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		#region savestates

		public void SaveStateText(TextWriter writer)
		{
			var temp = SaveStateBinary();
			temp.SaveAsHex(writer);
			// write extra copy of stuff we don't use
			writer.WriteLine("Frame {0}", Frame);
		}

		public void LoadStateText(TextReader reader)
		{
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			state.ReadFromHex(hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			L.SaveStateBinary(writer);
			R.SaveStateBinary(writer);
			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			L.LoadStateBinary(reader);
			R.LoadStateBinary(reader);
			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		#endregion

		public CoreComm CoreComm { get; private set; }

		public IList<MemoryDomain> MemoryDomains
		{
			get { throw new NotImplementedException(); }
		}

		public MemoryDomain MainMemory
		{
			get { throw new NotImplementedException(); }
		}

		public void Dispose()
		{
			if (L != null)
			{
				L.Dispose();
				L = null;
			}
			if (R != null)
			{
				R.Dispose();
				R = null;
			}
		}

		int[] VideoBuffer = new int[160 * 2 * 144];
		public int[] GetVideoBuffer() { return VideoBuffer; }
		public int VirtualWidth { get { return 320; } }
		public int BufferWidth { get { return 320; } }
		public int BufferHeight { get { return 144; } }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		void BlitFrameBuffer()
		{
			var lb = L.GetVideoBuffer();
			var rb = R.GetVideoBuffer();
			int destpos = 0;
			for (int y = 0; y < 144; y++)
			{
				Buffer.BlockCopy(lb, 160 * 4 * y, VideoBuffer, destpos, 160 * 4);
				destpos += 160 * 4;
				Buffer.BlockCopy(rb, 160 * 4 * y, VideoBuffer, destpos, 160 * 4);
				destpos += 160 * 4;
			}
		}


		public void GetSamples(out short[] samples, out int nsamp)
		{
			// TODO
			samples = new short[735 * 2];
			nsamp = 735;
		}

		public void DiscardSamples()
		{
			// TODO
		}
	}
}
