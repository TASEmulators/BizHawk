using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using BizHawk.Client.Common;

//this throttle is nitsuja's fine-tuned techniques from desmume

namespace BizHawk.Client.EmuHawk
{
	public class Throttle
	{
		int lastskiprate;
		int framestoskip;
		int framesskipped;
		public bool skipnextframe;

		//if the emulator is paused then we dont need to behave as if unthrottled
		public bool signal_paused;
		public bool signal_frameAdvance;
		public bool signal_unthrottle;
		public bool signal_continuousframeAdvancing; //continuousframeAdvancing
		public bool signal_overrideSecondaryThrottle;

		public int cfg_frameskiprate
		{
			get
			{
				return Global.Config.FrameSkip;
			}
		}

		public bool cfg_frameLimit
		{
			get
			{
				return Global.Config.ClockThrottle;
			}
		}

		public bool cfg_autoframeskipenab
		{
			get
			{
				return Global.Config.AutoMinimizeSkipping;
			}
		}

		public void Step(bool allowSleep, int forceFrameSkip)
		{
			//TODO - figure out what allowSleep is supposed to be used for
			//TODO - figure out what forceFrameSkip is supposed to be used for

			bool extraThrottle = false;

			//if we're paused, none of this should happen. just clean out our state and dont skip
			//notably, if we're frame-advancing, we should be paused.
			if (signal_paused && !signal_continuousframeAdvancing)
			{
				//Console.WriteLine("THE THING: {0} {1}", signal_paused ,signal_continuousframeAdvancing);
				skipnextframe = false;
				framesskipped = 0;
				framestoskip = 0;

				//keep from burning CPU
				Thread.Sleep(10);
				return;
			}

			//heres some ideas for how to begin cleaning this up
			////at this point, its assumed that we're running.
			////this could be a free run, an unthrottled run, or a 'continuous frame advance' (aka continuous) run
			////free run: affected by frameskips and throttles
			////unthrottled run: affected by frameskips only
			////continuous run: affected by frameskips and throttles
			////so continuous and free are the same?

			//bool continuous_run = signal_continuousframeAdvancing;
			//bool unthrottled_run = signal_unthrottle;
			//bool free_run = !continuous_run && !unthrottled_run;

			//bool do_throttle, do_skip;
			//if (continuous_run || free_run)
			//  do_throttle = do_skip = true;
			//else if (unthrottled_run)
			//  do_skip = true;
			//else throw new InvalidOperationException();

			int skipRate = (forceFrameSkip < 0) ? cfg_frameskiprate : forceFrameSkip;
			int ffSkipRate = (forceFrameSkip < 0) ? 3 : forceFrameSkip;

			if (lastskiprate != skipRate)
			{
				lastskiprate = skipRate;
				framestoskip = 0; // otherwise switches to lower frameskip rates will lag behind
			}

			if (!skipnextframe || forceFrameSkip == 0 || (signal_continuousframeAdvancing && !signal_unthrottle))
			{
				framesskipped = 0;

				if (signal_continuousframeAdvancing)
				{
					//dont ever skip frames when continuous frame advancing. it's meant for precision work.
					//but we DO need to throttle
					if(Global.Config.ClockThrottle)
						extraThrottle = true;
				}
				else
				{
					if (framestoskip > 0)
						skipnextframe = true;
				}
			}
			else
			{
				framestoskip--;

				if (framestoskip < 1)
					skipnextframe = false;
				else
				  skipnextframe = true;

				framesskipped++;
			}

			if (signal_unthrottle)
			{
				if (framesskipped < ffSkipRate)
				{
					skipnextframe = true;
					framestoskip = 1;
				}
				if (framestoskip < 1)
					framestoskip += ffSkipRate;
			}
			else if ((extraThrottle || signal_paused || /*autoframeskipenab && frameskiprate ||*/ cfg_frameLimit || signal_overrideSecondaryThrottle) && allowSleep)
			{
				SpeedThrottle(signal_paused);
			}

			if (cfg_autoframeskipenab && cfg_frameskiprate != 0)
			{
				if (!signal_continuousframeAdvancing)
				{
					AutoFrameSkip_NextFrame();
					if (framestoskip < 1)
						framestoskip += AutoFrameSkip_GetSkipAmount(0, skipRate);
				}
			}
			else
			{
				if (framestoskip < 1)
					framestoskip += skipRate;
			}
		}

		static ulong GetCurTime()
		{
			if (tmethod == 1)
				return (ulong)Stopwatch.GetTimestamp();
			else
				return (ulong)Environment.TickCount;
		}

#if WINDOWS
		[DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
		static extern uint timeBeginPeriod(uint uMilliseconds);
#endif

		static readonly int tmethod;
		static readonly ulong afsfreq;
		static readonly ulong tfreq;

		static Throttle()
		{
#if WINDOWS
			timeBeginPeriod(1);
#endif
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
			Console.WriteLine("throttle method: {0}; resolution: {1}", tmethod, afsfreq);
			tfreq = afsfreq * 65536;
		}

		public void SetCoreFps(double desired_fps)
		{
			core_desiredfps = (ulong)(65536 * desired_fps);
			int target_pct = pct;
			pct = -1;
			SetSpeedPercent(target_pct);
		}

		int pct = -1;
		public void SetSpeedPercent(int percent)
		{
			//Console.WriteLine("throttle set percent " + percent);
			if (pct == percent) return;
			pct = percent;
			float fraction = percent / 100.0f;
			desiredfps = (ulong)(core_desiredfps * fraction);
			//Console.WriteLine("throttle set desiredfps " + desiredfps);
			desiredspf = 65536.0f / desiredfps;
			AutoFrameSkip_IgnorePreviousDelay();
		}

		ulong core_desiredfps;
		ulong desiredfps;
		float desiredspf;

		ulong ltime;
		ulong beginticks, endticks, preThrottleEndticks;
		float fSkipFrames;
		float fSkipFramesError;
		int lastSkip;
		float lastError;
		float integral;

		public void AutoFrameSkip_IgnorePreviousDelay()
		{
			beginticks = GetCurTime();

			// this seems to be a stable way of allowing the skip frames to
			// quickly adjust to a faster environment (e.g. after a loadstate)
			// without causing oscillation or a sudden change in skip rate
			fSkipFrames *= 0.5f;
		}

		void AutoFrameSkip_BeforeThrottle()
		{
			preThrottleEndticks = GetCurTime();
		}

		void AutoFrameSkip_NextFrame()
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
			if (diff > 1)
				diff = 1;
			if (error > 1 || error < -1)
				error = 0;
			if (diffUnthrottled > 1)
				diffUnthrottled = desiredspf;

			float derivative = (error - lastError) / diff;
			lastError = error;

			integral = integral + (error * diff);
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

		int AutoFrameSkip_GetSkipAmount(int min, int max)
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

		void SpeedThrottle(bool paused)
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
					int maxMissedFrames = (int)Math.Ceiling((Global.SoundMaxBufferDeficitMs / 1000.0) / ((double)timePerFrame / afsfreq));
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
#if WINDOWS
					// Assuming a timer period of 1 ms (i.e. timeBeginPeriod(1)): The actual sleep time
					// on Windows XP is generally within a half millisecond either way of the requested
					// time. The actual sleep time on Windows 8 is generally between the requested time
					// and up to a millisecond over. So we'll subtract 1 ms from the time to avoid
					// sleeping longer than desired.
					sleepTime -= 1;
#else
					// The actual sleep time on OS X with Mono is generally between the request time
					// and up to 25% over. So we'll scale the sleep time back to account for that.
					sleepTime = sleepTime * 4 / 5;
#endif

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
