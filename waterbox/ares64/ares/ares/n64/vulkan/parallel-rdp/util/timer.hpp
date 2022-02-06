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

#include <stdint.h>
#include <memory>

namespace Util
{
class FrameTimer
{
public:
	FrameTimer();

	void reset();
	double frame();
	double frame(double frame_time);
	double get_elapsed() const;
	double get_frame_time() const;

	void enter_idle();
	void leave_idle();

private:
	int64_t start;
	int64_t last;
	int64_t last_period;
	int64_t idle_start;
	int64_t idle_time = 0;
	int64_t get_time();
};

class FrameLimiter
{
public:
	FrameLimiter();
	~FrameLimiter();
	bool begin_interval_ns(uint64_t ns);
	bool wait_interval();
	bool is_active() const;

private:
	struct Impl;
	std::unique_ptr<Impl> impl;
};

class Timer
{
public:
	void start();
	double end();

private:
	int64_t t = 0;
};

int64_t get_current_time_nsecs();
}