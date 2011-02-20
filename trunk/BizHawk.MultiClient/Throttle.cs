using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

//this throttle is nitsuja's fine-tuned techniques from desmume

namespace BizHawk.MultiClient
{
	class Throttle
	{
		static ulong GetCurTime()
		{
			if (tmethod == 1)
			{
				ulong tmp;
				QueryPerformanceCounter(out tmp);
				return (ulong)tmp;
			}
			else
			{
				return (ulong)GetTickCount();
			}
		}

		[DllImport("kernel32.dll")]
		static extern uint GetTickCount();

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool QueryPerformanceCounter(out ulong lpPerformanceCount);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool QueryPerformanceFrequency(out ulong frequency);

		static int tmethod;
		static ulong afsfreq;
		static ulong tfreq;

		static Throttle()
		{
			tmethod = 0;
			if (QueryPerformanceFrequency(out afsfreq))
				tmethod = 1;
			else
				afsfreq = 1000;
			tfreq = afsfreq << 16;
		}

		public Throttle(float desired_fps)
		{
			core_desiredfps = (ulong)(65536 * desired_fps);
			desiredfps = core_desiredfps;
			desiredspf = 65536.0f / core_desiredfps;
			AutoFrameSkip_IgnorePreviousDelay();
		}

		ulong core_desiredfps;
		ulong desiredfps;
		float desiredspf;

		ulong ltime;
		ulong beginticks = 0, endticks = 0, preThrottleEndticks = 0;
		float fSkipFrames = 0;
		float fSkipFramesError = 0;
		int lastSkip = 0;
		float lastError = 0;
		float integral = 0;

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
			ulong diffticks = endticks - beginticks;
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

	}
}