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

#include "buffer.hpp"
#include "device.hpp"

namespace Vulkan
{
Buffer::Buffer(Device *device_, VkBuffer buffer_, const DeviceAllocation &alloc_, const BufferCreateInfo &info_)
    : Cookie(device_)
    , device(device_)
    , buffer(buffer_)
    , alloc(alloc_)
    , info(info_)
{
}

Buffer::~Buffer()
{
	if (internal_sync)
	{
		device->destroy_buffer_nolock(buffer);
		device->free_memory_nolock(alloc);
	}
	else
	{
		device->destroy_buffer(buffer);
		device->free_memory(alloc);
	}
}

void BufferDeleter::operator()(Buffer *buffer)
{
	buffer->device->handle_pool.buffers.free(buffer);
}

BufferView::BufferView(Device *device_, VkBufferView view_, const BufferViewCreateInfo &create_info_)
    : Cookie(device_)
    , device(device_)
    , view(view_)
    , info(create_info_)
{
}

BufferView::~BufferView()
{
	if (view != VK_NULL_HANDLE)
	{
		if (internal_sync)
			device->destroy_buffer_view_nolock(view);
		else
			device->destroy_buffer_view(view);
	}
}

void BufferViewDeleter::operator()(BufferView *view)
{
	view->device->handle_pool.buffer_views.free(view);
}

}
