using System.Diagnostics;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy : IEmulator, IBoardInfo
	{
		private readonly BasicServiceProvider _serviceProvider;
		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		public ControllerDefinition ControllerDefinition { get; }

		public bool FrameAdvance(IController controller, bool render, bool rendersound)
		{
			FrameAdvancePrep(controller);
			uint samplesEmitted;
			uint samplesEmittedInFrame = 0; // for sgb
			switch (_syncSettings.FrameLength)
			{
				case GambatteSyncSettings.FrameLengthType.VBlankDrivenFrames:
					// target number of samples to emit: always 59.7fps
					// runfor() always ends after creating a video frame, so sync-up is guaranteed
					// when the display has been off, some frames can be markedly shorter than expected
					samplesEmitted = TICKSINFRAME;
					if (LibGambatte.gambatte_runfor(GambatteState, FrameBuffer, 160, _soundbuff, ref samplesEmitted) > 0)
					{
						Array.Copy(FrameBuffer, VideoBuffer, FrameBuffer.Length);
						if (IsSgb)
						{
							if (LibGambatte.gambatte_updatescreenborder(GambatteState, SgbVideoBuffer, 256) != 0)
							{
								throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_updatescreenborder)}() returned non-zero (border error???)");
							}
						}
					}

					_cycleCount += samplesEmitted;
					samplesEmittedInFrame += samplesEmitted;
					frameOverflow = 0;

					if (rendersound && !Muted)
					{
						ProcessSound((int)samplesEmitted);
					}
					break;
				case GambatteSyncSettings.FrameLengthType.EqualLengthFrames:
					while (true)
					{
						// target number of samples to emit: length of 1 frame minus whatever overflow
						samplesEmitted = TICKSINFRAME - frameOverflow;
						Debug.Assert(samplesEmitted * 2 <= _soundbuff.Length, "buffer capacity exceeded");
						if (LibGambatte.gambatte_runfor(GambatteState, FrameBuffer, 160, _soundbuff, ref samplesEmitted) > 0)
						{
							Array.Copy(FrameBuffer, VideoBuffer, FrameBuffer.Length);
							if (IsSgb)
							{
								if (LibGambatte.gambatte_updatescreenborder(GambatteState, SgbVideoBuffer, 256) != 0)
								{
									throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_updatescreenborder)}() returned non-zero (border error???)");
								}
							}
						}

						// account for actual number of samples emitted
						_cycleCount += samplesEmitted;
						samplesEmittedInFrame += samplesEmitted;
						frameOverflow += samplesEmitted;

						if (rendersound && !Muted)
						{
							ProcessSound((int)samplesEmitted);
						}

						if (frameOverflow >= TICKSINFRAME)
						{
							frameOverflow -= TICKSINFRAME;
							break;
						}
					}
					break;
				case GambatteSyncSettings.FrameLengthType.UserDefinedFrames:
					while (true)
					{
						// target number of samples to emit: input length
						float inputFrameLength = controller.AxisValue("Input Length");
						uint inputFrameLengthInt = (uint)Math.Floor(inputFrameLength);
						if (inputFrameLengthInt == 0)
						{
							inputFrameLengthInt = TICKSINFRAME;
						}
						samplesEmitted = inputFrameLengthInt - frameOverflow;
						Debug.Assert(samplesEmitted * 2 <= _soundbuff.Length, "buffer capacity exceeded");
						if (LibGambatte.gambatte_runfor(GambatteState, FrameBuffer, 160, _soundbuff, ref samplesEmitted) > 0)
						{
							Array.Copy(FrameBuffer, VideoBuffer, FrameBuffer.Length);
							if (IsSgb)
							{
								if (LibGambatte.gambatte_updatescreenborder(GambatteState, SgbVideoBuffer, 256) != 0)
								{
									throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_updatescreenborder)}() returned non-zero (border error???)");
								}
							}
						}

						// account for actual number of samples emitted
						_cycleCount += samplesEmitted;
						samplesEmittedInFrame += samplesEmitted;
						frameOverflow += samplesEmitted;

						if (rendersound && !Muted)
						{
							ProcessSound((int)samplesEmitted);
						}

						if (frameOverflow >= inputFrameLengthInt)
						{
							frameOverflow = 0;
							break;
						}
					}
					break;
				default:
					throw new InvalidOperationException();
			}

			if (IsSgb)
			{
				ProcessSgbSound(rendersound && !Muted);
			}

			ProcessMbcSound(rendersound && !Muted);

			if (rendersound && !Muted)
			{
				ProcessSoundEnd();
			}

			FrameAdvancePost();

			return true;
		}

		public int Frame { get; private set; }

		public string SystemId => IsSgb ? VSystemID.Raw.SGB : VSystemID.Raw.GB;

		public string BoardName { get; }

		public bool DeterministicEmulation { get; }

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;

			// reset frame counters is meant to "re-zero" emulation time wherever it was
			// so these should be reset as well
			_cycleCount = 0;
			frameOverflow = 0;
		}

		public void Dispose()
		{
			if (GambatteState != IntPtr.Zero)
			{
				LibGambatte.gambatte_destroy(GambatteState);
				GambatteState = IntPtr.Zero;
			}

			DisposeSound();
		}
	}
}
