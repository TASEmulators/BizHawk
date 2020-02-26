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

	internal class ZeromusSynchronizer : ISynchronizingAudioBuffer
	{
		public ZeromusSynchronizer()
		{
			////#ifdef NDEBUG
			_adjustobuf = new Adjustobuf(200, 1000);
			////#else
			////adjustobuf = new Adjustobuf(22000, 44000);
			////#endif
		}

		// adjustobuf(200,1000)
		private bool _mixqueueGo;

		public void Clear()
		{
			_adjustobuf.Clear();
		}

		public void EnqueueSample(short left, short right)
		{
			_adjustobuf.Enqueue(left, right);
		}

		public void EnqueueSamples(short[] buf, int samplesProvided)
		{
			int ctr = 0;
			for (int i = 0; i < samplesProvided; i++)
			{
				short left = buf[ctr++];
				short right = buf[ctr++];
				_adjustobuf.Enqueue(left, right);
			}
		}

		// returns the number of samples actually supplied, which may not match the number requested
		public int OutputSamples(short[] buf, int samplesRequested)
		{
			int ctr = 0;
			int done = 0;
			if (!_mixqueueGo)
			{
				if (_adjustobuf.Size > 200)
				{
					_mixqueueGo = true;
				}
			}
			else
			{
				for (int i = 0; i < samplesRequested; i++)
				{
					if (_adjustobuf.Size == 0)
					{
						_mixqueueGo = false;
						break;
					}

					done++;
					_adjustobuf.Dequeue(out var left, out var right);
					buf[ctr++] = left;
					buf[ctr++] = right;
				}
			}

			return done;
		}

		private readonly Adjustobuf _adjustobuf;

		private class Adjustobuf
		{
			public Adjustobuf(int minLatency, int maxLatency)
			{
				_minLatency = minLatency;
				_maxLatency = maxLatency;
				Clear();
			}

			private readonly int _minLatency;
			private readonly int _maxLatency;
			private readonly short[] _curr = new short[2];

			private readonly Queue<short> _buffer = new Queue<short>();
			private readonly Queue<int> _statsHistory = new Queue<int>();

			private float _rate, _cursor;
			private int _targetLatency;
			private long _rollingTotalSize;
			private uint _kAverageSize;

			public int Size { get; private set; }

			public void Clear()
			{
				_buffer.Clear();
				_statsHistory.Clear();
				_rollingTotalSize = 0;
				_targetLatency = (_maxLatency + _minLatency) / 2;
				_rate = 1.0f;
				_cursor = 0.0f;
				_curr[0] = _curr[1] = 0;
				_kAverageSize = 80000;
				Size = 0;
			}

			public void Enqueue(short left, short right)
			{
				_buffer.Enqueue(left);
				_buffer.Enqueue(right);
				Size++;
			}

			private void AddStatistic()
			{
				_statsHistory.Enqueue(Size);
				_rollingTotalSize += Size;
				if (_statsHistory.Count > _kAverageSize)
				{
					_rollingTotalSize -= _statsHistory.Peek();
					_statsHistory.Dequeue();

					float averageSize = (float)(_rollingTotalSize / _kAverageSize);
					////static int ctr=0;  ctr++; if((ctr&127)==0) printf("avg size: %f curr size: %d rate: %f\n",averageSize,size,rate);
					{
						float targetRate;
						if (averageSize < _targetLatency)
						{
							targetRate = 1.0f - ((_targetLatency - averageSize) / _kAverageSize);
						}
						else if (averageSize > _targetLatency)
						{
							targetRate = 1.0f + ((averageSize - _targetLatency) / _kAverageSize);
						}
						else
						{
							targetRate = 1.0f;
						}

						////rate = moveValueTowards(rate,targetRate,0.001f);
						_rate = targetRate;
					}
				}
			}

			public void Dequeue(out short left, out short right)
			{
				left = right = 0;
				AddStatistic();
				if (Size == 0)
				{
					return;
				}

				_cursor += _rate;
				while (_cursor > 1.0f)
				{
					_cursor -= 1.0f;
					if (Size > 0)
					{
						_curr[0] = _buffer.Dequeue();
						_curr[1] = _buffer.Dequeue();
						Size--;
					}
				}

				left = _curr[0];
				right = _curr[1];
			}
		}
	}

	internal class NitsujaSynchronizer : ISynchronizingAudioBuffer
	{
		private struct Ssamp
		{
			public readonly short L, R;

			public Ssamp(short left, short right)
			{
				L = left;
				R = right;
			}
		}

		private readonly List<Ssamp> _sampleQueue = new List<Ssamp>();

		// returns values going between 0 and y-1 in a saw wave pattern, based on x
		private static int Pingpong(int x, int y)
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

		private static Ssamp Crossfade(Ssamp lhs, Ssamp rhs, int cur, int start, int end)
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

			return new Ssamp((short)lrv, (short)rrv);
		}

		public void Clear()
		{
			_sampleQueue.Clear();
		}

		private static void EmitSample(short[] outbuf, ref int cursor, Ssamp sample)
		{
			outbuf[cursor++] = sample.L;
			outbuf[cursor++] = sample.R;
		}

		private static void EmitSamples(short[] outbuf, ref int outcursor, Ssamp[] samplebuf, int incursor, int samples)
		{
			for (int i = 0; i < samples; i++)
			{
				EmitSample(outbuf, ref outcursor, samplebuf[i + incursor]);
			}
		}

		private static short Abs(short value)
		{
			if (value < 0)
			{
				return (short)-value;
			}

			return value;
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
				_sampleQueue.Add(new Ssamp(buf[cursor + 0], buf[cursor + 1]));
				cursor += 2;
			}
		}

		public void EnqueueSample(short left, short right)
		{
			_sampleQueue.Add(new Ssamp(left, right));
		}

		public int OutputSamples(short[] buf, int samplesRequested)
		{
			Console.WriteLine("{0} {1}", samplesRequested, _sampleQueue.Count); // add this line

			int bufcursor = 0;
			int audiosize = samplesRequested;
			int queued = _sampleQueue.Count;

			// I am too lazy to deal with odd numbers
			audiosize &= ~1;
			queued &= ~1;

			if (queued > 0x200 && audiosize > 0) // is there any work to do?
			{
				// are we going at normal speed?
				// or more precisely, are the input and output queues/buffers of similar size?
				if (queued > 900 || audiosize > queued * 2)
				{
					// not normal speed. we have to resample it somehow in this case.
					if (audiosize <= queued)
					{
						// fast forward speed
						// this is the easy case, just crossfade it and it sounds ok
						for (int i = 0; i < audiosize; i++)
						{
							int j = i + queued - audiosize;
							Ssamp outsamp = Crossfade(_sampleQueue[i], _sampleQueue[j], i, 0, audiosize);
							EmitSample(buf, ref bufcursor, outsamp);
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
						// |   --> audiosize (this axis represents the output index we write to, right meaning forward in output time/position)
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
						int beststart = 0, extraAtEnd = 0;
						{
							int bestend = queued;
							const int Worstdiff = 99999999;
							int beststartdiff = Worstdiff;
							int bestenddiff = Worstdiff;
							for (int i = 0; i < 128; i += 2)
							{
								int diff = Abs(_sampleQueue[i].L - _sampleQueue[i + 1].L) + Abs(_sampleQueue[i].R - _sampleQueue[i + 1].R);
								if (diff < beststartdiff)
								{
									beststartdiff = diff;
									beststart = i;
								}
							}

							for (int i = queued - 3; i > queued - 3 - 128; i -= 2)
							{
								int diff = Abs(_sampleQueue[i].L - _sampleQueue[i + 1].L) + Abs(_sampleQueue[i].R - _sampleQueue[i + 1].R);
								if (diff < bestenddiff)
								{
									bestenddiff = diff;
									bestend = i + 1;
								}
							}

							extraAtEnd = queued - bestend;
							queued = bestend - beststart;

							int oksize = queued;
							while (oksize + (queued * 2) + beststart + extraAtEnd <= samplesRequested)
							{
								oksize += queued * 2;
							}

							audiosize = oksize;

							for (int x = 0; x < beststart; x++)
							{
								EmitSample(buf, ref bufcursor, _sampleQueue[x]);
							}

							// sampleQueue.erase(sampleQueue.begin(), sampleQueue.begin() + beststart);
							_sampleQueue.RemoveRange(0, beststart); // zero 08-nov-2010: did i do this right?
						}

						int midpointX = audiosize >> 1;
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
							int a = Abs(Pingpong(midpointX - midpointXOffset, queued) - midpointY) - midpointXOffset;
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
						int leftMidpointY = Pingpong(leftMidpointX, queued);
						int rightMidpointY = (queued - 1) - Pingpong((int)audiosize - 1 - rightMidpointX + (queued * 2), queued);

						// output the left almost-half of the sound (section "A")
						for (int x = 0; x < leftMidpointX; x++)
						{
							int i = Pingpong(x, queued);
							EmitSample(buf, ref bufcursor, _sampleQueue[i]);
						}

						// output the middle stretch (section "B")
						int y = leftMidpointY;
						int dyMidLeft = (leftMidpointY < midpointY) ? 1 : -1;
						int dyMidRight = (rightMidpointY > midpointY) ? 1 : -1;
						for (int x = leftMidpointX; x < midpointX; x++, y += dyMidLeft)
						{
							EmitSample(buf, ref bufcursor, _sampleQueue[y]);
						}

						for (int x = midpointX; x < rightMidpointX; x++, y += dyMidRight)
						{
							EmitSample(buf, ref bufcursor, _sampleQueue[y]);
						}

						// output the end of the queued sound (section "C")
						for (int x = rightMidpointX; x < audiosize; x++)
						{
							int i = (queued - 1) - Pingpong((int)audiosize - 1 - x + (queued * 2), queued);
							EmitSample(buf, ref bufcursor, _sampleQueue[i]);
						}

						for (int x = 0; x < extraAtEnd; x++)
						{
							int i = queued + x;
							EmitSample(buf, ref bufcursor, _sampleQueue[i]);
						}

						queued += extraAtEnd;
						audiosize += beststart + extraAtEnd;
					} // end else

					// sampleQueue.erase(sampleQueue.begin(), sampleQueue.begin() + queued);
					_sampleQueue.RemoveRange(0, queued);

					// zero 08-nov-2010: did i do this right?
					return audiosize;
				}
				else
				{
					// normal speed
					// just output the samples straightforwardly.
					//
					// at almost-full speeds (like 50/60 FPS)
					// what will happen is that we rapidly fluctuate between entering this branch
					// and entering the "slow motion speed" branch above.
					// but that's ok! because all of these branches sound similar enough that we can get away with it.
					// so the two cases actually complement each other.
					if (audiosize >= queued)
					{
						EmitSamples(buf, ref bufcursor, _sampleQueue.ToArray(), 0, queued);

						// sampleQueue.erase(sampleQueue.begin(), sampleQueue.begin() + queued);
						_sampleQueue.RemoveRange(0, queued);

						// zero 08-nov-2010: did i do this right?
						return queued;
					}
					else
					{
						EmitSamples(buf, ref bufcursor, _sampleQueue.ToArray(), 0, audiosize);

						// sampleQueue.erase(sampleQueue.begin(), sampleQueue.begin()+audiosize);
						_sampleQueue.RemoveRange(0, audiosize);

						// zero 08-nov-2010: did i do this right?
						return audiosize;
					}
				} // end normal speed
			} // end if there is any work to do
			else
			{
				return 0;
			}
		} // output_samples
	} // NitsujaSynchronizer

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
		// When fastforwarding, it will discard samples above the maximum excess buffer.

		// When underflowing, it will attempt to resample to a certain threshhold.
		// If it underflows beyond that threshhold, it will give up and output silence.
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

		private const int SamplesInOneFrame = 735;
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
				////Console.WriteLine("samples in buffer {0}, requested {1}", buffer.Count, samples_requested);
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
