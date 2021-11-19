using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class GambatteLink : IEmulator, IBoardInfo
	{
		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		public ControllerDefinition ControllerDefinition => GBLinkController;

		public bool FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			for (int i = 0; i < _numCores; i++)
			{
				_linkedConts[i].Clear();
			}

			foreach (var s in GBLinkController.BoolButtons)
			{
				if (controller.IsPressed(s))
				{
					for (int i = 0; i < _numCores; i++)
					{
						if (s.Contains($"P{i + 1} "))
						{
							_linkedConts[i].Set(s.Replace($"P{i + 1} ", ""));
						}
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

			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i].FrameAdvancePrep(_linkedConts[i]);
			}

			unsafe
			{
				fixed (int* fbuff = &FrameBuffer[0])
				{
					// use pitch to have both cores write to the same frame buffer, interleaved
					int Pitch = 160 * _numCores;

					fixed (short* sbuff = &SoundBuffer[0])
					{
						const int Step = 32; // could be 1024 for GB

						int[] n = new int[_numCores];
						for (int i = 0; i < _numCores; i++)
						{
							n[i] = _linkedOverflow[i];
						}

						// slowly step our way through the frame, while continually checking and resolving link cable status
						for (int target = 0; target < SampPerFrame;)
						{
							target += Step;
							if (target > SampPerFrame)
							{
								target = SampPerFrame; // don't run for slightly too long depending on step
							}

							for (int i = 0; i < _numCores; i++)
							{
								// gambatte_runfor() aborts early when a frame is produced, but we don't want that, hence the while()
								while (n[i] < target)
								{
									uint nsamp = (uint)(target - n[i]);
									if (LibGambatte.gambatte_runfor(_linkedCores[i].GambatteState, fbuff + (i * 160), Pitch, sbuff + (i * MaxSampsPerFrame) + (n[i] * 2), ref nsamp) > 0)
									{
										for (int j = 0; j < 144; j++)
										{
											Array.Copy(FrameBuffer, (i * 160) + (j * Pitch), VideoBuffer, (i * 160) + (j * Pitch), 160);
										}
									}

									n[i] += (int)nsamp;
								}
							}

							// poll link cable statuses, but not when the cable is disconnected
							if (!_cableconnected)
							{
								continue;
							}

							if (LibGambatte.gambatte_linkstatus(_linkedCores[0].GambatteState, 256) != 0) // ClockTrigger
							{
								LibGambatte.gambatte_linkstatus(_linkedCores[0].GambatteState, 257); // ack
								int lo = LibGambatte.gambatte_linkstatus(_linkedCores[0].GambatteState, 258); // GetOut
								int ro = LibGambatte.gambatte_linkstatus(_linkedCores[1].GambatteState, 258);
								LibGambatte.gambatte_linkstatus(_linkedCores[0].GambatteState, ro & 0xff); // ShiftIn
								LibGambatte.gambatte_linkstatus(_linkedCores[1].GambatteState, lo & 0xff); // ShiftIn
							}

							if (LibGambatte.gambatte_linkstatus(_linkedCores[1].GambatteState, 256) != 0) // ClockTrigger
							{
								LibGambatte.gambatte_linkstatus(_linkedCores[1].GambatteState, 257); // ack
								int lo = LibGambatte.gambatte_linkstatus(_linkedCores[0].GambatteState, 258); // GetOut
								int ro = LibGambatte.gambatte_linkstatus(_linkedCores[1].GambatteState, 258);
								LibGambatte.gambatte_linkstatus(_linkedCores[0].GambatteState, ro & 0xff); // ShiftIn
								LibGambatte.gambatte_linkstatus(_linkedCores[1].GambatteState, lo & 0xff); // ShiftIn
							}
						}

						for (int i = 0; i < _numCores; i++)
						{
							_linkedOverflow[i] = n[i] - SampPerFrame;
							if (_linkedOverflow[i] < 0)
							{
								throw new Exception("Timing problem?");
							}
						}

						if (rendersound)
						{
							PrepSound();
						}

						// copy extra samples back to beginning
						for (int i = 0; i < _numCores; i++)
						{
							for (int j = 0; j < _linkedOverflow[i] * 2; j++)
							{
								SoundBuffer[(i * MaxSampsPerFrame) + j] = SoundBuffer[(i * MaxSampsPerFrame) + j + (SampPerFrame * 2)];
							}
						}
					}
				}
			}

			IsLagFrame = true;

			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i].FrameAdvancePost();
				IsLagFrame &= _linkedCores[i].IsLagFrame;
			}

			if (IsLagFrame)
			{
				LagCount++;
			}

			Frame++;

			return true;
		}

		public int Frame { get; private set; }

		public string SystemId => VSystemID.Raw.DGB;

		public bool DeterministicEmulation => LinkedDeterministicEmulation();

		private bool LinkedDeterministicEmulation()
		{
			bool deterministicEmulation = true;
			for (int i = 0; i < _numCores; i++)
			{
				deterministicEmulation &= _linkedCores[i].DeterministicEmulation;
			}
			return deterministicEmulation;
		}

		public string BoardName => LinkedBoardName();

		private string LinkedBoardName()
		{
			string boardName = "";
			for (int i = 0; i < _numCores; i++)
			{
				boardName += _linkedCores[i].BoardName + "|";
			}
			return boardName.Remove(boardName.Length - 1);
		}

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;

			for (int i = 0; i < _numCores; i++)
			{
				_linkedOverflow[i] = 0;
			}
		}

		public void Dispose()
		{
			if (_numCores > 0)
			{
				for (int i = 0; i < _numCores; i++)
				{
					_linkedCores[i].Dispose();
					_linkedCores[i] = null;
					_linkedBlips[i].Dispose();
					_linkedBlips[i] = null;
				}

				_numCores = 0;
			}
		}
	}
}
