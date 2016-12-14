using System;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.SNES;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	[CoreAttributes(
		"DualGambatte",
		"sinamas/natt",
		isPorted: true,
		isReleased: true
		)]
	[ServiceNotApplicable(typeof(IDriveLight))]
	public partial class GambatteLink : IEmulator, IVideoProvider, ISoundProvider, IInputPollable, ISaveRam, IStatable, ILinkable,
		IDebuggable, ISettable<GambatteLink.GambatteLinkSettings, GambatteLink.GambatteLinkSyncSettings>, ICodeDataLogger
	{
		public GambatteLink(CoreComm comm, GameInfo leftinfo, byte[] leftrom, GameInfo rightinfo, byte[] rightrom, object Settings, object SyncSettings, bool deterministic)
		{
			ServiceProvider = new BasicServiceProvider(this);
			GambatteLinkSettings _Settings = (GambatteLinkSettings)Settings ?? new GambatteLinkSettings();
			GambatteLinkSyncSettings _SyncSettings = (GambatteLinkSyncSettings)SyncSettings ?? new GambatteLinkSyncSettings();

			CoreComm = comm;
			L = new Gameboy(new CoreComm(comm.ShowMessage, comm.Notify), leftinfo, leftrom, _Settings.L, _SyncSettings.L, deterministic);
			R = new Gameboy(new CoreComm(comm.ShowMessage, comm.Notify), rightinfo, rightrom, _Settings.R, _SyncSettings.R, deterministic);

			// connect link cable
			LibGambatte.gambatte_linkstatus(L.GambatteState, 259);
			LibGambatte.gambatte_linkstatus(R.GambatteState, 259);

			L.Controller = LCont;
			R.Controller = RCont;
			L.ConnectInputCallbackSystem(_inputCallbacks);
			R.ConnectInputCallbackSystem(_inputCallbacks);
			L.ConnectMemoryCallbackSystem(_memorycallbacks);
			R.ConnectMemoryCallbackSystem(_memorycallbacks);

			comm.VsyncNum = L.CoreComm.VsyncNum;
			comm.VsyncDen = L.CoreComm.VsyncDen;
			comm.RomStatusAnnotation = null;
			comm.RomStatusDetails = "LEFT:\r\n" + L.CoreComm.RomStatusDetails + "RIGHT:\r\n" + R.CoreComm.RomStatusDetails;
			comm.NominalWidth = L.CoreComm.NominalWidth + R.CoreComm.NominalWidth;
			comm.NominalHeight = L.CoreComm.NominalHeight;

			LinkConnected = true;

			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;

			blip_left = new BlipBuffer(1024);
			blip_right = new BlipBuffer(1024);
			blip_left.SetRates(2097152 * 2, 44100);
			blip_right.SetRates(2097152 * 2, 44100);

			SetMemoryDomains();
		}

		public bool LinkConnected { get; private set; }

		bool disposed = false;

		Gameboy L;
		Gameboy R;
		// counter to ensure we do 35112 samples per frame
		int overflowL = 0;
		int overflowR = 0;
		/// <summary>if true, the link cable is currently connected</summary>
		bool cableconnected = true;
		/// <summary>if true, the link cable toggle signal is currently asserted</summary>
		bool cablediscosignal = false;

		const int SampPerFrame = 35112;

		LibsnesCore.SnesSaveController LCont = new LibsnesCore.SnesSaveController(Gameboy.GbController);
		LibsnesCore.SnesSaveController RCont = new LibsnesCore.SnesSaveController(Gameboy.GbController);

		public bool IsCGBMode(bool right)
		{
			return right ? R.IsCGBMode() : L.IsCGBMode();
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public static readonly ControllerDefinition DualGbController = new ControllerDefinition
		{
			Name = "Dual Gameboy Controller",
			BoolButtons =
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 A", "P1 B", "P1 Select", "P1 Start", "P1 Power",
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 A", "P2 B", "P2 Select", "P2 Start", "P2 Power",
				"Toggle Cable"
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
				if (Controller.IsPressed(s))
				{
					if (s.Contains("P1 "))
						LCont.Set(s.Replace("P1 ", ""));
					else if (s.Contains("P2 "))
						RCont.Set(s.Replace("P2 ", ""));
				}
			}
			bool cablediscosignal_new = Controller.IsPressed("Toggle Cable");
			if (cablediscosignal_new && !cablediscosignal)
			{
				cableconnected ^= true;
				Console.WriteLine("Cable connect status to {0}", cableconnected);
				LinkConnected = cableconnected;
			}
			cablediscosignal = cablediscosignal_new;

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
								if (LibGambatte.gambatte_runfor(L.GambatteState, leftsbuff + nL * 2, ref nsamp) > 0)
									LibGambatte.gambatte_blitto(L.GambatteState, leftvbuff, pitch);
								nL += (int)nsamp;
							}
							while (nR < target)
							{
								uint nsamp = (uint)(target - nR);
								if (LibGambatte.gambatte_runfor(R.GambatteState, rightsbuff + nR * 2, ref nsamp) > 0)
									LibGambatte.gambatte_blitto(R.GambatteState, rightvbuff, pitch);
								nR += (int)nsamp;
							}

							// poll link cable statuses, but not when the cable is disconnected
							if (!cableconnected)
								continue;

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
		
		public string SystemId { get { return "DGB"; } }
		public bool DeterministicEmulation { get { return L.DeterministicEmulation && R.DeterministicEmulation; } }

		public string BoardName { get { return L.BoardName + '|' + R.BoardName; } }

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public CoreComm CoreComm { get; private set; }

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

		#region SoundProvider

		// i tried using the left and right buffers and then mixing them together... it was kind of a mess of code, and slow

		BlipBuffer blip_left;
		BlipBuffer blip_right;

		short[] LeftBuffer = new short[(35112 + 2064) * 2];
		short[] RightBuffer = new short[(35112 + 2064) * 2];

		short[] SampleBuffer = new short[1536];
		int SampleBufferContains = 0;

		int LatchL;
		int LatchR;

		unsafe void PrepSound()
		{
			fixed (short* sl = LeftBuffer, sr = RightBuffer)
			{
				for (uint i = 0; i < SampPerFrame * 2; i += 2)
				{
					int s = (sl[i] + sl[i + 1]) / 2;
					if (s != LatchL)
					{
						blip_left.AddDelta(i, s - LatchL);
						LatchL = s;
					}
					s = (sr[i] + sr[i + 1]) / 2;
					if (s != LatchR)
					{
						blip_right.AddDelta(i, s - LatchR);
						LatchR = s;
					}
				}

			}

			blip_left.EndFrame(SampPerFrame * 2);
			blip_right.EndFrame(SampPerFrame * 2);
			int count = blip_left.SamplesAvailable();
			if (count != blip_right.SamplesAvailable())
				throw new Exception("Sound problem?");

			// calling blip.Clear() causes rounding fractions to be reset,
			// and if only one channel is muted, in subsequent frames we can be off by a sample or two
			// not a big deal, but we didn't account for it.  so we actually complete the entire
			// audio read and then stamp it out if muted.

			blip_left.ReadSamplesLeft(SampleBuffer, count);
			if (L.Muted)
			{
				fixed (short* p = SampleBuffer)
				{
					for (int i = 0; i < SampleBuffer.Length; i += 2)
						p[i] = 0;
				}
			}

			blip_right.ReadSamplesRight(SampleBuffer, count);
			if (R.Muted)
			{
				fixed (short* p = SampleBuffer)
				{
					for (int i = 1; i < SampleBuffer.Length; i += 2)
						p[i] = 0;
				}
			}
			SampleBufferContains = count;
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			nsamp = SampleBufferContains;
			samples = SampleBuffer;
		}

		public void DiscardSamples()
		{
			SampleBufferContains = 0;
		}

		public bool CanProvideAsync
		{
			get { return false; }
		}

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		public SyncSoundMode SyncMode
		{
			get { return SyncSoundMode.Sync; }
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		#endregion
	}
}
