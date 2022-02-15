/* Copyright (c) 2020 Themaister
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

#include <thread>
#include <mutex>
#include <condition_variable>
#include <vector>

#ifdef PARALLEL_RDP_SHADER_DIR
#include "global_managers.hpp"
#endif

namespace RDP
{
class CommandProcessor;
class CommandRing
{
public:
	void init(
#ifdef PARALLEL_RDP_SHADER_DIR
			Granite::Global::GlobalManagersHandle global_handles,
#endif
			CommandProcessor *processor, unsigned count);
	~CommandRing();
	void drain();

	void enqueue_command(unsigned num_words, const uint32_t *words);

private:
	CommandProcessor *processor = nullptr;
	std::thread thr;
	std::mutex lock;
	std::condition_variable cond;

	std::vector<uint32_t> ring;
	uint64_t write_count = 0;
	uint64_t read_count = 0;
	uint64_t completed_count = 0;

	void thread_loop();
	void teardown_thread();
#ifdef PARALLEL_RDP_SHADER_DIR
	Granite::Global::GlobalManagersHandle global_handles;
#endif
};
}
