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
