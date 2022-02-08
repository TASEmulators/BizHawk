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

#include "render_pass.hpp"
#include "stack_allocator.hpp"
#include "device.hpp"
#include "quirks.hpp"
#include <utility>
#include <cstring>

using namespace std;
using namespace Util;

#ifdef GRANITE_VULKAN_MT
#define LOCK() std::lock_guard<std::mutex> holder__{lock}
#else
#define LOCK() ((void)0)
#endif

namespace Vulkan
{
void RenderPass::setup_subpasses(const VkRenderPassCreateInfo &create_info)
{
	auto *attachments = create_info.pAttachments;

	// Store the important subpass information for later.
	for (uint32_t i = 0; i < create_info.subpassCount; i++)
	{
		auto &subpass = create_info.pSubpasses[i];

		SubpassInfo subpass_info = {};
		subpass_info.num_color_attachments = subpass.colorAttachmentCount;
		subpass_info.num_input_attachments = subpass.inputAttachmentCount;
		subpass_info.depth_stencil_attachment = *subpass.pDepthStencilAttachment;
		memcpy(subpass_info.color_attachments, subpass.pColorAttachments,
		       subpass.colorAttachmentCount * sizeof(*subpass.pColorAttachments));
		memcpy(subpass_info.input_attachments, subpass.pInputAttachments,
		       subpass.inputAttachmentCount * sizeof(*subpass.pInputAttachments));

		unsigned samples = 0;
		for (unsigned att = 0; att < subpass_info.num_color_attachments; att++)
		{
			if (subpass_info.color_attachments[att].attachment == VK_ATTACHMENT_UNUSED)
				continue;

			unsigned samp = attachments[subpass_info.color_attachments[att].attachment].samples;
			if (samples && (samp != samples))
				VK_ASSERT(samp == samples);
			samples = samp;
		}

		if (subpass_info.depth_stencil_attachment.attachment != VK_ATTACHMENT_UNUSED)
		{
			unsigned samp = attachments[subpass_info.depth_stencil_attachment.attachment].samples;
			if (samples && (samp != samples))
				VK_ASSERT(samp == samples);
			samples = samp;
		}

		VK_ASSERT(samples > 0);
		subpass_info.samples = samples;
		subpasses_info.push_back(subpass_info);
	}
}

RenderPass::RenderPass(Hash hash, Device *device_, const VkRenderPassCreateInfo &create_info)
	: IntrusiveHashMapEnabled<RenderPass>(hash)
	, device(device_)
{
	auto &table = device->get_device_table();
	unsigned num_color_attachments = 0;
	if (create_info.attachmentCount > 0)
	{
		auto &att = create_info.pAttachments[create_info.attachmentCount - 1];
		if (format_has_depth_or_stencil_aspect(att.format))
		{
			depth_stencil = att.format;
			num_color_attachments = create_info.attachmentCount - 1;
		}
		else
			num_color_attachments = create_info.attachmentCount;
	}

	for (unsigned i = 0; i < num_color_attachments; i++)
		color_attachments[i] = create_info.pAttachments[i].format;

	// Store the important subpass information for later.
	setup_subpasses(create_info);

	// Fixup after, we want the Fossilize render pass to be generic.
	auto info = create_info;
	VkAttachmentDescription fixup_attachments[VULKAN_NUM_ATTACHMENTS + 1];
	fixup_render_pass_workaround(info, fixup_attachments);

#ifdef VULKAN_DEBUG
	LOGI("Creating render pass.\n");
#endif
	if (table.vkCreateRenderPass(device->get_device(), &info, nullptr, &render_pass) != VK_SUCCESS)
		LOGE("Failed to create render pass.");

#ifdef GRANITE_VULKAN_FOSSILIZE
	device->register_render_pass(render_pass, get_hash(), create_info);
#endif
}

RenderPass::RenderPass(Hash hash, Device *device_, const RenderPassInfo &info)
	: IntrusiveHashMapEnabled<RenderPass>(hash)
	, device(device_)
{
	fill(begin(color_attachments), end(color_attachments), VK_FORMAT_UNDEFINED);

	VK_ASSERT(info.num_color_attachments || info.depth_stencil);

	// Want to make load/store to transient a very explicit thing to do, since it will kill performance.
	bool enable_transient_store = (info.op_flags & RENDER_PASS_OP_ENABLE_TRANSIENT_STORE_BIT) != 0;
	bool enable_transient_load = (info.op_flags & RENDER_PASS_OP_ENABLE_TRANSIENT_LOAD_BIT) != 0;
	bool multiview = info.num_layers > 1;

	// Set up default subpass info structure if we don't have it.
	auto *subpass_infos = info.subpasses;
	unsigned num_subpasses = info.num_subpasses;
	RenderPassInfo::Subpass default_subpass_info;
	if (!info.subpasses)
	{
		default_subpass_info.num_color_attachments = info.num_color_attachments;
		if (info.op_flags & RENDER_PASS_OP_DEPTH_STENCIL_READ_ONLY_BIT)
			default_subpass_info.depth_stencil_mode = RenderPassInfo::DepthStencil::ReadOnly;
		else
			default_subpass_info.depth_stencil_mode = RenderPassInfo::DepthStencil::ReadWrite;

		for (unsigned i = 0; i < info.num_color_attachments; i++)
			default_subpass_info.color_attachments[i] = i;
		num_subpasses = 1;
		subpass_infos = &default_subpass_info;
	}

	// First, set up attachment descriptions.
	const unsigned num_attachments = info.num_color_attachments + (info.depth_stencil ? 1 : 0);
	VkAttachmentDescription attachments[VULKAN_NUM_ATTACHMENTS + 1];
	uint32_t implicit_transitions = 0;
	uint32_t implicit_bottom_of_pipe = 0;

	VkAttachmentLoadOp ds_load_op = VK_ATTACHMENT_LOAD_OP_DONT_CARE;
	VkAttachmentStoreOp ds_store_op = VK_ATTACHMENT_STORE_OP_DONT_CARE;

	VK_ASSERT(!(info.clear_attachments & info.load_attachments));

	const auto color_load_op = [&info](unsigned index) -> VkAttachmentLoadOp {
		if ((info.clear_attachments & (1u << index)) != 0)
			return VK_ATTACHMENT_LOAD_OP_CLEAR;
		else if ((info.load_attachments & (1u << index)) != 0)
			return VK_ATTACHMENT_LOAD_OP_LOAD;
		else
			return VK_ATTACHMENT_LOAD_OP_DONT_CARE;
	};

	const auto color_store_op = [&info](unsigned index) -> VkAttachmentStoreOp {
		if ((info.store_attachments & (1u << index)) != 0)
			return VK_ATTACHMENT_STORE_OP_STORE;
		else
			return VK_ATTACHMENT_STORE_OP_DONT_CARE;
	};

	if (info.op_flags & RENDER_PASS_OP_CLEAR_DEPTH_STENCIL_BIT)
		ds_load_op = VK_ATTACHMENT_LOAD_OP_CLEAR;
	else if (info.op_flags & RENDER_PASS_OP_LOAD_DEPTH_STENCIL_BIT)
		ds_load_op = VK_ATTACHMENT_LOAD_OP_LOAD;

	if (info.op_flags & RENDER_PASS_OP_STORE_DEPTH_STENCIL_BIT)
		ds_store_op = VK_ATTACHMENT_STORE_OP_STORE;

	bool ds_read_only = (info.op_flags & RENDER_PASS_OP_DEPTH_STENCIL_READ_ONLY_BIT) != 0;
	VkImageLayout depth_stencil_layout = VK_IMAGE_LAYOUT_UNDEFINED;
	if (info.depth_stencil)
	{
		depth_stencil_layout = info.depth_stencil->get_image().get_layout(
				ds_read_only ?
				VK_IMAGE_LAYOUT_DEPTH_STENCIL_READ_ONLY_OPTIMAL :
				VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL);
	}

	for (unsigned i = 0; i < info.num_color_attachments; i++)
	{
		VK_ASSERT(info.color_attachments[i]);
		color_attachments[i] = info.color_attachments[i]->get_format();
		auto &image = info.color_attachments[i]->get_image();
		auto &att = attachments[i];
		att.flags = 0;
		att.format = color_attachments[i];
		att.samples = image.get_create_info().samples;
		att.loadOp = color_load_op(i);
		att.storeOp = color_store_op(i);
		att.stencilLoadOp = VK_ATTACHMENT_LOAD_OP_DONT_CARE;
		att.stencilStoreOp = VK_ATTACHMENT_STORE_OP_DONT_CARE;
		// Undefined final layout here for now means that we will just use the layout of the last
		// subpass which uses this attachment to avoid any dummy transition at the end.
		att.finalLayout = VK_IMAGE_LAYOUT_UNDEFINED;

		if (image.get_create_info().domain == ImageDomain::Transient)
		{
			if (enable_transient_load)
			{
				// The transient will behave like a normal image.
				att.initialLayout = info.color_attachments[i]->get_image().get_layout(VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL);
			}
			else
			{
				// Force a clean discard.
				VK_ASSERT(att.loadOp != VK_ATTACHMENT_LOAD_OP_LOAD);
				att.initialLayout = VK_IMAGE_LAYOUT_UNDEFINED;
			}

			if (!enable_transient_store)
				att.storeOp = VK_ATTACHMENT_STORE_OP_DONT_CARE;

			implicit_transitions |= 1u << i;
		}
		else if (image.is_swapchain_image())
		{
			if (att.loadOp == VK_ATTACHMENT_LOAD_OP_LOAD)
				att.initialLayout = image.get_swapchain_layout();
			else
				att.initialLayout = VK_IMAGE_LAYOUT_UNDEFINED;

			att.finalLayout = image.get_swapchain_layout();

			// If we transition from PRESENT_SRC_KHR, this came from an implicit external subpass dependency
			// which happens in BOTTOM_OF_PIPE. To properly transition away from it, we must wait for BOTTOM_OF_PIPE,
			// without any memory barriers, since memory has been made available in the implicit barrier.
			if (att.loadOp == VK_ATTACHMENT_LOAD_OP_LOAD)
				implicit_bottom_of_pipe |= 1u << i;
			implicit_transitions |= 1u << i;
		}
		else
			att.initialLayout = info.color_attachments[i]->get_image().get_layout(VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL);
	}

	depth_stencil = info.depth_stencil ? info.depth_stencil->get_format() : VK_FORMAT_UNDEFINED;
	if (info.depth_stencil)
	{
		auto &image = info.depth_stencil->get_image();
		auto &att = attachments[info.num_color_attachments];
		att.flags = 0;
		att.format = depth_stencil;
		att.samples = image.get_create_info().samples;
		att.loadOp = ds_load_op;
		att.storeOp = ds_store_op;
		// Undefined final layout here for now means that we will just use the layout of the last
		// subpass which uses this attachment to avoid any dummy transition at the end.
		att.finalLayout = VK_IMAGE_LAYOUT_UNDEFINED;

		if (format_to_aspect_mask(depth_stencil) & VK_IMAGE_ASPECT_STENCIL_BIT)
		{
			att.stencilLoadOp = ds_load_op;
			att.stencilStoreOp = ds_store_op;
		}
		else
		{
			att.stencilLoadOp = VK_ATTACHMENT_LOAD_OP_DONT_CARE;
			att.stencilStoreOp = VK_ATTACHMENT_STORE_OP_DONT_CARE;
		}

		if (image.get_create_info().domain == ImageDomain::Transient)
		{
			if (enable_transient_load)
			{
				// The transient will behave like a normal image.
				att.initialLayout = depth_stencil_layout;
			}
			else
			{
				if (att.loadOp == VK_ATTACHMENT_LOAD_OP_LOAD)
					att.loadOp = VK_ATTACHMENT_LOAD_OP_DONT_CARE;
				if (att.stencilLoadOp == VK_ATTACHMENT_LOAD_OP_LOAD)
					att.stencilLoadOp = VK_ATTACHMENT_LOAD_OP_DONT_CARE;

				// For transient attachments we force the layouts.
				att.initialLayout = VK_IMAGE_LAYOUT_UNDEFINED;
			}

			if (!enable_transient_store)
			{
				att.storeOp = VK_ATTACHMENT_STORE_OP_DONT_CARE;
				att.stencilStoreOp = VK_ATTACHMENT_STORE_OP_DONT_CARE;
			}

			implicit_transitions |= 1u << info.num_color_attachments;
		}
		else
			att.initialLayout = depth_stencil_layout;
	}

	Util::StackAllocator<VkAttachmentReference, 1024> reference_allocator;
	Util::StackAllocator<uint32_t, 1024> preserve_allocator;

	vector<VkSubpassDescription> subpasses(num_subpasses);
	vector<VkSubpassDependency> external_dependencies;
	for (unsigned i = 0; i < num_subpasses; i++)
	{
		auto *colors = reference_allocator.allocate_cleared(subpass_infos[i].num_color_attachments);
		auto *inputs = reference_allocator.allocate_cleared(subpass_infos[i].num_input_attachments);
		auto *resolves = reference_allocator.allocate_cleared(subpass_infos[i].num_color_attachments);
		auto *depth = reference_allocator.allocate_cleared(1);

		auto &subpass = subpasses[i];
		subpass.pipelineBindPoint = VK_PIPELINE_BIND_POINT_GRAPHICS;
		subpass.colorAttachmentCount = subpass_infos[i].num_color_attachments;
		subpass.pColorAttachments = colors;
		subpass.inputAttachmentCount = subpass_infos[i].num_input_attachments;
		subpass.pInputAttachments = inputs;
		subpass.pDepthStencilAttachment = depth;

		if (subpass_infos[i].num_resolve_attachments)
		{
			VK_ASSERT(subpass_infos[i].num_color_attachments == subpass_infos[i].num_resolve_attachments);
			subpass.pResolveAttachments = resolves;
		}

		for (unsigned j = 0; j < subpass.colorAttachmentCount; j++)
		{
			auto att = subpass_infos[i].color_attachments[j];
			VK_ASSERT(att == VK_ATTACHMENT_UNUSED || (att < num_attachments));
			colors[j].attachment = att;
			// Fill in later.
			colors[j].layout = VK_IMAGE_LAYOUT_UNDEFINED;
		}

		for (unsigned j = 0; j < subpass.inputAttachmentCount; j++)
		{
			auto att = subpass_infos[i].input_attachments[j];
			VK_ASSERT(att == VK_ATTACHMENT_UNUSED || (att < num_attachments));
			inputs[j].attachment = att;
			// Fill in later.
			inputs[j].layout = VK_IMAGE_LAYOUT_UNDEFINED;
		}

		if (subpass.pResolveAttachments)
		{
			for (unsigned j = 0; j < subpass.colorAttachmentCount; j++)
			{
				auto att = subpass_infos[i].resolve_attachments[j];
				VK_ASSERT(att == VK_ATTACHMENT_UNUSED || (att < num_attachments));
				resolves[j].attachment = att;
				// Fill in later.
				resolves[j].layout = VK_IMAGE_LAYOUT_UNDEFINED;
			}
		}

		if (info.depth_stencil && subpass_infos[i].depth_stencil_mode != RenderPassInfo::DepthStencil::None)
		{
			depth->attachment = info.num_color_attachments;
			// Fill in later.
			depth->layout = VK_IMAGE_LAYOUT_UNDEFINED;
		}
		else
		{
			depth->attachment = VK_ATTACHMENT_UNUSED;
			depth->layout = VK_IMAGE_LAYOUT_UNDEFINED;
		}
	}

	const auto find_color = [&](unsigned subpass, unsigned attachment) -> VkAttachmentReference * {
		auto *colors = subpasses[subpass].pColorAttachments;
		for (unsigned i = 0; i < subpasses[subpass].colorAttachmentCount; i++)
			if (colors[i].attachment == attachment)
				return const_cast<VkAttachmentReference *>(&colors[i]);
		return nullptr;
	};

	const auto find_resolve = [&](unsigned subpass, unsigned attachment) -> VkAttachmentReference * {
		if (!subpasses[subpass].pResolveAttachments)
			return nullptr;

		auto *resolves = subpasses[subpass].pResolveAttachments;
		for (unsigned i = 0; i < subpasses[subpass].colorAttachmentCount; i++)
			if (resolves[i].attachment == attachment)
				return const_cast<VkAttachmentReference *>(&resolves[i]);
		return nullptr;
	};

	const auto find_input = [&](unsigned subpass, unsigned attachment) -> VkAttachmentReference * {
		auto *inputs = subpasses[subpass].pInputAttachments;
		for (unsigned i = 0; i < subpasses[subpass].inputAttachmentCount; i++)
			if (inputs[i].attachment == attachment)
				return const_cast<VkAttachmentReference *>(&inputs[i]);
		return nullptr;
	};

	const auto find_depth_stencil = [&](unsigned subpass, unsigned attachment) -> VkAttachmentReference * {
		if (subpasses[subpass].pDepthStencilAttachment->attachment == attachment)
			return const_cast<VkAttachmentReference *>(subpasses[subpass].pDepthStencilAttachment);
		else
			return nullptr;
	};

	// Now, figure out how each attachment is used throughout the subpasses.
	// Either we don't care (inherit previous pass), or we need something specific.
	// Start with initial layouts.
	uint32_t preserve_masks[VULKAN_NUM_ATTACHMENTS + 1] = {};

	// Last subpass which makes use of an attachment.
	unsigned last_subpass_for_attachment[VULKAN_NUM_ATTACHMENTS + 1] = {};

	VK_ASSERT(num_subpasses <= 32);

	// 1 << subpass bit set if there are color attachment self-dependencies in the subpass.
	uint32_t color_self_dependencies = 0;
	// 1 << subpass bit set if there are depth-stencil attachment self-dependencies in the subpass.
	uint32_t depth_self_dependencies = 0;

	// 1 << subpass bit set if any input attachment is read in the subpass.
	uint32_t input_attachment_read = 0;
	uint32_t color_attachment_read_write = 0;
	uint32_t depth_stencil_attachment_write = 0;
	uint32_t depth_stencil_attachment_read = 0;

	uint32_t external_color_dependencies = 0;
	uint32_t external_depth_dependencies = 0;
	uint32_t external_input_dependencies = 0;
	uint32_t external_bottom_of_pipe_dependencies = 0;

	for (unsigned attachment = 0; attachment < num_attachments; attachment++)
	{
		bool used = false;
		auto current_layout = attachments[attachment].initialLayout;
		for (unsigned subpass = 0; subpass < num_subpasses; subpass++)
		{
			auto *color = find_color(subpass, attachment);
			auto *resolve = find_resolve(subpass, attachment);
			auto *input = find_input(subpass, attachment);
			auto *depth = find_depth_stencil(subpass, attachment);

			// Sanity check.
			if (color || resolve)
				VK_ASSERT(!depth);
			if (depth)
				VK_ASSERT(!color && !resolve);
			if (resolve)
				VK_ASSERT(!color && !depth);

			if (!color && !input && !depth && !resolve)
			{
				if (used)
					preserve_masks[attachment] |= 1u << subpass;
				continue;
			}

			if (!used && (implicit_transitions & (1u << attachment)))
			{
				// This is the first subpass we need implicit transitions.
				if (color)
					external_color_dependencies |= 1u << subpass;
				if (depth)
					external_depth_dependencies |= 1u << subpass;
				if (input)
					external_input_dependencies |= 1u << subpass;
			}

			if (!used && (implicit_bottom_of_pipe & (1u << attachment)))
				external_bottom_of_pipe_dependencies |= 1u << subpass;

			if (resolve && input) // If used as both resolve attachment and input attachment in same subpass, need GENERAL.
			{
				current_layout = VK_IMAGE_LAYOUT_GENERAL;
				resolve->layout = current_layout;
				input->layout = current_layout;

				// If the attachment is first used as a feedback attachment, the initial layout should actually be GENERAL.
				if (!used && attachments[attachment].initialLayout != VK_IMAGE_LAYOUT_UNDEFINED)
					attachments[attachment].initialLayout = current_layout;

				// If first subpass changes the layout, we'll need to inject an external subpass dependency.
				if (!used && attachments[attachment].initialLayout != current_layout)
				{
					external_color_dependencies |= 1u << subpass;
					external_input_dependencies |= 1u << subpass;
				}

				used = true;
				last_subpass_for_attachment[attachment] = subpass;

				color_attachment_read_write |= 1u << subpass;
				input_attachment_read |= 1u << subpass;
			}
			else if (resolve)
			{
				if (current_layout != VK_IMAGE_LAYOUT_GENERAL)
					current_layout = VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL;

				// If first subpass changes the layout, we'll need to inject an external subpass dependency.
				if (!used && attachments[attachment].initialLayout != current_layout)
					external_color_dependencies |= 1u << subpass;

				resolve->layout = current_layout;
				used = true;
				last_subpass_for_attachment[attachment] = subpass;
				color_attachment_read_write |= 1u << subpass;
			}
			else if (color && input) // If used as both input attachment and color attachment in same subpass, need GENERAL.
			{
				current_layout = VK_IMAGE_LAYOUT_GENERAL;
				color->layout = current_layout;
				input->layout = current_layout;

				// If the attachment is first used as a feedback attachment, the initial layout should actually be GENERAL.
				if (!used && attachments[attachment].initialLayout != VK_IMAGE_LAYOUT_UNDEFINED)
					attachments[attachment].initialLayout = current_layout;

				// If first subpass changes the layout, we'll need to inject an external subpass dependency.
				if (!used && attachments[attachment].initialLayout != current_layout)
				{
					external_color_dependencies |= 1u << subpass;
					external_input_dependencies |= 1u << subpass;
				}

				used = true;
				last_subpass_for_attachment[attachment] = subpass;
				color_self_dependencies |= 1u << subpass;

				color_attachment_read_write |= 1u << subpass;
				input_attachment_read |= 1u << subpass;
			}
			else if (color) // No particular preference
			{
				if (current_layout != VK_IMAGE_LAYOUT_GENERAL)
					current_layout = VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL;
				color->layout = current_layout;

				// If first subpass changes the layout, we'll need to inject an external subpass dependency.
				if (!used && attachments[attachment].initialLayout != current_layout)
					external_color_dependencies |= 1u << subpass;

				used = true;
				last_subpass_for_attachment[attachment] = subpass;
				color_attachment_read_write |= 1u << subpass;
			}
			else if (depth && input) // Depends on the depth mode
			{
				VK_ASSERT(subpass_infos[subpass].depth_stencil_mode != RenderPassInfo::DepthStencil::None);
				if (subpass_infos[subpass].depth_stencil_mode == RenderPassInfo::DepthStencil::ReadWrite)
				{
					depth_self_dependencies |= 1u << subpass;
					current_layout = VK_IMAGE_LAYOUT_GENERAL;
					depth_stencil_attachment_write |= 1u << subpass;

					// If the attachment is first used as a feedback attachment, the initial layout should actually be GENERAL.
					if (!used && attachments[attachment].initialLayout != VK_IMAGE_LAYOUT_UNDEFINED)
						attachments[attachment].initialLayout = current_layout;
				}
				else
				{
					if (current_layout != VK_IMAGE_LAYOUT_GENERAL)
						current_layout = VK_IMAGE_LAYOUT_DEPTH_STENCIL_READ_ONLY_OPTIMAL;
				}

				// If first subpass changes the layout, we'll need to inject an external subpass dependency.
				if (!used && attachments[attachment].initialLayout != current_layout)
				{
					external_input_dependencies |= 1u << subpass;
					external_depth_dependencies |= 1u << subpass;
				}

				depth_stencil_attachment_read |= 1u << subpass;
				input_attachment_read |= 1u << subpass;
				depth->layout = current_layout;
				input->layout = current_layout;
				used = true;
				last_subpass_for_attachment[attachment] = subpass;
			}
			else if (depth)
			{
				if (subpass_infos[subpass].depth_stencil_mode == RenderPassInfo::DepthStencil::ReadWrite)
				{
					depth_stencil_attachment_write |= 1u << subpass;
					if (current_layout != VK_IMAGE_LAYOUT_GENERAL)
						current_layout = VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL;
				}
				else
				{
					if (current_layout != VK_IMAGE_LAYOUT_GENERAL)
						current_layout = VK_IMAGE_LAYOUT_DEPTH_STENCIL_READ_ONLY_OPTIMAL;
				}

				// If first subpass changes the layout, we'll need to inject an external subpass dependency.
				if (!used && attachments[attachment].initialLayout != current_layout)
					external_depth_dependencies |= 1u << subpass;

				depth_stencil_attachment_read |= 1u << subpass;
				depth->layout = current_layout;
				used = true;
				last_subpass_for_attachment[attachment] = subpass;
			}
			else if (input)
			{
				if (current_layout != VK_IMAGE_LAYOUT_GENERAL)
					current_layout = VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;

				// If the attachment is first used as an input attachment, the initial layout should actually be
				// SHADER_READ_ONLY_OPTIMAL.
				if (!used && attachments[attachment].initialLayout == VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL)
					attachments[attachment].initialLayout = current_layout;

				// If first subpass changes the layout, we'll need to inject an external subpass dependency.
				if (!used && attachments[attachment].initialLayout != current_layout)
					external_input_dependencies |= 1u << subpass;

				input->layout = current_layout;
				used = true;
				last_subpass_for_attachment[attachment] = subpass;
			}
			else
			{
				VK_ASSERT(0 && "Unhandled attachment usage.");
			}
		}

		// If we don't have a specific layout we need to end up in, just
		// use the last one.
		// Assert that we actually use all the attachments we have ...
		VK_ASSERT(used);
		if (attachments[attachment].finalLayout == VK_IMAGE_LAYOUT_UNDEFINED)
		{
			VK_ASSERT(current_layout != VK_IMAGE_LAYOUT_UNDEFINED);
			attachments[attachment].finalLayout = current_layout;
		}
	}

	// Only consider preserve masks before last subpass which uses an attachment.
	for (unsigned attachment = 0; attachment < num_attachments; attachment++)
		preserve_masks[attachment] &= (1u << last_subpass_for_attachment[attachment]) - 1;

	// Add preserve attachments as needed.
	for (unsigned subpass = 0; subpass < num_subpasses; subpass++)
	{
		auto &pass = subpasses[subpass];
		unsigned preserve_count = 0;
		for (unsigned attachment = 0; attachment < num_attachments; attachment++)
			if (preserve_masks[attachment] & (1u << subpass))
				preserve_count++;

		auto *preserve = preserve_allocator.allocate_cleared(preserve_count);
		pass.pPreserveAttachments = preserve;
		pass.preserveAttachmentCount = preserve_count;
		for (unsigned attachment = 0; attachment < num_attachments; attachment++)
			if (preserve_masks[attachment] & (1u << subpass))
				*preserve++ = attachment;
	}

	VK_ASSERT(num_subpasses > 0);
	VkRenderPassCreateInfo rp_info = { VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO };
	rp_info.subpassCount = num_subpasses;
	rp_info.pSubpasses = subpasses.data();
	rp_info.pAttachments = attachments;
	rp_info.attachmentCount = num_attachments;

	// Add external subpass dependencies.
	for_each_bit(external_color_dependencies | external_depth_dependencies | external_input_dependencies,
	             [&](unsigned subpass) {
		             external_dependencies.emplace_back();
		             auto &dep = external_dependencies.back();
		             dep.srcSubpass = VK_SUBPASS_EXTERNAL;
		             dep.dstSubpass = subpass;

		             if (external_bottom_of_pipe_dependencies & (1u << subpass))
			             dep.srcStageMask |= VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT;

		             if (external_color_dependencies & (1u << subpass))
		             {
			             dep.srcStageMask |= VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT;
			             dep.dstStageMask |= VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT;
			             dep.srcAccessMask |= VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT;
			             dep.dstAccessMask |= VK_ACCESS_COLOR_ATTACHMENT_READ_BIT | VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT;
		             }

		             if (external_depth_dependencies & (1u << subpass))
		             {
			             dep.srcStageMask |= VK_PIPELINE_STAGE_LATE_FRAGMENT_TESTS_BIT;
			             dep.dstStageMask |= VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT | VK_PIPELINE_STAGE_LATE_FRAGMENT_TESTS_BIT;
			             dep.srcAccessMask |= VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT;
			             dep.dstAccessMask |= VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT | VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_READ_BIT;
		             }

		             if (external_input_dependencies & (1u << subpass))
		             {
			             dep.srcStageMask |= VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT |
			                                 VK_PIPELINE_STAGE_LATE_FRAGMENT_TESTS_BIT;
			             dep.dstStageMask |= VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT;
			             dep.srcAccessMask |= VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT |
			                                  VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT;
			             dep.dstAccessMask |= VK_ACCESS_INPUT_ATTACHMENT_READ_BIT;
		             }
	             });

	// Queue up self-dependencies (COLOR | DEPTH) -> INPUT.
	for_each_bit(color_self_dependencies | depth_self_dependencies, [&](unsigned subpass) {
		external_dependencies.emplace_back();
		auto &dep = external_dependencies.back();
		dep.srcSubpass = subpass;
		dep.dstSubpass = subpass;
		dep.dependencyFlags = VK_DEPENDENCY_BY_REGION_BIT;
		if (multiview)
			dep.dependencyFlags |= VK_DEPENDENCY_VIEW_LOCAL_BIT_KHR;

		if (color_self_dependencies & (1u << subpass))
		{
			dep.srcStageMask |= VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT;
			dep.srcAccessMask |= VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT;
		}

		if (depth_self_dependencies & (1u << subpass))
		{
			dep.srcStageMask |= VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT | VK_PIPELINE_STAGE_LATE_FRAGMENT_TESTS_BIT;
			dep.srcAccessMask |= VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT;
		}

		dep.dstStageMask = VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT;
		dep.dstAccessMask = VK_ACCESS_INPUT_ATTACHMENT_READ_BIT;
	});

	// Flush and invalidate caches between each subpass.
	for (unsigned subpass = 1; subpass < num_subpasses; subpass++)
	{
		external_dependencies.emplace_back();
		auto &dep = external_dependencies.back();
		dep.srcSubpass = subpass - 1;
		dep.dstSubpass = subpass;
		dep.dependencyFlags = VK_DEPENDENCY_BY_REGION_BIT;
		if (multiview)
			dep.dependencyFlags |= VK_DEPENDENCY_VIEW_LOCAL_BIT_KHR;

		if (color_attachment_read_write & (1u << (subpass - 1)))
		{
			dep.srcStageMask |= VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT;
			dep.srcAccessMask |= VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT;
		}

		if (depth_stencil_attachment_write & (1u << (subpass - 1)))
		{
			dep.srcStageMask |= VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT | VK_PIPELINE_STAGE_LATE_FRAGMENT_TESTS_BIT;
			dep.srcAccessMask |= VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT;
		}

		if (color_attachment_read_write & (1u << subpass))
		{
			dep.dstStageMask |= VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT;
			dep.dstAccessMask |= VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT | VK_ACCESS_COLOR_ATTACHMENT_READ_BIT;
		}

		if (depth_stencil_attachment_read & (1u << subpass))
		{
			dep.dstStageMask |= VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT | VK_PIPELINE_STAGE_LATE_FRAGMENT_TESTS_BIT;
			dep.dstAccessMask |= VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_READ_BIT;
		}

		if (depth_stencil_attachment_write & (1u << subpass))
		{
			dep.dstStageMask |= VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT | VK_PIPELINE_STAGE_LATE_FRAGMENT_TESTS_BIT;
			dep.dstAccessMask |= VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT | VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_READ_BIT;
		}

		if (input_attachment_read & (1u << subpass))
		{
			dep.dstStageMask |= VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT;
			dep.dstAccessMask |= VK_ACCESS_INPUT_ATTACHMENT_READ_BIT;
		}
	}

	if (!external_dependencies.empty())
	{
		rp_info.dependencyCount = external_dependencies.size();
		rp_info.pDependencies = external_dependencies.data();
	}

	// Store the important subpass information for later.
	setup_subpasses(rp_info);

	VkRenderPassMultiviewCreateInfoKHR multiview_info = { VK_STRUCTURE_TYPE_RENDER_PASS_MULTIVIEW_CREATE_INFO_KHR };
	vector<uint32_t> multiview_view_mask;
	if (multiview && device->get_device_features().multiview_features.multiview)
	{
		multiview_view_mask.resize(num_subpasses);
		multiview_info.subpassCount = num_subpasses;
		for (auto &mask : multiview_view_mask)
			mask = ((1u << info.num_layers) - 1u) << info.base_layer;
		multiview_info.pViewMasks = multiview_view_mask.data();
		rp_info.pNext = &multiview_info;
	}
	else if (multiview)
		LOGE("Multiview not supported. Predending render pass is not multiview.");

	// Fixup after, we want the Fossilize render pass to be generic.
	VkAttachmentDescription fixup_attachments[VULKAN_NUM_ATTACHMENTS + 1];
	fixup_render_pass_workaround(rp_info, fixup_attachments);

#ifdef VULKAN_DEBUG
	LOGI("Creating render pass.\n");
#endif
	auto &table = device->get_device_table();
	if (table.vkCreateRenderPass(device->get_device(), &rp_info, nullptr, &render_pass) != VK_SUCCESS)
		LOGE("Failed to create render pass.");

#ifdef GRANITE_VULKAN_FOSSILIZE
	device->register_render_pass(render_pass, get_hash(), rp_info);
#endif
}

void RenderPass::fixup_render_pass_workaround(VkRenderPassCreateInfo &create_info, VkAttachmentDescription *attachments)
{
	if (device->get_workarounds().force_store_in_render_pass)
	{
		// Workaround a bug on NV where depth-stencil input attachments break if we have STORE_OP_DONT_CARE.
		// Force STORE_OP_STORE for all attachments.
		if (attachments != create_info.pAttachments)
		{
			memcpy(attachments, create_info.pAttachments, create_info.attachmentCount * sizeof(attachments[0]));
			create_info.pAttachments = attachments;
		}

		for (uint32_t i = 0; i < create_info.attachmentCount; i++)
		{
			VkFormat format = attachments[i].format;
			auto aspect = format_to_aspect_mask(format);
			if ((aspect & (VK_IMAGE_ASPECT_COLOR_BIT | VK_IMAGE_ASPECT_DEPTH_BIT)) != 0)
				attachments[i].storeOp = VK_ATTACHMENT_STORE_OP_STORE;
			if ((aspect & VK_IMAGE_ASPECT_STENCIL_BIT) != 0)
				attachments[i].stencilStoreOp = VK_ATTACHMENT_STORE_OP_STORE;
		}
	}
}

RenderPass::~RenderPass()
{
	auto &table = device->get_device_table();
	if (render_pass != VK_NULL_HANDLE)
		table.vkDestroyRenderPass(device->get_device(), render_pass, nullptr);
}

unsigned Framebuffer::setup_raw_views(VkImageView *views, const RenderPassInfo &info)
{
	unsigned num_views = 0;
	for (unsigned i = 0; i < info.num_color_attachments; i++)
	{
		VK_ASSERT(info.color_attachments[i]);

		// For multiview, we use view indices to pick right layers.
		if (info.num_layers > 1)
			views[num_views++] = info.color_attachments[i]->get_view();
		else
			views[num_views++] = info.color_attachments[i]->get_render_target_view(info.base_layer);
	}

	if (info.depth_stencil)
	{
		// For multiview, we use view indices to pick right layers.
		if (info.num_layers > 1)
			views[num_views++] = info.depth_stencil->get_view();
		else
			views[num_views++] = info.depth_stencil->get_render_target_view(info.base_layer);
	}

	return num_views;
}

static const ImageView *get_image_view(const RenderPassInfo &info, unsigned index)
{
	if (index < info.num_color_attachments)
		return info.color_attachments[index];
	else
		return info.depth_stencil;
}

void Framebuffer::compute_attachment_dimensions(const RenderPassInfo &info, unsigned index,
                                                uint32_t &width, uint32_t &height)
{
	auto *view = get_image_view(info, index);
	VK_ASSERT(view);
	unsigned lod = view->get_create_info().base_level;
	width = view->get_image().get_width(lod);
	height = view->get_image().get_height(lod);
}

void Framebuffer::compute_dimensions(const RenderPassInfo &info, uint32_t &width, uint32_t &height)
{
	width = UINT32_MAX;
	height = UINT32_MAX;
	VK_ASSERT(info.num_color_attachments || info.depth_stencil);

	for (unsigned i = 0; i < info.num_color_attachments; i++)
	{
		VK_ASSERT(info.color_attachments[i]);
		unsigned lod = info.color_attachments[i]->get_create_info().base_level;
		width = min(width, info.color_attachments[i]->get_image().get_width(lod));
		height = min(height, info.color_attachments[i]->get_image().get_height(lod));
	}

	if (info.depth_stencil)
	{
		unsigned lod = info.depth_stencil->get_create_info().base_level;
		width = min(width, info.depth_stencil->get_image().get_width(lod));
		height = min(height, info.depth_stencil->get_image().get_height(lod));
	}
}

Framebuffer::Framebuffer(Device *device_, const RenderPass &rp, const RenderPassInfo &info_)
    : Cookie(device_)
    , device(device_)
    , render_pass(rp)
    , info(info_)
{
	compute_dimensions(info_, width, height);
	VkImageView views[VULKAN_NUM_ATTACHMENTS + 1];
	unsigned num_views = 0;

	num_views = setup_raw_views(views, info_);

	VkFramebufferCreateInfo fb_info = { VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO };
	fb_info.renderPass = rp.get_render_pass();
	fb_info.attachmentCount = num_views;
	fb_info.pAttachments = views;
	fb_info.width = width;
	fb_info.height = height;
	fb_info.layers = 1; // For multiview, layers must be 1. The render pass encodes a mask.

	auto &table = device->get_device_table();
	if (table.vkCreateFramebuffer(device->get_device(), &fb_info, nullptr, &framebuffer) != VK_SUCCESS)
		LOGE("Failed to create framebuffer.");
}

Framebuffer::~Framebuffer()
{
	if (framebuffer != VK_NULL_HANDLE)
	{
		if (internal_sync)
			device->destroy_framebuffer_nolock(framebuffer);
		else
			device->destroy_framebuffer(framebuffer);
	}
}

FramebufferAllocator::FramebufferAllocator(Device *device_)
    : device(device_)
{
}

void FramebufferAllocator::clear()
{
	framebuffers.clear();
}

void FramebufferAllocator::begin_frame()
{
	framebuffers.begin_frame();
}

Framebuffer &FramebufferAllocator::request_framebuffer(const RenderPassInfo &info)
{
	auto &rp = device->request_render_pass(info, true);
	Hasher h;
	h.u64(rp.get_hash());

	for (unsigned i = 0; i < info.num_color_attachments; i++)
	{
		VK_ASSERT(info.color_attachments[i]);
		h.u64(info.color_attachments[i]->get_cookie());
	}

	if (info.depth_stencil)
		h.u64(info.depth_stencil->get_cookie());

	// For multiview we bind the whole attachment, and base layer is encoded in the render pass.
	if (info.num_layers > 1)
		h.u32(0);
	else
		h.u32(info.base_layer);

	auto hash = h.get();

	LOCK();
	auto *node = framebuffers.request(hash);
	if (node)
		return *node;

	return *framebuffers.emplace(hash, device, rp, info);
}

void TransientAttachmentAllocator::clear()
{
	attachments.clear();
}

void TransientAttachmentAllocator::begin_frame()
{
	attachments.begin_frame();
}

ImageHandle TransientAttachmentAllocator::request_attachment(unsigned width, unsigned height, VkFormat format,
                                                             unsigned index, unsigned samples, unsigned layers)
{
	Hasher h;
	h.u32(width);
	h.u32(height);
	h.u32(format);
	h.u32(index);
	h.u32(samples);
	h.u32(layers);

	auto hash = h.get();

	LOCK();
	auto *node = attachments.request(hash);
	if (node)
		return node->handle;

	auto image_info = ImageCreateInfo::transient_render_target(width, height, format);

	image_info.samples = static_cast<VkSampleCountFlagBits>(samples);
	image_info.layers = layers;
	node = attachments.emplace(hash, device->create_image(image_info, nullptr));
	node->handle->set_internal_sync_object();
	node->handle->get_view().set_internal_sync_object();
	device->set_name(*node->handle, "AttachmentAllocator");
	return node->handle;
}
}
