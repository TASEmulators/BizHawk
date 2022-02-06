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

namespace Vulkan
{
struct ImplementationQuirks
{
	bool instance_deferred_lights = true;
	bool merge_subpasses = true;
	bool use_transient_color = true;
	bool use_transient_depth_stencil = true;
	bool clustering_list_iteration = false;
	bool clustering_force_cpu = false;
	bool queue_wait_on_submission = false;
	bool staging_need_device_local = false;
	bool use_async_compute_post = true;
	bool render_graph_force_single_queue = false;
	bool force_no_subgroups = false;
	bool force_no_subgroup_shuffle = false;
	bool force_no_subgroup_size_control = false;

	static ImplementationQuirks &get();
};

struct ImplementationWorkarounds
{
	bool emulate_event_as_pipeline_barrier = false;
	bool optimize_all_graphics_barrier = false;
	bool force_store_in_render_pass = false;
	bool broken_color_write_mask = false;
	bool split_binary_timeline_semaphores = false;
};
}
