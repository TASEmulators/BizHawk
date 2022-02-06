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

#include "logging.hpp"
#include "timeline_trace_file.hpp"
#include "thread_name.hpp"
#include "timer.hpp"
#include <string.h>
#include <stdio.h>

namespace Util
{
static thread_local char trace_tid[32];
static thread_local TimelineTraceFile *trace_file;

void TimelineTraceFile::set_tid(const char *tid)
{
	snprintf(trace_tid, sizeof(trace_tid), "%s", tid);
}

void TimelineTraceFile::set_per_thread(TimelineTraceFile *file)
{
	trace_file = file;
}

TimelineTraceFile *TimelineTraceFile::get_per_thread()
{
	return trace_file;
}

void TimelineTraceFile::Event::set_desc(const char *desc_)
{
	snprintf(desc, sizeof(desc), "%s", desc_);
}

void TimelineTraceFile::Event::set_tid(const char *tid_)
{
	snprintf(tid, sizeof(tid), "%s", tid_);
}

TimelineTraceFile::Event *TimelineTraceFile::begin_event(const char *desc, uint32_t pid)
{
	auto *e = event_pool.allocate();
	e->pid = pid;
	e->set_tid(trace_tid);
	e->set_desc(desc);
	e->start_ns = get_current_time_nsecs();
	return e;
}

TimelineTraceFile::Event *TimelineTraceFile::allocate_event()
{
	auto *e = event_pool.allocate();
	e->desc[0] = '\0';
	e->tid[0] = '\0';
	e->pid = 0;
	e->start_ns = 0;
	e->end_ns = 0;
	return e;
}

void TimelineTraceFile::submit_event(Event *e)
{
	std::lock_guard<std::mutex> holder{lock};
	queued_events.push(e);
	cond.notify_one();
}

void TimelineTraceFile::end_event(Event *e)
{
	e->end_ns = get_current_time_nsecs();
	submit_event(e);
}

TimelineTraceFile::TimelineTraceFile(const std::string &path)
{
	thr = std::thread(&TimelineTraceFile::looper, this, path);
}

void TimelineTraceFile::looper(std::string path)
{
	set_current_thread_name("json-trace-io");

	FILE *file = fopen(path.c_str(), "w");
	if (!file)
		LOGE("Failed to open file: %s.\n", path.c_str());

	if (file)
		fputs("[\n", file);

	uint64_t base_ts = get_current_time_nsecs();

	for (;;)
	{
		Event *e;
		{
			std::unique_lock<std::mutex> holder{lock};
			cond.wait(holder, [this]() {
				return !queued_events.empty();
			});
			e = queued_events.front();
			queued_events.pop();
		}

		if (!e)
			break;

		auto start_us = int64_t(e->start_ns - base_ts) * 1e-3;
		auto end_us = int64_t(e->end_ns - base_ts) * 1e-3;

		if (file && start_us <= end_us)
		{
			fprintf(file, "{ \"name\": \"%s\", \"ph\": \"B\", \"tid\": \"%s\", \"pid\": \"%u\", \"ts\": %f },\n",
			        e->desc, e->tid, e->pid, start_us);
			fprintf(file, "{ \"name\": \"%s\", \"ph\": \"E\", \"tid\": \"%s\", \"pid\": \"%u\", \"ts\": %f },\n",
			        e->desc, e->tid, e->pid, end_us);
		}

		event_pool.free(e);
	}

	// Intentionally truncate the JSON so that we can emit "," after the last element.
	if (file)
		fclose(file);
}

TimelineTraceFile::~TimelineTraceFile()
{
	submit_event(nullptr);
	if (thr.joinable())
		thr.join();
}
}
