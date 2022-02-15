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

#include <queue>
#include <mutex>
#include <thread>
#include <condition_variable>
#include <utility>

#ifdef PARALLEL_RDP_SHADER_DIR
#include "global_managers.hpp"
#endif

namespace RDP
{
template <typename T, typename Executor>
class WorkerThread
{
public:
	explicit WorkerThread(
#ifdef PARALLEL_RDP_SHADER_DIR
			Granite::Global::GlobalManagersHandle globals,
#endif
			Executor exec)
		: executor(std::move(exec))
#ifdef PARALLEL_RDP_SHADER_DIR
		, handles(std::move(globals))
#endif
	{
		thr = std::thread(&WorkerThread::main_loop, this);
	}

	~WorkerThread()
	{
		if (thr.joinable())
		{
			{
				std::lock_guard<std::mutex> holder{to_thread_mutex};
				work_queue.push({});
				to_thread_cond.notify_one();
			}
			thr.join();
		}
	}

	template <typename Cond>
	void wait(Cond &&cond)
	{
		std::unique_lock<std::mutex> holder{to_main_mutex};
		to_main_cond.wait(holder, std::forward<Cond>(cond));
	}

	void push(T &&t)
	{
		std::lock_guard<std::mutex> holder{to_thread_mutex};
		work_queue.push(std::move(t));
		to_thread_cond.notify_one();
	}

private:
	std::thread thr;
	std::mutex to_thread_mutex;
	std::condition_variable to_thread_cond;
	std::mutex to_main_mutex;
	std::condition_variable to_main_cond;
	std::queue<T> work_queue;
	Executor executor;

#ifdef PARALLEL_RDP_SHADER_DIR
	Granite::Global::GlobalManagersHandle handles;
#endif

	void main_loop()
	{
#ifdef PARALLEL_RDP_SHADER_DIR
		Granite::Global::set_thread_context(*handles);
		handles.reset();
#endif

		for (;;)
		{
			T value;

			{
				std::unique_lock<std::mutex> holder{to_thread_mutex};
				to_thread_cond.wait(holder, [this]() { return !work_queue.empty(); });
				value = std::move(work_queue.front());
				work_queue.pop();
			}

			if (executor.is_sentinel(value))
				break;

			executor.perform_work(value);
			std::lock_guard<std::mutex> holder{to_main_mutex};
			executor.notify_work_locked(value);
			to_main_cond.notify_one();
		}
	}
};
}