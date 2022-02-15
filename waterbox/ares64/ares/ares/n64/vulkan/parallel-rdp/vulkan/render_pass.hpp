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

#include "cookie.hpp"
#include "hash.hpp"
#include "image.hpp"
#include "intrusive.hpp"
#include "limits.hpp"
#include "object_pool.hpp"
#include "temporary_hashmap.hpp"
#include "vulkan_headers.hpp"

namespace Vulkan
{
enum RenderPassOp
{
	RENDER_PASS_OP_CLEAR_DEPTH_STENCIL_BIT = 1 << 0,
	RENDER_PASS_OP_LOAD_DEPTH_STENCIL_BIT = 1 << 1,
	RENDER_PASS_OP_STORE_DEPTH_STENCIL_BIT = 1 << 2,
	RENDER_PASS_OP_DEPTH_STENCIL_READ_ONLY_BIT = 1 << 3,
	RENDER_PASS_OP_ENABLE_TRANSIENT_STORE_BIT = 1 << 4,
	RENDER_PASS_OP_ENABLE_TRANSIENT_LOAD_BIT = 1 << 5
};
using RenderPassOpFlags = uint32_t;

class ImageView;
struct RenderPassInfo
{
	const ImageView *color_attachments[VULKAN_NUM_ATTACHMENTS];
	const ImageView *depth_stencil = nullptr;
	unsigned num_color_attachments = 0;
	RenderPassOpFlags op_flags = 0;
	uint32_t clear_attachments = 0;
	uint32_t load_attachments = 0;
	uint32_t store_attachments = 0;
	uint32_t base_layer = 0;
	uint32_t num_layers = 1;

	// Render area will be clipped to the actual framebuffer.
	VkRect2D render_area = { { 0, 0 }, { UINT32_MAX, UINT32_MAX } };

	VkClearColorValue clear_color[VULKAN_NUM_ATTACHMENTS] = {};
	VkClearDepthStencilValue clear_depth_stencil = { 1.0f, 0 };

	enum class DepthStencil
	{
		None,
		ReadOnly,
		ReadWrite
	};

	struct Subpass
	{
		uint32_t color_attachments[VULKAN_NUM_ATTACHMENTS];
		uint32_t input_attachments[VULKAN_NUM_ATTACHMENTS];
		uint32_t resolve_attachments[VULKAN_NUM_ATTACHMENTS];
		unsigned num_color_attachments = 0;
		unsigned num_input_attachments = 0;
		unsigned num_resolve_attachments = 0;
		DepthStencil depth_stencil_mode = DepthStencil::ReadWrite;
	};
	// If 0/nullptr, assume a default subpass.
	const Subpass *subpasses = nullptr;
	unsigned num_subpasses = 0;
};

class RenderPass : public HashedObject<RenderPass>, public NoCopyNoMove
{
public:
	struct SubpassInfo
	{
		VkAttachmentReference color_attachments[VULKAN_NUM_ATTACHMENTS];
		unsigned num_color_attachments;
		VkAttachmentReference input_attachments[VULKAN_NUM_ATTACHMENTS];
		unsigned num_input_attachments;
		VkAttachmentReference depth_stencil_attachment;

		unsigned samples;
	};

	RenderPass(Util::Hash hash, Device *device, const RenderPassInfo &info);
	RenderPass(Util::Hash hash, Device *device, const VkRenderPassCreateInfo &create_info);
	~RenderPass();

	unsigned get_num_subpasses() const
	{
		return unsigned(subpasses_info.size());
	}

	VkRenderPass get_render_pass() const
	{
		return render_pass;
	}

	uint32_t get_sample_count(unsigned subpass) const
	{
		VK_ASSERT(subpass < subpasses_info.size());
		return subpasses_info[subpass].samples;
	}

	unsigned get_num_color_attachments(unsigned subpass) const
	{
		VK_ASSERT(subpass < subpasses_info.size());
		return subpasses_info[subpass].num_color_attachments;
	}

	unsigned get_num_input_attachments(unsigned subpass) const
	{
		VK_ASSERT(subpass < subpasses_info.size());
		return subpasses_info[subpass].num_input_attachments;
	}

	const VkAttachmentReference &get_color_attachment(unsigned subpass, unsigned index) const
	{
		VK_ASSERT(subpass < subpasses_info.size());
		VK_ASSERT(index < subpasses_info[subpass].num_color_attachments);
		return subpasses_info[subpass].color_attachments[index];
	}

	const VkAttachmentReference &get_input_attachment(unsigned subpass, unsigned index) const
	{
		VK_ASSERT(subpass < subpasses_info.size());
		VK_ASSERT(index < subpasses_info[subpass].num_input_attachments);
		return subpasses_info[subpass].input_attachments[index];
	}

	bool has_depth(unsigned subpass) const
	{
		VK_ASSERT(subpass < subpasses_info.size());
		return subpasses_info[subpass].depth_stencil_attachment.attachment != VK_ATTACHMENT_UNUSED &&
		       format_has_depth_aspect(depth_stencil);
	}

	bool has_stencil(unsigned subpass) const
	{
		VK_ASSERT(subpass < subpasses_info.size());
		return subpasses_info[subpass].depth_stencil_attachment.attachment != VK_ATTACHMENT_UNUSED &&
		       format_has_stencil_aspect(depth_stencil);
	}

private:
	Device *device;
	VkRenderPass render_pass = VK_NULL_HANDLE;

	VkFormat color_attachments[VULKAN_NUM_ATTACHMENTS] = {};
	VkFormat depth_stencil = VK_FORMAT_UNDEFINED;
	std::vector<SubpassInfo> subpasses_info;

	void setup_subpasses(const VkRenderPassCreateInfo &create_info);

	void fixup_render_pass_workaround(VkRenderPassCreateInfo &create_info, VkAttachmentDescription *attachments);
};

class Framebuffer : public Cookie, public NoCopyNoMove, public InternalSyncEnabled
{
public:
	Framebuffer(Device *device, const RenderPass &rp, const RenderPassInfo &info);
	~Framebuffer();

	VkFramebuffer get_framebuffer() const
	{
		return framebuffer;
	}

	static unsigned setup_raw_views(VkImageView *views, const RenderPassInfo &info);
	static void compute_dimensions(const RenderPassInfo &info, uint32_t &width, uint32_t &height);
	static void compute_attachment_dimensions(const RenderPassInfo &info, unsigned index, uint32_t &width, uint32_t &height);

	uint32_t get_width() const
	{
		return width;
	}

	uint32_t get_height() const
	{
		return height;
	}

	const RenderPass &get_compatible_render_pass() const
	{
		return render_pass;
	}

private:
	Device *device;
	VkFramebuffer framebuffer = VK_NULL_HANDLE;
	const RenderPass &render_pass;
	RenderPassInfo info;
	uint32_t width = 0;
	uint32_t height = 0;
};

static const unsigned VULKAN_FRAMEBUFFER_RING_SIZE = 8;
class FramebufferAllocator
{
public:
	explicit FramebufferAllocator(Device *device);
	Framebuffer &request_framebuffer(const RenderPassInfo &info);

	void begin_frame();
	void clear();

private:
	struct FramebufferNode : Util::TemporaryHashmapEnabled<FramebufferNode>,
	                         Util::IntrusiveListEnabled<FramebufferNode>,
	                         Framebuffer
	{
		FramebufferNode(Device *device_, const RenderPass &rp, const RenderPassInfo &info_)
		    : Framebuffer(device_, rp, info_)
		{
			set_internal_sync_object();
		}
	};

	Device *device;
	Util::TemporaryHashmap<FramebufferNode, VULKAN_FRAMEBUFFER_RING_SIZE, false> framebuffers;
#ifdef GRANITE_VULKAN_MT
	std::mutex lock;
#endif
};

class TransientAttachmentAllocator
{
public:
	TransientAttachmentAllocator(Device *device_)
		: device(device_)
	{
	}

	ImageHandle request_attachment(unsigned width, unsigned height, VkFormat format,
	                               unsigned index = 0, unsigned samples = 1, unsigned layers = 1);

	void begin_frame();
	void clear();

private:
	struct TransientNode : Util::TemporaryHashmapEnabled<TransientNode>, Util::IntrusiveListEnabled<TransientNode>
	{
		explicit TransientNode(ImageHandle handle_)
		    : handle(std::move(handle_))
		{
		}

		ImageHandle handle;
	};

	Device *device;
	Util::TemporaryHashmap<TransientNode, VULKAN_FRAMEBUFFER_RING_SIZE, false> attachments;
#ifdef GRANITE_VULKAN_MT
	std::mutex lock;
#endif
};
}

