using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class GambatteLink : IEmulator, IBoardInfo
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public IVGamepadDef ControllerDefinition => DualGbController;

		public bool FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			LCont.Clear();
			RCont.Clear();

			foreach (var s in DualGbController.BoolButtons)
			{
				if (controller.IsPressed(s))
				{
					if (s.Contains("P1 "))
					{
						LCont.Set(s.Replace("P1 ", ""));
					}
					else if (s.Contains("P2 "))
					{
						RCont.Set(s.Replace("P2 ", ""));
					}
				}
			}

			bool cablediscosignalNew = controller.IsPressed("Toggle Cable");
			if (cablediscosignalNew && !_cablediscosignal)
			{
				_cableconnected ^= true;
				Console.WriteLine("Cable connect status to {0}", _cableconnected);
			}

			_cablediscosignal = cablediscosignalNew;

			Frame++;
			L.FrameAdvancePrep(LCont);
			R.FrameAdvancePrep(RCont);

			unsafe
			{
				fixed (int* leftvbuff = &VideoBuffer[0])
				{
					// use pitch to have both cores write to the same video buffer, interleaved
					int* rightvbuff = leftvbuff + 160;
					const int Pitch = 160 * 2;

					fixed (short* leftsbuff = LeftBuffer, rightsbuff = RightBuffer)
					{
						const int Step = 32; // could be 1024 for GB

						int nL = _overflowL;
						int nR = _overflowR;

						// slowly step our way through the frame, while continually checking and resolving link cable status
						for (int target = 0; target < SampPerFrame;)
						{
							target += Step;
							if (target > SampPerFrame)
							{
								target = SampPerFrame; // don't run for slightly too long depending on step
							}

							// gambatte_runfor() aborts early when a frame is produced, but we don't want that, hence the while()
							while (nL < target)
							{
								uint nsamp = (uint)(target - nL);
								if (LibGambatte.gambatte_runfor(L.GambatteState, leftsbuff + (nL * 2), ref nsamp) > 0)
								{
									LibGambatte.gambatte_blitto(L.GambatteState, leftvbuff, Pitch);
								}

								nL += (int)nsamp;
							}

							while (nR < target)
							{
								uint nsamp = (uint)(target - nR);
								if (LibGambatte.gambatte_runfor(R.GambatteState, rightsbuff + (nR * 2), ref nsamp) > 0)
								{
									LibGambatte.gambatte_blitto(R.GambatteState, rightvbuff, Pitch);
								}

								nR += (int)nsamp;
							}

							// poll link cable statuses, but not when the cable is disconnected
							if (!_cableconnected)
							{
								continue;
							}

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

						_overflowL = nL - SampPerFrame;
						_overflowR = nR - SampPerFrame;
						if (_overflowL < 0 || _overflowR < 0)
						{
							throw new Exception("Timing problem?");
						}

						if (rendersound)
						{
							PrepSound();
						}

						// copy extra samples back to beginning
						for (int i = 0; i < _overflowL * 2; i++)
						{
							LeftBuffer[i] = LeftBuffer[i + (SampPerFrame * 2)];
						}

						for (int i = 0; i < _overflowR * 2; i++)
						{
							RightBuffer[i] = RightBuffer[i + (SampPerFrame * 2)];
						}
					}
				}
			}

			L.FrameAdvancePost();
			R.FrameAdvancePost();
			IsLagFrame = L.IsLagFrame && R.IsLagFrame;
			if (IsLagFrame)
			{
				LagCount++;
			}

			return true;
		}

		public int Frame { get; private set; }

		public string SystemId => "DGB";

		public bool DeterministicEmulation => L.DeterministicEmulation && R.DeterministicEmulation;

		public string BoardName => L.BoardName + '|' + R.BoardName;

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				L.Dispose();
				L = null;

				R.Dispose();
				R = null;

				_blipLeft.Dispose();
				_blipLeft = null;

				_blipRight.Dispose();
				_blipRight = null;

				_disposed = true;
			}
		}
	}
}
