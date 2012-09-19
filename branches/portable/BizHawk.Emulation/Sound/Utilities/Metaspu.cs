using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Sound
{
	public class MetaspuSoundProvider : ISoundProvider
	{
		public ISynchronizingAudioBuffer buffer;
		public MetaspuSoundProvider(ESynchMethod method)
		{
			buffer = Metaspu.metaspu_construct(method);
		}

		public MetaspuSoundProvider() : this(ESynchMethod.ESynchMethod_V)
		{
		}

        short[] pullBuffer = new short[1470];
        public void PullSamples(ISoundProvider source)
        {
            Array.Clear(pullBuffer, 0, 1470);
            source.GetSamples(pullBuffer);
            buffer.enqueue_samples(pullBuffer, 735);
        }

		public void GetSamples(short[] samples)
		{
			buffer.output_samples(samples, samples.Length / 2);
		}

		public void DiscardSamples()
		{
			buffer.clear();
		}

        public int MaxVolume { get; set; }
    }

	public interface ISynchronizingAudioBuffer
	{
		void enqueue_samples(short[] buf, int samples_provided);
		void enqueue_sample(short left, short right);
		void clear();
		
		//returns the number of samples actually supplied, which may not match the number requested
		// ^^ what the hell is that supposed to mean.
		// the entire point of an ISynchronzingAudioBuffer
		// is to provide exact amounts of output samples,
		// even when the input provided varies....
		int output_samples(short[] buf, int samples_requested);
	};

	public enum ESynchMethod
	{
		ESynchMethod_N, //nitsuja's
		ESynchMethod_Z, //zero's
		//ESynchMethod_P, //PCSX2 spu2-x //ohno! not available yet in c#
        ESynchMethod_V // vecna
	};
	
	public static class Metaspu
	{
		public static ISynchronizingAudioBuffer metaspu_construct(ESynchMethod method)
		{
			switch (method)
			{
				case ESynchMethod.ESynchMethod_Z:
					return new ZeromusSynchronizer();
				case ESynchMethod.ESynchMethod_N:
					return new NitsujaSynchronizer();
                case ESynchMethod.ESynchMethod_V:
                    return new VecnaSynchronizer();
				default:
					return new NitsujaSynchronizer();
			}
		}
	}

		
	class ZeromusSynchronizer : ISynchronizingAudioBuffer
	{
		public ZeromusSynchronizer()
		{
			//#ifdef NDEBUG
			adjustobuf = new Adjustobuf(200, 1000);
			//#else
			//adjustobuf = new Adjustobuf(22000, 44000);
			//#endif
			
		}

		//adjustobuf(200,1000)
		bool mixqueue_go = false;

		public void clear()
		{
			adjustobuf.clear();
		}

		public void enqueue_sample(short left, short right)
		{
			adjustobuf.enqueue(left, right);
		}

		public void enqueue_samples(short[] buf, int samples_provided)
		{
			int ctr = 0;
			for (int i = 0; i < samples_provided; i++)
			{
				short left = buf[ctr++];
				short right = buf[ctr++];
				adjustobuf.enqueue(left, right);
			}
		}

		//returns the number of samples actually supplied, which may not match the number requested
		public int output_samples(short[] buf, int samples_requested)
		{
            int ctr=0;
			int done = 0;
			if (!mixqueue_go)
			{
				if (adjustobuf.size > 200)
					mixqueue_go = true;
			}
			else
			{
				for (int i = 0; i < samples_requested; i++)
				{
					if (adjustobuf.size == 0)
					{
						mixqueue_go = false;
						break;
					}
					done++;
					short left, right;
					adjustobuf.dequeue(out left, out right);
					buf[ctr++] = left;
					buf[ctr++] = right;
				}
			}

			return done;
		}

		Adjustobuf adjustobuf;
		class Adjustobuf
		{
			public Adjustobuf(int _minLatency, int _maxLatency)
			{
				minLatency = _minLatency;
				maxLatency = _maxLatency;
				clear();
			}

			float rate, cursor;
			int minLatency, targetLatency, maxLatency;
			Queue<short> buffer = new Queue<short>();
			Queue<int> statsHistory = new Queue<int>();
			public int size = 0;
			short[] curr = new short[2];

			public void clear()
			{
				buffer.Clear();
				statsHistory.Clear();
				rollingTotalSize = 0;
				targetLatency = (maxLatency + minLatency) / 2;
				rate = 1.0f;
				cursor = 0.0f;
				curr[0] = curr[1] = 0;
				kAverageSize = 80000;
				size = 0;
			}

			public void enqueue(short left, short  right) 
			{
				buffer.Enqueue(left);
				buffer.Enqueue(right);
				size++;
			}

			long rollingTotalSize;

			uint kAverageSize;

			void addStatistic()
			{
				statsHistory.Enqueue(size);
				rollingTotalSize += size;
				if (statsHistory.Count > kAverageSize)
				{
					rollingTotalSize -= statsHistory.Peek();
					statsHistory.Dequeue();

					float averageSize = (float)(rollingTotalSize / kAverageSize);
					//static int ctr=0;  ctr++; if((ctr&127)==0) printf("avg size: %f curr size: %d rate: %f\n",averageSize,size,rate);
					{
						float targetRate;
						if(averageSize < targetLatency)
						{
							targetRate = 1.0f - (targetLatency-averageSize)/kAverageSize;
						}
						else if(averageSize > targetLatency) {
							targetRate = 1.0f + (averageSize-targetLatency)/kAverageSize;
						} else targetRate = 1.0f;
					
						//rate = moveValueTowards(rate,targetRate,0.001f);
						rate = targetRate;
					}

				}


			}

			public void dequeue(out short left, out short right)
			{
				left = right = 0; 
				addStatistic();
				if(size==0) { return; }
				cursor += rate;
				while(cursor>1.0f) {
					cursor -= 1.0f;
					if(size>0) {
						curr[0] = buffer.Dequeue();
						curr[1] = buffer.Dequeue();
						size--;
					}
				}
				left = curr[0]; 
				right = curr[1];
			}
		}
	}

	class NitsujaSynchronizer : ISynchronizingAudioBuffer
	{
		struct ssamp
		{
			public short l, r;
			public ssamp(short ll, short rr) { l = ll; r = rr; }
		};

		List<ssamp> sampleQueue = new List<ssamp>();

		// returns values going between 0 and y-1 in a saw wave pattern, based on x
		static int pingpong(int x, int y)
		{
			x %= 2*y;
			if(x >= y)
				x = 2*y - x - 1;
			return x;

			// in case we want to switch to odd buffer sizes for more sharpness
			//x %= 2*(y-1);
			//if(x >= y)
			//	x = 2*(y-1) - x;
			//return x;
		}

		static ssamp crossfade (ssamp lhs, ssamp rhs,  int cur, int start, int end)
		{
			if(cur <= start)
				return lhs;
			if(cur >= end)
				return rhs;

			// in case we want sine wave interpolation instead of linear here
			//float ang = 3.14159f * (float)(cur - start) / (float)(end - start);
			//cur = start + (int)((1-cosf(ang))*0.5f * (end - start));

			int inNum = cur - start;
			int outNum = end - cur;
			int denom = end - start;

			int lrv = ((int)lhs.l * outNum + (int)rhs.l * inNum) / denom;
			int rrv = ((int)lhs.r * outNum + (int)rhs.r * inNum) / denom;

			return new ssamp((short)lrv,(short)rrv);
		}

		public void clear()
		{
			sampleQueue.Clear();
		}

		static void emit_sample(short[] outbuf, ref int cursor, ssamp sample)
		{
			outbuf[cursor++] = sample.l;
			outbuf[cursor++] = sample.r;
		}

		static void emit_samples(short[] outbuf, ref int outcursor, ssamp[] samplebuf, int incursor, int samples)
		{
			for(int i=0;i<samples;i++)
				emit_sample(outbuf,ref outcursor, samplebuf[i+incursor]);
		}

		static short abs(short value)
		{
			if (value < 0) return (short)-value;
			else return value;
		}

		static int abs(int value)
		{
			if (value < 0) return -value;
			else return value;
		}

		public void enqueue_samples(short[] buf, int samples_provided)
		{
			int cursor = 0;
			for(int i=0;i<samples_provided;i++)
			{
				sampleQueue.Add(new ssamp(buf[cursor+0],buf[cursor+1]));
				cursor += 2;
			}
		}

		public void enqueue_sample(short left, short right)
		{
			sampleQueue.Add(new ssamp(left,right));
		}

		public int output_samples(short[] buf, int samples_requested)
		{
            Console.WriteLine("{0} {1}", samples_requested, sampleQueue.Count); //add this line


			int bufcursor = 0;
		int audiosize = samples_requested;
		int queued = sampleQueue.Count;

		// I am too lazy to deal with odd numbers
		audiosize &= ~1;
		queued &= ~1;

		if(queued > 0x200 && audiosize > 0) // is there any work to do?
		{
			// are we going at normal speed?
			// or more precisely, are the input and output queues/buffers of similar size?
			if(queued > 900 || audiosize > queued * 2)
			{
				// not normal speed. we have to resample it somehow in this case.
				if(audiosize <= queued)
				{
					// fast forward speed
					// this is the easy case, just crossfade it and it sounds ok
					for(int i = 0; i < audiosize; i++)
					{
						int j = i + queued - audiosize;
						ssamp outsamp = crossfade(sampleQueue[i],sampleQueue[j], i,0,audiosize);
						emit_sample(buf,ref bufcursor,outsamp);
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
						const int worstdiff = 99999999;
						int beststartdiff = worstdiff;
						int bestenddiff = worstdiff;
						for(int i = 0; i < 128; i+=2)
						{
							int diff = abs(sampleQueue[i].l - sampleQueue[i+1].l) + abs(sampleQueue[i].r - sampleQueue[i+1].r);
							if(diff < beststartdiff)
							{
								beststartdiff = diff;
								beststart = i;
							}
						}
						for(int i = queued-3; i > queued-3-128; i-=2)
						{
							int diff = abs(sampleQueue[i].l - sampleQueue[i+1].l) + abs(sampleQueue[i].r - sampleQueue[i+1].r);
							if(diff < bestenddiff)
							{
								bestenddiff = diff;
								bestend = i+1;
							}
						}

						extraAtEnd = queued - bestend;
						queued = bestend - beststart;

						int oksize = queued;
						while(oksize + queued*2 + beststart + extraAtEnd <= samples_requested)
							oksize += queued*2;
						audiosize = oksize;

						for(int x = 0; x < beststart; x++)
						{
							emit_sample(buf,ref bufcursor,sampleQueue[x]);
						}
						//sampleQueue.erase(sampleQueue.begin(), sampleQueue.begin() + beststart);
						sampleQueue.RemoveRange(0, beststart);
						//zero 08-nov-2010: did i do this right?
					}


					int midpointX = audiosize >> 1;
					int midpointY = queued >> 1;

					// all we need to do here is calculate the X position of the leftmost "B" in the above diagram.
					// TODO: we should calculate it with a simple equation like
					//   midpointXOffset = min(something,somethingElse);
					// but it's a little difficult to work it out exactly
					// so here's a stupid search for the value for now:

					int prevA = 999999;
					int midpointXOffset = queued/2;
					while(true)
					{
						int a = abs(pingpong(midpointX - midpointXOffset, queued) - midpointY) - midpointXOffset;
						if(((a > 0) != (prevA > 0) || (a < 0) != (prevA < 0)) && prevA != 999999)
						{
							if(((a + prevA)&1)!=0) // there's some sort of off-by-one problem with this search since we're moving diagonally...
								midpointXOffset++; // but this fixes it most of the time...
							break; // found it
						}
						prevA = a;
						midpointXOffset--;
						if(midpointXOffset < 0)
						{
							midpointXOffset = 0;
							break; // failed to find it. the two sides probably meet exactly in the center.
						}
					}

					int leftMidpointX = midpointX - midpointXOffset;
					int rightMidpointX = midpointX + midpointXOffset;
					int leftMidpointY = pingpong(leftMidpointX, queued);
					int rightMidpointY = (queued-1) - pingpong((int)audiosize-1 - rightMidpointX + queued*2, queued);

					// output the left almost-half of the sound (section "A")
					for(int x = 0; x < leftMidpointX; x++)
					{
						int i = pingpong(x, queued);
						emit_sample(buf,ref bufcursor,sampleQueue[i]);
					}

					// output the middle stretch (section "B")
					int y = leftMidpointY;
					int dyMidLeft  = (leftMidpointY  < midpointY) ? 1 : -1;
					int dyMidRight = (rightMidpointY > midpointY) ? 1 : -1;
					for(int x = leftMidpointX; x < midpointX; x++, y+=dyMidLeft)
						emit_sample(buf,ref bufcursor,sampleQueue[y]);
					for(int x = midpointX; x < rightMidpointX; x++, y+=dyMidRight)
						emit_sample(buf, ref bufcursor, sampleQueue[y]);

					// output the end of the queued sound (section "C")
					for(int x = rightMidpointX; x < audiosize; x++)
					{
						int i = (queued-1) - pingpong((int)audiosize-1 - x + queued*2, queued);
						emit_sample(buf,ref bufcursor,sampleQueue[i]);
					}

					for(int x = 0; x < extraAtEnd; x++)
					{
						int i = queued + x;
						emit_sample(buf,ref bufcursor,sampleQueue[i]);
					}
					queued += extraAtEnd;
					audiosize += beststart + extraAtEnd;
				} //end else

				//sampleQueue.erase(sampleQueue.begin(), sampleQueue.begin() + queued);
				sampleQueue.RemoveRange(0, queued);
				//zero 08-nov-2010: did i do this right?
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

				if(audiosize >= queued)
				{
					emit_samples(buf,ref bufcursor, sampleQueue.ToArray(),0,queued);
					//sampleQueue.erase(sampleQueue.begin(), sampleQueue.begin() + queued);
					sampleQueue.RemoveRange(0, queued);
					//zero 08-nov-2010: did i do this right?
					return queued;
				}
				else
				{
					emit_samples(buf,ref bufcursor, sampleQueue.ToArray(),0,audiosize);
					//sampleQueue.erase(sampleQueue.begin(), sampleQueue.begin()+audiosize);
					sampleQueue.RemoveRange(0, audiosize);
					//zero 08-nov-2010: did i do this right?
					return audiosize;
				}

			} //end normal speed

		} //end if there is any work to do
		else
		{
			return 0;
		}

	} //output_samples


}; //NitsujaSynchronizer

    class VecnaSynchronizer : ISynchronizingAudioBuffer
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

        struct Sample
        {
            public short left, right;
            public Sample(short l, short r)
            {
                left = l;
                right = r;
            }
        }

        Queue<Sample> buffer;
        Sample[] resampleBuffer;

        const int SamplesInOneFrame = 735;
        const int MaxExcessSamples = 2048;

        public VecnaSynchronizer()
        {
            buffer = new Queue<Sample>(2048);
            resampleBuffer = new Sample[2730]; // 2048 * 1.25

            // Give us a little buffer wiggle-room
            for (int i=0; i<367; i++)
                buffer.Enqueue(new Sample(0,0));
        }

        public void enqueue_samples(short[] buf, int samples_provided)
        {
            int ctr = 0;
            for (int i = 0; i < samples_provided; i++)
            {
                short left = buf[ctr++];
                short right = buf[ctr++];
                enqueue_sample(left, right);
            }
        }

        public void enqueue_sample(short left, short right)
        {
            if (buffer.Count >= MaxExcessSamples - 1)
            {
                // if buffer is overfull, dequeue old samples to make room for new samples.
                buffer.Dequeue();
            }
            buffer.Enqueue(new Sample(left, right));
        }

        public void clear()
        {
            buffer.Clear();
        }

        public int output_samples(short[] buf, int samples_requested)
        {
            if (samples_requested > buffer.Count)
            {
                // underflow!
                if (buffer.Count > samples_requested * 3 / 4)
                {
                    // if we're within 75% of target, then I guess we suck it up and resample.
                    // we sample in a goofy way, we could probably do it a bit smarter, if we cared more.

                    int samples_available = buffer.Count;
                    for (int i = 0; buffer.Count > 0; i++)
                        resampleBuffer[i] = buffer.Dequeue();

                    int index = 0;
                    for (int i = 0; i<samples_requested; i++)
                    {
                        Sample sample = resampleBuffer[i*samples_available/samples_requested];
                        buf[index++] += sample.left;
                        buf[index++] += sample.right;
                    }
                } else {
                    // we're outside of a "reasonable" underflow. Give up and output silence.
                    // Do nothing. The whole frame will be excess buffer.
                }
            } 
            else
            {
                // normal operation
                //Console.WriteLine("samples in buffer {0}, requested {1}", buffer.Count, samples_requested);
                int index = 0;
                for (int i = 0; i < samples_requested && buffer.Count > 0; i++)
                {
                    Sample sample = buffer.Dequeue();
                    buf[index++] += sample.left;
                    buf[index++] += sample.right;
                }
            }
            return samples_requested;
        }
    }
}