using System.Diagnostics;
using System.Threading;

using BizHawk.Client.Common;
using BizHawk.Common;

using Windows.Win32;

//this throttle is nitsuja's fine-tuned techniques from desmume

namespace BizHawk.Client.EmuHawk
{
	public class Throttle
	{
		private int lastSkipRate;
		private int framesToSkip;
		public bool skipNextFrame;

		//if the emulator is paused then we don't need to behave as if unthrottled
		public bool signal_paused;
		public bool signal_frameAdvance;
		public bool signal_unthrottle;
		public bool signal_continuousFrameAdvancing;
		public bool signal_overrideSecondaryThrottle;

		public void Step(Config config, Sound sound, bool allowSleep, int forceFrameSkip)
		{
			//TODO - figure out what allowSleep is supposed to be used for
			//TODO - figure out what forceFrameSkip is supposed to be used for

			//if we're paused, none of this should happen. just clean out our state and don't skip
			//notably, if we're frame-advancing, we should be paused.
			if (signal_paused && !signal_continuousFrameAdvancing)
			{
				//Console.WriteLine($"THE THING: {signal_paused} {signal_continuousFrameAdvancing}");
				skipNextFrame = false;
				framesToSkip = 0;

				//keep from burning CPU
				Thread.Sleep(15);
				return;
			}

#if false
			//here's some ideas for how to begin cleaning this up
			////at this point, its assumed that we're running.
			////this could be a free run, an unthrottled run, or a 'continuous frame advance' (aka continuous) run
			////free run: affected by frameskips and throttles
			////unthrottled run: affected by frameskips only
			////continuous run: affected by frameskips and throttles
			////so continuous and free are the same?

			bool continuous_run = signal_continuousFrameAdvancing;
			bool unthrottled_run = signal_unthrottle;
			bool free_run = !continuous_run && !unthrottled_run;

			bool do_throttle, do_skip;
			if (continuous_run || free_run) do_throttle = do_skip = true;
			else if (unthrottled_run) do_skip = true;
			else throw new InvalidOperationException();
#endif

			int skipRate = (forceFrameSkip < 0) ? config.FrameSkip : forceFrameSkip;
			if (signal_unthrottle) skipRate = 3;

			if (lastSkipRate != skipRate)
			{
				lastSkipRate = skipRate;
				framesToSkip = 0; // otherwise switches to lower frameskip rates will lag behind
			}

			//don't ever skip frames when continuous frame advancing. it's meant for precision work.
			if (signal_continuousFrameAdvancing && !signal_unthrottle)
			{
				skipNextFrame = false;
			}
			else
			{
				skipNextFrame = framesToSkip > 0;
			}

			if ((signal_paused || config.ClockThrottle || signal_overrideSecondaryThrottle) && allowSleep)
			{
				SpeedThrottle(sound, signal_paused);
			}

			if (signal_unthrottle || !config.AutoMinimizeSkipping)
			{
				if (framesToSkip < 1)
					framesToSkip += skipRate;
			}
			else
			{
				if (!signal_continuousFrameAdvancing)
				{
					AutoFrameSkip_NextFrame();
					if (framesToSkip < 1)
						framesToSkip += AutoFrameSkip_GetSkipAmount(0, skipRate);
				}
			}

			framesToSkip--;
		}

		private static ulong GetCurTime()
		{
			if (tmethod == 1)
				return (ulong)Stopwatch.GetTimestamp();
			else
				return (ulong)Environment.TickCount;
		}

		private static readonly Func<uint, uint> TimeBeginPeriod = OSTailoredCode.IsUnixHost
			? u => u
			: Win32Imports.timeBeginPeriod;

		private static readonly int tmethod;
		private static readonly ulong afsfreq;
		private static readonly ulong tfreq;

		static Throttle()
		{
			TimeBeginPeriod(1);
			if (Stopwatch.IsHighResolution)
			{
				afsfreq = (ulong)Stopwatch.Frequency;
				tmethod = 1;
			}
			else
			{
				afsfreq = 1000;
				tmethod = 0;
			}
			Util.DebugWriteLine("throttle method: {0}; resolution: {1}", tmethod, afsfreq);
			tfreq = afsfreq * 65536;
		}

		public void SetCoreFps(double desired_fps)
		{
			core_desiredfps = (ulong)(65536 * desired_fps);
			int target_pct = pct;
			pct = -1;
			SetSpeedPercent(target_pct);
		}

		private int pct = -1;
		public void SetSpeedPercent(int percent)
		{
			//Console.WriteLine($"throttle set percent {percent}");
			if (pct == percent) return;
			pct = percent;
			float fraction = percent / 100.0f;
			desiredfps = (ulong)(core_desiredfps * fraction);
			//Console.WriteLine($"throttle set desiredfps {desiredfps}");
			desiredspf = 65536.0f / desiredfps;
			AutoFrameSkip_IgnorePreviousDelay();
		}

		private ulong core_desiredfps;
		private ulong desiredfps;
		private float desiredspf;

		private ulong ltime;
		private ulong beginticks, endticks, preThrottleEndticks;
		private float fSkipFrames;
		private float fSkipFramesError;
		private int lastSkip;
		private float lastError;
		private float integral;

		public void AutoFrameSkip_IgnorePreviousDelay()
		{
			beginticks = GetCurTime();

			// this seems to be a stable way of allowing the skip frames to
			// quickly adjust to a faster environment (e.g. after a loadstate)
			// without causing oscillation or a sudden change in skip rate
			fSkipFrames *= 0.5f;
		}

		private void AutoFrameSkip_BeforeThrottle()
		{
			preThrottleEndticks = GetCurTime();
		}

		private void AutoFrameSkip_NextFrame()
		{
			endticks = GetCurTime();

			// calculate time since last frame
			ulong diffticks = Math.Max(endticks - beginticks, 1);
			float diff = (float)diffticks / afsfreq;

			// calculate time since last frame not including throttle sleep time
			if (preThrottleEndticks == 0) // if we didn't throttle, use the non-throttle time
				preThrottleEndticks = endticks;
			ulong diffticksUnthrottled = preThrottleEndticks - beginticks;
			float diffUnthrottled = (float)diffticksUnthrottled / afsfreq;


			float error = diffUnthrottled - desiredspf;


			// reset way-out-of-range values
			if (diff > 1.0f)
				diff = 1.0f;
			if (!(-1.0f).RangeTo(1.0f).Contains(error))
				error = 0.0f;
			if (diffUnthrottled > 1.0f)
				diffUnthrottled = desiredspf;

			float derivative = (error - lastError) / diff;
			lastError = error;

			integral += error * diff;
			integral *= 0.99f; // since our integral isn't reliable, reduce it to 0 over time.

			// "PID controller" constants
			// this stuff is probably being done all wrong, but these seem to work ok
			const float Kp = 40.0f;
			const float Ki = 0.55f;
			const float Kd = 0.04f;

			float errorTerm = error * Kp;
			float derivativeTerm = derivative * Kd;
			float integralTerm = integral * Ki;
			float adjustment = errorTerm + derivativeTerm + integralTerm;

			// apply the output adjustment
			fSkipFrames += adjustment;

			// if we're running too slowly, prevent the throttle from kicking in
			if (adjustment > 0 && fSkipFrames > 0)
				ltime -= tfreq / desiredfps;

			preThrottleEndticks = 0;
			beginticks = GetCurTime();
		}

		private int AutoFrameSkip_GetSkipAmount(int min, int max)
		{
			int rv = (int)fSkipFrames;
			fSkipFramesError += fSkipFrames - rv;

			// resolve accumulated fractional error
			// where doing so doesn't push us out of range
			while (fSkipFramesError >= 1.0f && rv <= lastSkip && rv < max)
			{
				fSkipFramesError -= 1.0f;
				rv++;
			}
			while (fSkipFramesError <= -1.0f && rv >= lastSkip && rv > min)
			{
				fSkipFramesError += 1.0f;
				rv--;
			}

			// restrict skip amount to requested range
			if (rv < min)
				rv = min;
			if (rv > max)
				rv = max;

			// limit maximum error accumulation (it's mainly only for fractional components)
			if (fSkipFramesError >= 4.0f)
				fSkipFramesError = 4.0f;
			if (fSkipFramesError <= -4.0f)
				fSkipFramesError = -4.0f;

			// limit ongoing skipframes to requested range + 1 on each side
			if (fSkipFrames < min - 1)
				fSkipFrames = (float)min - 1;
			if (fSkipFrames > max + 1)
				fSkipFrames = (float)max + 1;

			//	printf("%d", rv);

			lastSkip = rv;
			return rv;
		}

		private void SpeedThrottle(Sound sound, bool paused)
		{
			AutoFrameSkip_BeforeThrottle();

			ulong timePerFrame = tfreq / desiredfps;

			while (true)
			{
				if (signal_unthrottle)
					return;

				ulong ttime = GetCurTime();
				ulong elapsedTime = ttime - ltime;

				if (elapsedTime >= timePerFrame)
				{
					int maxMissedFrames = (int)Math.Ceiling((sound.SoundMaxBufferDeficitMs / 1000.0) / ((double)timePerFrame / afsfreq));
					if (maxMissedFrames < 3)
						maxMissedFrames = 3;

					if (elapsedTime > timePerFrame * (ulong)(1 + maxMissedFrames))
						ltime = ttime;
					else
						ltime += timePerFrame;

					return;
				}

				int sleepTime = (int)((timePerFrame - elapsedTime) * 1000 / afsfreq);
				if (sleepTime >= 2 || paused)
				{
					switch (OSTailoredCode.CurrentOS)
					{
						case OSTailoredCode.DistinctOS.Linux: //TODO repro
						case OSTailoredCode.DistinctOS.macOS:
							// The actual sleep time on OS X with Mono is generally between the request time
							// and up to 25% over. So we'll scale the sleep time back to account for that.
							sleepTime = sleepTime * 4 / 5;
							break;
						case OSTailoredCode.DistinctOS.Windows:
							// Assuming a timer period of 1 ms (i.e. TimeBeginPeriod(1)): The actual sleep time
							// on Windows XP is generally within a half millisecond either way of the requested
							// time. The actual sleep time on Windows 8 is generally between the requested time
							// and up to a millisecond over. So we'll subtract 1 ms from the time to avoid
							// sleeping longer than desired.
							sleepTime -= 1;
							break;
					}

					Thread.Sleep(Math.Max(sleepTime, 1));
				}
				else if (sleepTime > 0) // spin for <1 millisecond waits
				{
					Thread.Yield(); // limit to other threads on the same CPU core for other short waits
				}
			}
		}
	}
}
