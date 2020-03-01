using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	public interface ISynchronizingAudioBuffer
	{
		void EnqueueSamples(short[] buf, int samplesProvided);
		void EnqueueSample(short left, short right);
		void Clear();

		// returns the number of samples actually supplied, which may not match the number requested
		// ^^ what the hell is that supposed to mean.
		// the entire point of an ISynchronizingAudioBuffer
		// is to provide exact amounts of output samples,
		// even when the input provided varies....
		int OutputSamples(short[] buf, int samplesRequested);
	}

	internal class NitsujaSynchronizer : ISynchronizingAudioBuffer
	{
		private struct StereoSamp
		{
			public readonly short L, R;

			public StereoSamp(short left, short right)
			{
				L = left;
				R = right;
			}
		}

		private readonly List<StereoSamp> _sampleQueue = new List<StereoSamp>();

		// returns values going between 0 and y-1 in a saw wave pattern, based on x
		private static int PingPong(int x, int y)
		{
			x %= 2 * y;
			if (x >= y)
			{
				x = (2 * y) - x - 1;
			}

			return x;

			// in case we want to switch to odd buffer sizes for more sharpness
			////x %= 2*(y-1);
			////if(x >= y)
			////x = 2*(y-1) - x;
			////return x;
		}

		private static StereoSamp CrossFade(StereoSamp lhs, StereoSamp rhs, int cur, int start, int end)
		{
			if (cur <= start)
			{
				return lhs;
			}

			if (cur >= end)
			{
				return rhs;
			}

			// in case we want sine wave interpolation instead of linear here
			////float ang = 3.14159f * (float)(cur - start) / (float)(end - start);
			////cur = start + (int)((1-cosf(ang))*0.5f * (end - start));

			int inNum = cur - start;
			int outNum = end - cur;
			int denom = end - start;

			int lrv = ((lhs.L * outNum) + (rhs.L * inNum)) / denom;
			int rrv = ((lhs.R * outNum) + (rhs.R * inNum)) / denom;

			return new StereoSamp((short)lrv, (short)rrv);
		}

		public void Clear()
		{
			_sampleQueue.Clear();
		}

		private static void EmitSample(short[] outBuf, ref int cursor, StereoSamp sample)
		{
			outBuf[cursor++] = sample.L;
			outBuf[cursor++] = sample.R;
		}

		private static void EmitSamples(short[] outBuf, ref int outCursor, StereoSamp[] sampleBuf, int inCursor, int samples)
		{
			for (int i = 0; i < samples; i++)
			{
				EmitSample(outBuf, ref outCursor, sampleBuf[i + inCursor]);
			}
		}

		private static int Abs(int value)
		{
			if (value < 0)
			{
				return -value;
			}

			return value;
		}

		public void EnqueueSamples(short[] buf, int samplesProvided)
		{
			int cursor = 0;
			for (int i = 0; i < samplesProvided; i++)
			{
				_sampleQueue.Add(new StereoSamp(buf[cursor + 0], buf[cursor + 1]));
				cursor += 2;
			}
		}

		public void EnqueueSample(short left, short right)
		{
			_sampleQueue.Add(new StereoSamp(left, right));
		}

		public int OutputSamples(short[] buf, int samplesRequested)
		{
			Console.WriteLine("{0} {1}", samplesRequested, _sampleQueue.Count); // add this line

			int bufCursor = 0;
			int audioSize = samplesRequested;
			int queued = _sampleQueue.Count;

			// I am too lazy to deal with odd numbers
			audioSize &= ~1;
			queued &= ~1;

			if (queued > 0x200 && audioSize > 0) // is there any work to do?
			{
				// are we going at normal speed?
				// or more precisely, are the input and output queues/buffers of similar size?
				if (queued > 900 || audioSize > queued * 2)
				{
					// not normal speed. we have to resample it somehow in this case.
					if (audioSize <= queued)
					{
						// fast forward speed
						// this is the easy case, just crossfade it and it sounds ok
						for (int i = 0; i < audioSize; i++)
						{
							int j = i + queued - audioSize;
							StereoSamp outSamp = CrossFade(_sampleQueue[i], _sampleQueue[j], i, 0, audioSize);
							EmitSample(buf, ref bufCursor, outSamp);
						}
					}
					else
					{
						// slow motion speed
						// here we take a very different approach,
						// instead of crossfading it, we select a single sample from the queue
						// and make sure that the index we use to select a sample is constantly moving
						// and that it starts at the first sample in the queue and ends on the last one.
						//
						// hopefully the index doesn't move discontinuously or we'll get slight crackling
						// (there might still be a minor bug here that causes this occasionally)
						//
						// here's a diagram of how the index we sample from moves:
						//
						// queued (this axis represents the index we sample from. the top means the end of the queue)
						// ^
						// |   --> audio size (this axis represents the output index we write to, right meaning forward in output time/position)
						// |   A           C       C  end
						//    A A     B   C C     C
						//   A   A   A B C   C   C
						//  A     A A   B     C C
						// A       A           C
						// start
						//
						// yes, this means we are spending some stretches of time playing the sound backwards,
						// but the stretches are short enough that this doesn't sound weird.
						// this lets us avoid most crackling problems due to the endpoints matching up.

						// first calculate a shorter-than-full window
						// that has minimal slope at the endpoints
						// (to further reduce crackling, especially in sine waves)
						int bestStart = 0;
						int extraAtEnd;
						{
							int bestEnd = queued;
							const int worstDiff = 99999999;
							int bestStartDiff = worstDiff;
							int bestEndDiff = worstDiff;
							for (int i = 0; i < 128; i += 2)
							{
								int diff = Abs(_sampleQueue[i].L - _sampleQueue[i + 1].L) + Abs(_sampleQueue[i].R - _sampleQueue[i + 1].R);
								if (diff < bestStartDiff)
								{
									bestStartDiff = diff;
									bestStart = i;
								}
							}

							for (int i = queued - 3; i > queued - 3 - 128; i -= 2)
							{
								int diff = Abs(_sampleQueue[i].L - _sampleQueue[i + 1].L) + Abs(_sampleQueue[i].R - _sampleQueue[i + 1].R);
								if (diff < bestEndDiff)
								{
									bestEndDiff = diff;
									bestEnd = i + 1;
								}
							}

							extraAtEnd = queued - bestEnd;
							queued = bestEnd - bestStart;

							int okSize = queued;
							while (okSize + (queued * 2) + bestStart + extraAtEnd <= samplesRequested)
							{
								okSize += queued * 2;
							}

							audioSize = okSize;

							for (int x = 0; x < bestStart; x++)
							{
								EmitSample(buf, ref bufCursor, _sampleQueue[x]);
							}

							_sampleQueue.RemoveRange(0, bestStart);
						}

						int midpointX = audioSize >> 1;
						int midpointY = queued >> 1;

						// all we need to do here is calculate the X position of the leftmost "B" in the above diagram.
						// TODO: we should calculate it with a simple equation like
						//   midpointXOffset = min(something,somethingElse);
						// but it's a little difficult to work it out exactly
						// so here's a stupid search for the value for now:
						int prevA = 999999;
						int midpointXOffset = queued / 2;
						while (true)
						{
							int a = Abs(PingPong(midpointX - midpointXOffset, queued) - midpointY) - midpointXOffset;
							if (((a > 0) != (prevA > 0) || (a < 0) != (prevA < 0)) && prevA != 999999)
							{
								if (((a + prevA) & 1) != 0) // there's some sort of off-by-one problem with this search since we're moving diagonally...
								{
									midpointXOffset++; // but this fixes it most of the time...
								}

								break; // found it
							}

							prevA = a;
							midpointXOffset--;
							if (midpointXOffset < 0)
							{
								midpointXOffset = 0;
								break; // failed to find it. the two sides probably meet exactly in the center.
							}
						}

						int leftMidpointX = midpointX - midpointXOffset;
						int rightMidpointX = midpointX + midpointXOffset;
						int leftMidpointY = PingPong(leftMidpointX, queued);
						int rightMidpointY = (queued - 1) - PingPong((int)audioSize - 1 - rightMidpointX + (queued * 2), queued);

						// output the left almost-half of the sound (section "A")
						for (int x = 0; x < leftMidpointX; x++)
						{
							int i = PingPong(x, queued);
							EmitSample(buf, ref bufCursor, _sampleQueue[i]);
						}

						// output the middle stretch (section "B")
						int y = leftMidpointY;
						int dyMidLeft = (leftMidpointY < midpointY) ? 1 : -1;
						int dyMidRight = (rightMidpointY > midpointY) ? 1 : -1;
						for (int x = leftMidpointX; x < midpointX; x++, y += dyMidLeft)
						{
							EmitSample(buf, ref bufCursor, _sampleQueue[y]);
						}

						for (int x = midpointX; x < rightMidpointX; x++, y += dyMidRight)
						{
							EmitSample(buf, ref bufCursor, _sampleQueue[y]);
						}

						// output the end of the queued sound (section "C")
						for (int x = rightMidpointX; x < audioSize; x++)
						{
							int i = (queued - 1) - PingPong((int)audioSize - 1 - x + (queued * 2), queued);
							EmitSample(buf, ref bufCursor, _sampleQueue[i]);
						}

						for (int x = 0; x < extraAtEnd; x++)
						{
							int i = queued + x;
							EmitSample(buf, ref bufCursor, _sampleQueue[i]);
						}

						queued += extraAtEnd;
						audioSize += bestStart + extraAtEnd;
					} // end else

					// sampleQueue.erase(sampleQueue.begin(), sampleQueue.begin() + queued);
					_sampleQueue.RemoveRange(0, queued);

					// zero 08-nov-2010: did i do this right?
					return audioSize;
				}

				// normal speed
				// just output the samples straightforwardly.
				//
				// at almost-full speeds (like 50/60 FPS)
				// what will happen is that we rapidly fluctuate between entering this branch
				// and entering the "slow motion speed" branch above.
				// but that's ok! because all of these branches sound similar enough that we can get away with it.
				// so the two cases actually complement each other.
				if (audioSize >= queued)
				{
					EmitSamples(buf, ref bufCursor, _sampleQueue.ToArray(), 0, queued);
					_sampleQueue.RemoveRange(0, queued);
					return queued;
				}

				EmitSamples(buf, ref bufCursor, _sampleQueue.ToArray(), 0, audioSize);
				_sampleQueue.RemoveRange(0, audioSize);
				return audioSize;
			}

			return 0;
		}
	}

	internal class VecnaSynchronizer : ISynchronizingAudioBuffer
	{
		// vecna's attempt at a fully synchronous sound provider.
		// It's similar in philosophy to my "BufferedAsync" provider, but BufferedAsync is not
		// fully synchronous.

		// Like BufferedAsync, it tries to make most frames 100% correct and just suck it up
		// periodically and have a big bad-sounding mistake frame if it has to.

		// It is significantly less ambitious and elaborate than the other methods. 
		// We'll see if it works better or not!

		// It has a min and maximum amount of excess buffer to deal with minor overflows.
		// When fast-forwarding, it will discard samples above the maximum excess buffer.

		// When underflowing, it will attempt to resample to a certain thresh
		// old.
		// If it underflows beyond that threshold, it will give up and output silence.
		// Since it has done this, it will go ahead and generate some excess silence in order
		// to restock its excess buffer.
		private struct Sample
		{
			public readonly short Left;
			public readonly short Right;

			public Sample(short left, short right)
			{
				Left = left;
				Right = right;
			}
		}

		private const int MaxExcessSamples = 2048;

		private readonly Queue<Sample> _buffer;
		private readonly Sample[] _resampleBuffer;

		public VecnaSynchronizer()
		{
			_buffer = new Queue<Sample>(2048);
			_resampleBuffer = new Sample[2730]; // 2048 * 1.25

			// Give us a little buffer wiggle-room
			for (int i = 0; i < 367; i++)
			{
				_buffer.Enqueue(new Sample(0, 0));
			}
		}

		public void EnqueueSamples(short[] buf, int samplesProvided)
		{
			int ctr = 0;
			for (int i = 0; i < samplesProvided; i++)
			{
				short left = buf[ctr++];
				short right = buf[ctr++];
				EnqueueSample(left, right);
			}
		}

		public void EnqueueSample(short left, short right)
		{
			if (_buffer.Count >= MaxExcessSamples - 1)
			{
				// if buffer is overfull, dequeue old samples to make room for new samples.
				_buffer.Dequeue();
			}

			_buffer.Enqueue(new Sample(left, right));
		}

		public void Clear()
		{
			_buffer.Clear();
		}

		public int OutputSamples(short[] buf, int samplesRequested)
		{
			if (samplesRequested > _buffer.Count)
			{
				// underflow!
				if (_buffer.Count > samplesRequested * 3 / 4)
				{
					// if we're within 75% of target, then I guess we suck it up and resample.
					// we sample in a goofy way, we could probably do it a bit smarter, if we cared more.
					int samplesAvailable = _buffer.Count;
					for (int i = 0; _buffer.Count > 0; i++)
					{
						_resampleBuffer[i] = _buffer.Dequeue();
					}

					int index = 0;
					for (int i = 0; i < samplesRequested; i++)
					{
						Sample sample = _resampleBuffer[i * samplesAvailable / samplesRequested];
						buf[index++] += sample.Left;
						buf[index++] += sample.Right;
					}
				}
				else
				{
					// we're outside of a "reasonable" underflow. Give up and output silence.
					// Do nothing. The whole frame will be excess buffer.
				}
			}
			else
			{
				// normal operation
				int index = 0;
				for (int i = 0; i < samplesRequested && _buffer.Count > 0; i++)
				{
					Sample sample = _buffer.Dequeue();
					buf[index++] += sample.Left;
					buf[index++] += sample.Right;
				}
			}

			return samplesRequested;
		}
	}
}
