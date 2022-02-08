/* Copyright (c) 2017-2020 Hans-Kristian Arntzen
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#define __USE_MINGW_ANSI_STDIO 1
#define __STDC_FORMAT_MACROS 1
#include <inttypes.h>

#include "wsi_timing.hpp"
#include "wsi.hpp"
#include <string.h>
#include <algorithm>
#include <cmath>

#ifndef _WIN32
#include <time.h>
#endif

namespace Vulkan
{
void WSITiming::init(WSIPlatform *platform_, Device *device_, VkSwapchainKHR swapchain_, const WSITimingOptions &options_)
{
	platform = platform_;
	device = device_->get_device();
	table = &device_->get_device_table();
	swapchain = swapchain_;
	options = options_;

	serial_info = {};
	pacing = {};
	last_frame = {};
	feedback = {};
	smoothing = {};
	feedback.timing_buffer.resize(64);
}

void WSITiming::set_debug_enable(bool enable)
{
	options.debug = enable;
}

void WSITiming::set_latency_limiter(LatencyLimiter limiter)
{
	options.latency_limiter = limiter;
}

const WSITimingOptions &WSITiming::get_options() const
{
	return options;
}

void WSITiming::set_swap_interval(unsigned interval)
{
	if (interval == options.swap_interval || interval == 0)
		return;

	// First, extrapolate to our current serial so we can make a more correct target time using the new swap interval.
	uint64_t target = compute_target_present_time_for_serial(serial_info.serial);
	if (target)
	{
		pacing.base_serial = serial_info.serial;
		pacing.base_present = target;
	}

	options.swap_interval = interval;
}

void WSITiming::update_refresh_interval()
{
	VkRefreshCycleDurationGOOGLE refresh;
	if (table->vkGetRefreshCycleDurationGOOGLE(device, swapchain, &refresh) == VK_SUCCESS)
	{
		if (!feedback.refresh_interval || options.debug)
			LOGI("Observed refresh rate: %.6f Hz.\n", 1e9 / refresh.refreshDuration);
		feedback.refresh_interval = refresh.refreshDuration;
	}
	else
		LOGE("Failed to get refresh cycle duration.\n");
}

WSITiming::Timing *WSITiming::find_latest_timestamp(uint32_t start_serial)
{
	for (uint32_t i = 1; i < NUM_TIMINGS - 1; i++)
	{
		uint32_t past_serial = start_serial - i;
		auto &past = feedback.past_timings[past_serial & NUM_TIMING_MASK];
		if (past.wall_serial == past_serial && past.timing.actualPresentTime != 0)
			return &past;
	}

	return nullptr;
}

void WSITiming::update_past_presentation_timing()
{
	// Update past presentation timings.
	uint32_t presentation_count;
	if (table->vkGetPastPresentationTimingGOOGLE(device, swapchain, &presentation_count, nullptr) != VK_SUCCESS)
		return;

	if (presentation_count)
	{
		if (presentation_count > feedback.timing_buffer.size())
			feedback.timing_buffer.resize(presentation_count);
		auto res = table->vkGetPastPresentationTimingGOOGLE(device, swapchain, &presentation_count, feedback.timing_buffer.data());

		// I have a feeling this is racy in nature and we might have received another presentation timing in-between
		// querying count and getting actual data, so accept INCOMPLETE here.
		if (res == VK_SUCCESS || res == VK_INCOMPLETE)
		{
			for (uint32_t i = 0; i < presentation_count; i++)
			{
				auto &t = feedback.past_timings[feedback.timing_buffer[i].presentID & NUM_TIMING_MASK];
				if (t.wall_serial == feedback.timing_buffer[i].presentID)
				{
					t.timing = feedback.timing_buffer[i];

					uint64_t gpu_done_time = (t.timing.earliestPresentTime - t.timing.presentMargin);
					t.slack = int64_t(t.timing.actualPresentTime - gpu_done_time);
					t.pipeline_latency = int64_t(gpu_done_time - t.wall_frame_begin);

					// Expected result unless proven otherwise.
					t.result = TimingResult::Expected;

					// Feed the heuristics on when to drop frame rate or promote it.
					if ((feedback.refresh_interval != 0) &&
					    (t.timing.earliestPresentTime < t.timing.actualPresentTime) &&
					    (t.timing.presentMargin > feedback.refresh_interval / 4))
					{
						// We could have presented earlier, and we had decent GPU margin to do so.
						// Deal with frame dropping later.
						t.result = TimingResult::VeryEarly;
						if (options.debug)
							LOGI("Frame completed very early, but was held back by swap interval!\n");
					}
				}

				update_frame_pacing(t.wall_serial, t.timing.actualPresentTime, false);
			}
		}
	}

	auto *timing = find_latest_timestamp(serial_info.serial);
	if (timing && timing->timing.actualPresentTime >= timing->wall_frame_begin)
	{
		auto total_latency = timing->timing.actualPresentTime - timing->wall_frame_begin;
		feedback.latency = 0.99 * feedback.latency + 0.01e-9 * total_latency;

		if (options.debug)
		{
			LOGI("Have presentation timing for %u frames in the past.\n",
			     serial_info.serial - timing->timing.presentID);
		}

		if (int64_t(timing->timing.presentMargin) < 0)
			LOGE("Present margin is negative (%" PRId64 ") ... ?!\n", timing->timing.presentMargin);

		if (timing->timing.earliestPresentTime > timing->timing.actualPresentTime)
			LOGE("Earliest present time is > actual present time ... Bug?\n");

		if (timing->timing.actualPresentTime < timing->timing.desiredPresentTime)
		{
			LOGE("Image was presented before desired present time, bug? (actual: %" PRIu64 ", desired: %" PRIu64 "\n",
			     timing->timing.actualPresentTime,
			     timing->timing.desiredPresentTime);
		}
		else if (feedback.refresh_interval != 0 && timing->timing.desiredPresentTime != 0)
		{
			uint64_t delta = timing->timing.actualPresentTime - timing->timing.desiredPresentTime;
			if (delta >= feedback.refresh_interval)
			{
				LOGE("*** Image was presented %u frames late "
				     "(target: %.3f ms, rounded target: %.3f ms, actual: %.3f ms) compared to desired target. "
				     "This normally happens in startup phase, but otherwise it's either a real hitch or app bug. ***\n",
				     unsigned(delta / feedback.refresh_interval),
				     timing->wall_frame_target * 1e-6, timing->timing.desiredPresentTime * 1e-6,
				     timing->timing.actualPresentTime * 1e-6);
			}
		}

		// How much can we squeeze latency?
		if (options.debug)
			LOGI("Total latency: %.3f ms, slack time: %.3f\n", total_latency * 1e-6, timing->slack * 1e-6);

		if (last_frame.serial && timing->wall_serial != last_frame.serial)
		{
			double frame_time_ns = double(timing->timing.actualPresentTime - last_frame.present_time) /
			                       double(timing->wall_serial - last_frame.serial);

			// We only detect a hitch if we have the same swap interval target,
			// otherwise it might as well just be a transient state thing.
			if ((timing->swap_interval_target == options.swap_interval) &&
			    (feedback.refresh_interval != 0) &&
			    (frame_time_ns > 1.1 * options.swap_interval * feedback.refresh_interval))
			{
				LOGE("*** HITCH DETECTED ***\n");
				timing->result = TimingResult::TooLate;

				if (platform)
				{
					unsigned frame_delta = unsigned(round(frame_time_ns / (options.swap_interval *
					                                                       feedback.refresh_interval)));
					VK_ASSERT(frame_delta);
					unsigned dropped_frames = frame_delta - 1;
					platform->event_display_timing_stutter(serial_info.serial, timing->wall_serial, dropped_frames);
				}
			}

			if (options.debug)
			{
				LOGI("Frame time ID #%u: %.3f ms\n",
				     timing->wall_serial,
				     1e-6 * frame_time_ns);
			}
		}

		last_frame.serial = timing->wall_serial;
		last_frame.present_time = timing->timing.actualPresentTime;
	}
}

void WSITiming::wait_until(int64_t nsecs)
{
#ifdef __linux__
	timespec ts;
	ts.tv_sec = nsecs / 1000000000;
	ts.tv_nsec = nsecs % 1000000000;
	clock_nanosleep(CLOCK_MONOTONIC, TIMER_ABSTIME, &ts, nullptr);
#else
	(void)nsecs;
#endif
}

uint64_t WSITiming::get_wall_time()
{
#ifndef _WIN32
	// GOOGLE_display_timing on Linux and Android use CLOCK_MONOTONIC explicitly.
	timespec ts;
	clock_gettime(CLOCK_MONOTONIC, &ts);
	return ts.tv_sec * 1000000000ull + ts.tv_nsec;
#else
	return 0;
#endif
}

void WSITiming::update_frame_pacing(uint32_t serial, uint64_t present_time, bool wall_time)
{
	if (wall_time && !pacing.have_real_estimate)
	{
		// We don't have a refresh interval yet, just update the estimate from CPU.
		pacing.base_serial = serial;
		pacing.base_present = present_time;
		pacing.have_estimate = true;
		return;
	}
	else if (!wall_time && !pacing.have_real_estimate)
	{
		pacing.base_serial = serial;
		pacing.base_present = present_time;
		pacing.have_real_estimate = true;
		return;
	}
	else if (wall_time)
	{
		// We already have a correct estimate, don't update.
		return;
	}

	if (!feedback.refresh_interval)
	{
		// If we don't have a refresh interval yet, we cannot estimate anything.
		// What we can do instead is just to blindly use the latest observed timestamp as our guiding hand.
		if (present_time > pacing.base_present)
		{
			pacing.base_serial = serial;
			pacing.base_present = present_time;
		}
	}
	else
	{
		int32_t frame_dist = int32_t(serial - pacing.base_serial);

		// Don't update with the past.
		if (frame_dist <= 0)
			return;

		// Extrapolate timing from current.
		uint64_t extrapolated_present_time =
				pacing.base_present + feedback.refresh_interval * options.swap_interval * (serial - pacing.base_serial);
		int64_t error = std::abs(int64_t(extrapolated_present_time - present_time));

		// If the delta is close enough (expected frame pace),
		// update the base ID, so we can make more accurate future estimates.
		// This is relevant if we want to dynamically change swap interval.
		// If present time is significantly larger than extrapolated time,
		// we can assume we had a dropped frame, so we also need to update our base estimate.
		if ((present_time > extrapolated_present_time) || (error < int64_t(feedback.refresh_interval / 2)))
		{
			// We must have dropped frames, or similar.
			// Update our base estimate.
			pacing.base_serial = serial;
			pacing.base_present = present_time;
			if (options.debug)
			{
				LOGI("Updating frame pacing base to serial: %u (delta: %.3f ms)\n", pacing.base_serial,
				     1e-6 * int64_t(present_time - extrapolated_present_time));
			}
		}
	}
}

void WSITiming::update_frame_time_smoothing(double &frame_time, double &elapsed_time)
{
	double target_frame_time = frame_time;
	if (feedback.refresh_interval)
		target_frame_time = double(options.swap_interval * feedback.refresh_interval) * 1e-9;

	double actual_elapsed = elapsed_time - smoothing.offset;
	smoothing.elapsed += target_frame_time;

	double delta = actual_elapsed - smoothing.elapsed;
	if (delta > std::fabs(target_frame_time * 4.0))
	{
		// We're way off, something must have happened, reset the smoothing.
		// Don't jump skip the frame time, other than keeping the frame_time as-is.
		// We might have had a natural pause, and it doesn't make sense to report absurd frame times.
		// Apps needing to sync to wall time over time could use elapsed_time as a guiding hand.
		if (options.debug)
			LOGI("Detected discontinuity in smoothing algorithm!\n");
		smoothing.offset = elapsed_time;
		smoothing.elapsed = 0.0;
		return;
	}

	double jitter_offset = 0.0;

	// Accept up to 0.5% jitter to catch up or slow down smoothly to our target elapsed time.
	if (delta > 0.1 * target_frame_time)
		jitter_offset = 0.005 * target_frame_time;
	else if (delta < -0.1 * target_frame_time)
		jitter_offset = -0.005 * target_frame_time;

	target_frame_time += jitter_offset;
	smoothing.elapsed += jitter_offset;

	elapsed_time = smoothing.elapsed + smoothing.offset;
	frame_time = target_frame_time;
}

uint64_t WSITiming::get_refresh_interval() const
{
	return feedback.refresh_interval;
}

double WSITiming::get_current_latency() const
{
	return feedback.latency;
}

void WSITiming::limit_latency(Timing &new_timing)
{
	if (options.latency_limiter != LatencyLimiter::None)
	{
		// Try to squeeze timings by sleeping, quite shaky, but very fun :)
		if (feedback.refresh_interval)
		{
			int64_t target = int64_t(compute_target_present_time_for_serial(serial_info.serial));

			if (options.latency_limiter == LatencyLimiter::AdaptiveLowLatency)
			{
				int64_t latency = 0;
				if (get_conservative_latency(latency))
				{
					// Keep quarter frame as buffer in case this frame is heavier than normal.
					latency += feedback.refresh_interval >> 2;
					wait_until(target - latency);

					uint64_t old_time = new_timing.wall_frame_begin;
					new_timing.wall_frame_begin = get_wall_time();
					if (options.debug)
					{
						LOGI("Slept for %.3f ms for latency tuning.\n",
						     1e-6 * (new_timing.wall_frame_begin - old_time));
					}
				}
			}
			else if (options.latency_limiter == LatencyLimiter::IdealPipeline)
			{
				// In the ideal pipeline we have one frame for CPU to work,
				// then one frame for GPU to work in parallel, so we should strive for a little under 2 frames of latency here.
				// The assumption is that we can kick some work to GPU at least mid-way through our frame,
				// which will become our slack factor.
				int64_t latency = feedback.refresh_interval * 2;
				wait_until(target - latency);

				uint64_t old_time = new_timing.wall_frame_begin;
				new_timing.wall_frame_begin = get_wall_time();
				if (options.debug)
				{
					LOGI("Slept for %.3f ms for latency tuning.\n",
					     1e-6 * (new_timing.wall_frame_begin - old_time));
				}
			}
		}
	}
}

void WSITiming::begin_frame(double &frame_time, double &elapsed_time)
{
	promote_or_demote_frame_rate();

	// Update initial frame elapsed estimate,
	// from here, we'll try to lock the frame time to refresh_rate +/- epsilon.
	if (serial_info.serial == 0)
	{
		smoothing.offset = elapsed_time;
		smoothing.elapsed = 0.0;
	}
	serial_info.serial++;

	if (options.debug)
		LOGI("Starting WSITiming frame serial: %u\n", serial_info.serial);

	// On X11, this is found over time by observation, so we need to adapt it.
	// Only after we have observed the refresh cycle duration, we can start syncing against it.
	if ((serial_info.serial & 7) == 0)
		update_refresh_interval();

	auto &new_timing = feedback.past_timings[serial_info.serial & NUM_TIMING_MASK];
	new_timing.wall_serial = serial_info.serial;
	new_timing.wall_frame_begin = get_wall_time();
	new_timing.swap_interval_target = options.swap_interval;
	new_timing.result = TimingResult::Unknown;
	new_timing.timing = {};

	update_past_presentation_timing();
	// Absolute minimum case, just get some initial data before we have some real estimates.
	update_frame_pacing(serial_info.serial, new_timing.wall_frame_begin, true);
	update_frame_time_smoothing(frame_time, elapsed_time);
	limit_latency(new_timing);

	new_timing.wall_frame_target = compute_target_present_time_for_serial(serial_info.serial);
}

bool WSITiming::get_conservative_latency(int64_t &latency) const
{
	latency = 0;
	unsigned valid_latencies = 0;
	for (auto &timing : feedback.past_timings)
	{
		if (timing.timing.actualPresentTime >= timing.wall_frame_begin)
		{
			latency = std::max(latency, timing.pipeline_latency);
			valid_latencies++;
		}
	}

	return valid_latencies > (NUM_TIMINGS / 2);
}

uint64_t WSITiming::compute_target_present_time_for_serial(uint32_t serial)
{
	if (!pacing.have_estimate)
		return 0;

	uint64_t frame_delta = serial - pacing.base_serial;
	frame_delta *= options.swap_interval;
	uint64_t target = pacing.base_present + feedback.refresh_interval * frame_delta;
	return target;
}

void WSITiming::promote_or_demote_frame_rate()
{
	if (!options.adaptive_swap_interval)
		return;

	if (feedback.refresh_interval == 0)
		return;

	// Analyze if we should do something with frame rate.
	// The heuristic is something like:
	// - If we observe at least 3 hitches the last window of timing events, demote frame rate.
	// - If we observe consistent earliestPresent < actualPresent and presentMargin is at least a quarter frame,
	//   promote frame rate.

	// We can only make an analysis if all timings come from same swap interval.
	// This also limits how often we can automatically change frame rate.

	unsigned frame_drops = 0;
	unsigned early_frames = 0;
	unsigned total = 0;
	for (auto &timing : feedback.past_timings)
	{
		if (timing.result == TimingResult::Unknown)
			continue;
		if (options.swap_interval != timing.swap_interval_target)
			return;

		total++;
		if (timing.result == TimingResult::VeryEarly)
			early_frames++;
		else if (timing.result == TimingResult::TooLate)
			frame_drops++;
	}

	// Don't have enough data.
	if (total < NUM_TIMINGS / 2)
		return;

	if (early_frames == total && options.swap_interval > 1)
	{
		// We can go down in swap interval, great!
		set_swap_interval(options.swap_interval - 1);
		LOGI("Adjusted swap interval down to %u!\n", options.swap_interval);
	}
	else if (frame_drops >= 3)
	{
		if (options.swap_interval < 8) // swap interval of 8, lol
		{
			set_swap_interval(options.swap_interval + 1);
			LOGI("Too much hitching detected, increasing swap interval to %u!\n", options.swap_interval);
		}
		else
			LOGI("Still detecting hitching, but reached swap interval limit.\n");
	}
}

bool WSITiming::fill_present_info_timing(VkPresentTimeGOOGLE &time)
{
	time.presentID = serial_info.serial;

	time.desiredPresentTime = compute_target_present_time_for_serial(serial_info.serial);

	// Want to set the desired target close enough,
	// but not exactly at estimated target, since we have a rounding error cliff.
	// Set the target a quarter frame away from real target.
	if (time.desiredPresentTime != 0 && feedback.refresh_interval != 0)
		time.desiredPresentTime -= feedback.refresh_interval >> 4;

	return true;
}

}
