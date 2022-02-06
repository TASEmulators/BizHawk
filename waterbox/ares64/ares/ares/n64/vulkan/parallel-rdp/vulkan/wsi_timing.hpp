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

#pragma once

#include "vulkan_headers.hpp"
#include <vector>

namespace Vulkan
{
enum class LatencyLimiter
{
	None,
	AdaptiveLowLatency,
	IdealPipeline
};

struct WSITimingOptions
{
	uint32_t swap_interval = 1;
	LatencyLimiter latency_limiter = LatencyLimiter::None;
	bool adaptive_swap_interval = false;
	bool debug = false;
};

class WSIPlatform;
class Device;

class WSITiming
{
public:
	void init(WSIPlatform *platform, Device *device, VkSwapchainKHR swapchain, const WSITimingOptions &options = {});
	void begin_frame(double &frame_time, double &elapsed_time);

	bool fill_present_info_timing(VkPresentTimeGOOGLE &time);
	double get_current_latency() const;

	void set_swap_interval(unsigned interval);
	void set_debug_enable(bool enable);
	void set_latency_limiter(LatencyLimiter limiter);

	// Can return 0 if we don't know the refresh interval yet.
	uint64_t get_refresh_interval() const;

	const WSITimingOptions &get_options() const;

private:
	WSIPlatform *platform = nullptr;
	VkDevice device = VK_NULL_HANDLE;
	const VolkDeviceTable *table = nullptr;
	VkSwapchainKHR swapchain = VK_NULL_HANDLE;
	WSITimingOptions options;

	enum { NUM_TIMINGS = 32, NUM_TIMING_MASK = NUM_TIMINGS - 1 };

	struct Serial
	{
		uint32_t serial = 0;
	} serial_info;

	enum class TimingResult
	{
		Unknown,
		VeryEarly,
		TooLate,
		Expected
	};

	struct Timing
	{
		uint32_t wall_serial = 0;
		uint64_t wall_frame_begin = 0;
		uint64_t wall_frame_target = 0;
		uint32_t swap_interval_target = 0;
		TimingResult result = TimingResult::Unknown;
		int64_t slack = 0;
		int64_t pipeline_latency = 0;
		VkPastPresentationTimingGOOGLE timing = {};
	};

	struct Feedback
	{
		uint64_t refresh_interval = 0;
		Timing past_timings[NUM_TIMINGS];
		std::vector<VkPastPresentationTimingGOOGLE> timing_buffer;
		double latency = 0.0;
	} feedback;

	struct Pacing
	{
		uint32_t base_serial = 0;
		uint64_t base_present = 0;
		bool have_estimate = false;
		bool have_real_estimate = false;
	} pacing;

	struct FrameTimer
	{
		uint64_t present_time = 0;
		uint64_t serial = 0;
	} last_frame;

	struct SmoothTimer
	{
		double elapsed = 0.0;
		double offset = 0.0;
	} smoothing;

	uint64_t compute_target_present_time_for_serial(uint32_t serial);
	uint64_t get_wall_time();
	void update_past_presentation_timing();
	Timing *find_latest_timestamp(uint32_t start_serial);
	void update_frame_pacing(uint32_t id, uint64_t present_time, bool wall_time);
	void update_refresh_interval();
	void update_frame_time_smoothing(double &frame_time, double &elapsed_time);
	bool get_conservative_latency(int64_t &latency) const;
	void wait_until(int64_t nsecs);
	void limit_latency(Timing &new_timing);
	void promote_or_demote_frame_rate();
};
}
