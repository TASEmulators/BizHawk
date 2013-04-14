using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk.Emulation.Consoles.GB
{
	public class GambatteLink : IEmulator, IVideoProvider, ISyncSoundProvider
	{
		bool disposed = false;

		Gameboy L;
		Gameboy R;
		// counter to ensure we do 35112 samples per frame
		int overflowL = 0;
		int overflowR = 0;

		const int SampPerFrame = 35112;

		Consoles.Nintendo.SNES.LibsnesCore.SnesSaveController LCont = new Nintendo.SNES.LibsnesCore.SnesSaveController(Gameboy.GbController);
		Consoles.Nintendo.SNES.LibsnesCore.SnesSaveController RCont = new Nintendo.SNES.LibsnesCore.SnesSaveController(Gameboy.GbController);

		public GambatteLink(CoreComm comm, GameInfo leftinfo, byte[] leftrom, GameInfo rightinfo, byte[] rightrom)
		{
			CoreComm = comm;
			L = new Gameboy(new CoreComm(), leftinfo, leftrom);
			R = new Gameboy(new CoreComm(), rightinfo, rightrom);

			// connect link cable
			LibGambatte.gambatte_linkstatus(L.GambatteState, 259);
			LibGambatte.gambatte_linkstatus(R.GambatteState, 259);

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

			blip_left = new Sound.Utilities.BlipBuffer(1024);
			blip_right = new Sound.Utilities.BlipBuffer(1024);
			blip_left.SetRates(2097152 * 2, 44100);
			blip_right.SetRates(2097152 * 2, 44100);

			SetMemoryDomains();
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
			L.FrameAdvancePrep();
			R.FrameAdvancePrep();

			unsafe
			{
				fixed (int* leftvbuff = &VideoBuffer[0])
				{
					// use pitch to have both cores write to the same video buffer, interleaved
					int* rightvbuff = leftvbuff + 160;
					const int pitch = 160 * 2;

					fixed (short* leftsbuff = LeftBuffer, rightsbuff = RightBuffer)
					{

						const int step = 32; // could be 1024 for GB

						int nL = overflowL;
						int nR = overflowR;

						// slowly step our way through the frame, while continually checking and resolving link cable status
						for (int target = 0; target < SampPerFrame;)
						{
							target += step;
							if (target > SampPerFrame)
								target = SampPerFrame; // don't run for slightly too long depending on step

							// gambatte_runfor() aborts early when a frame is produced, but we don't want that, hence the while()
							while (nL < target)
							{
								uint nsamp = (uint)(target - nL);
								LibGambatte.gambatte_runfor(L.GambatteState, leftvbuff, pitch, leftsbuff + nL * 2, ref nsamp);
								nL += (int)nsamp;
							}
							while (nR < target)
							{
								uint nsamp = (uint)(target - nR);
								LibGambatte.gambatte_runfor(R.GambatteState, rightvbuff, pitch, rightsbuff + nR * 2, ref nsamp);
								nR += (int)nsamp;
							}
							// poll link cable statuses

							if (LibGambatte.gambatte_linkstatus(L.GambatteState, 256) != 0) // ClockTrigger
							{
								LibGambatte.gambatte_linkstatus(L.GambatteState, 257); // ack
								int lo = LibGambatte.gambatte_linkstatus(L.GambatteState, 258); // GetOut
								int ro = LibGambatte.gambatte_linkstatus(R.GambatteState, 258);
								LibGambatte.gambatte_linkstatus(L.GambatteState, ro & 0xff); // ShiftIn
								LibGambatte.gambatte_linkstatus(R.GambatteState, lo & 0xff); // ShiftIn
							}
							if (LibGambatte.gambatte_linkstatus(R.GambatteState, 256) != 0) // ClockTrigger
							{
								LibGambatte.gambatte_linkstatus(R.GambatteState, 257); // ack
								int lo = LibGambatte.gambatte_linkstatus(L.GambatteState, 258); // GetOut
								int ro = LibGambatte.gambatte_linkstatus(R.GambatteState, 258);
								LibGambatte.gambatte_linkstatus(L.GambatteState, ro & 0xff); // ShiftIn
								LibGambatte.gambatte_linkstatus(R.GambatteState, lo & 0xff); // ShiftIn
							}

						}
						overflowL = nL - SampPerFrame;
						overflowR = nR - SampPerFrame;
						if (overflowL < 0 || overflowR < 0)
							throw new Exception("Timing problem?");

						if (rendersound)
						{
							PrepSound();
						}
						// copy extra samples back to beginning
						for (int i = 0; i < overflowL * 2; i++)
							LeftBuffer[i] = LeftBuffer[i + SampPerFrame * 2];
						for (int i = 0; i < overflowR * 2; i++)
							RightBuffer[i] = RightBuffer[i + SampPerFrame * 2];
						
					}

				}
			}

			L.FrameAdvancePost();
			R.FrameAdvancePost();
			IsLagFrame = L.IsLagFrame && R.IsLagFrame;
			if (IsLagFrame)
				LagCount++;
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
			writer.Write(overflowL);
			writer.Write(overflowR);
			writer.Write(LatchL);
			writer.Write(LatchR);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			L.LoadStateBinary(reader);
			R.LoadStateBinary(reader);
			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
			overflowL = reader.ReadInt32();
			overflowR = reader.ReadInt32();
			LatchL = reader.ReadInt32();
			LatchR = reader.ReadInt32();
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

		public IList<MemoryDomain> MemoryDomains { get; private set; }
		public MemoryDomain MainMemory { get { return MemoryDomains[0]; } }

		void SetMemoryDomains()
		{
			var mm = new List<MemoryDomain>();

			foreach (var md in L.MemoryDomains)
				mm.Add(new MemoryDomain("L " + md.Name, md.Size, md.Endian, md.PeekByte, md.PokeByte));
			foreach (var md in R.MemoryDomains)
				mm.Add(new MemoryDomain("R " + md.Name, md.Size, md.Endian, md.PeekByte, md.PokeByte));

			MemoryDomains = mm.AsReadOnly();
		}

		public void Dispose()
		{
			if (!disposed)
			{
				L.Dispose();
				L = null;
				R.Dispose();
				R = null;
				blip_left.Dispose();
				blip_left = null;
				blip_right.Dispose();
				blip_right = null;

				disposed = true;
			}
		}

		#region VideoProvider

		int[] VideoBuffer = new int[160 * 2 * 144];
		public int[] GetVideoBuffer() { return VideoBuffer; }
		public int VirtualWidth { get { return 320; } }
		public int BufferWidth { get { return 320; } }
		public int BufferHeight { get { return 144; } }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		#endregion

		#region SoundProvider

		// i tried using the left and right buffers and then mixing them together... it was kind of a mess of code, and slow

		Sound.Utilities.BlipBuffer blip_left;
		Sound.Utilities.BlipBuffer blip_right;


		short[] LeftBuffer = new short[(35112 + 2064) * 2];
		short[] RightBuffer = new short[(35112 + 2064) * 2];

		short[] SampleBuffer = new short[1536];
		int SampleBufferContains = 0;

		int LatchL;
		int LatchR;

		void PrepSound()
		{
			unsafe
			{
				fixed (short* sl = LeftBuffer, sr = RightBuffer)
				{
					for (uint i = 0; i < SampPerFrame * 2; i += 2)
					{
						// gameboy audio output is mono, so ignore one sample
						int s = sl[i];
						if (s != LatchL)
						{
							blip_left.AddDelta(i, s - LatchL);
							LatchL = s;
						}
						s = sr[i];
						if (s != LatchR)
						{
							blip_right.AddDelta(i, s - LatchR);
							LatchR = s;
						}
					}

				}
			}
			blip_left.EndFrame(SampPerFrame * 2);
			blip_right.EndFrame(SampPerFrame * 2);
			int count = blip_left.SamplesAvailable();
			if (count != blip_right.SamplesAvailable())
				throw new Exception("Sound problem?");

			blip_left.ReadSamplesLeft(SampleBuffer, count);
			blip_right.ReadSamplesRight(SampleBuffer, count);
			SampleBufferContains = count;
		}

		public void GetSamples(out short[] samples, out int nsamp)
		{
			nsamp = SampleBufferContains;
			samples = SampleBuffer;
		}

		public void DiscardSamples()
		{
			SampleBufferContains = 0;
		}

		#endregion
	}
}
