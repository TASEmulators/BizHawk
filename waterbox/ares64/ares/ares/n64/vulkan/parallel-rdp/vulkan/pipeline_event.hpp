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
#include "vulkan_common.hpp"
#include "cookie.hpp"
#include "object_pool.hpp"

namespace Vulkan
{
class Device;
class EventHolder;

struct EventHolderDeleter
{
	void operator()(EventHolder *event);
};

class EventHolder : public Util::IntrusivePtrEnabled<EventHolder, EventHolderDeleter, HandleCounter>,
                    public InternalSyncEnabled
{
public:
	friend struct EventHolderDeleter;

	~EventHolder();

	const VkEvent &get_event() const
	{
		return event;
	}

	VkPipelineStageFlags get_stages() const
	{
		return stages;
	}

	void set_stages(VkPipelineStageFlags stages_)
	{
		stages = stages_;
	}

private:
	friend class Util::ObjectPool<EventHolder>;
	EventHolder(Device *device_, VkEvent event_)
		: device(device_)
		, event(event_)
	{
	}

	Device *device;
	VkEvent event;
	VkPipelineStageFlags stages = 0;
};

using PipelineEvent = Util::IntrusivePtr<EventHolder>;

}
