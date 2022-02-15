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

#include <memory>
#include <thread>
#include <queue>
#include "device.hpp"
#include "video_interface.hpp"
#include "rdp_renderer.hpp"
#include "rdp_common.hpp"
#include "command_ring.hpp"
#include "worker_thread.hpp"
#include "rdp_dump_write.hpp"

#ifndef GRANITE_VULKAN_MT
#error "Granite Vulkan backend must be built with multithreading support."
#endif

namespace RDP
{
struct RGBA
{
	uint8_t r, g, b, a;
};

enum CommandProcessorFlagBits
{
	COMMAND_PROCESSOR_FLAG_HOST_VISIBLE_HIDDEN_RDRAM_BIT = 1 << 0,
	COMMAND_PROCESSOR_FLAG_HOST_VISIBLE_TMEM_BIT = 1 << 1,
	COMMAND_PROCESSOR_FLAG_UPSCALING_2X_BIT = 1 << 2,
	COMMAND_PROCESSOR_FLAG_UPSCALING_4X_BIT = 1 << 3,
	COMMAND_PROCESSOR_FLAG_UPSCALING_8X_BIT = 1 << 4,
	COMMAND_PROCESSOR_FLAG_SUPER_SAMPLED_READ_BACK_BIT = 1 << 5,
	COMMAND_PROCESSOR_FLAG_SUPER_SAMPLED_DITHER_BIT = 1 << 6
};
using CommandProcessorFlags = uint32_t;

struct CoherencyCopy
{
	size_t src_offset = 0;
	size_t mask_offset = 0;
	size_t dst_offset = 0;
	size_t size = 0;
	std::atomic_uint32_t *counter_base = nullptr;
	unsigned counters = 0;
};

struct CoherencyOperation
{
	Vulkan::Fence fence;
	uint64_t timeline_value = 0;

	uint8_t *dst = nullptr;
	const Vulkan::Buffer *src = nullptr;
	std::vector<CoherencyCopy> copies;
	std::atomic_uint32_t *unlock_cookie = nullptr;
};

// These options control various behavior when upscaling to workaround glitches which arise naturally as part of upscaling.
struct Quirks
{
	inline Quirks()
	{
		u.options.native_resolution_tex_rect = true;
		u.options.native_texture_lod = false;
	}

	inline void set_native_resolution_tex_rect(bool enable)
	{
		u.options.native_resolution_tex_rect = enable;
	}

	inline void set_native_texture_lod(bool enable)
	{
		u.options.native_texture_lod = enable;
	}

	union
	{
		struct Opts
		{
			// If true, force TEX_RECT and TEX_RECT_FLIP to render without upscaling.
			// Works around bilinear filtering bugs in Cycle1/Cycle2 mode where game assumed 1:1 pixel transfer.
			bool native_resolution_tex_rect;

			// Forces LOD to be computed as 1x upscale.
			// Fixes content which relies on LOD computation to select textures in clever ways.
			bool native_texture_lod;
		} options;
		uint32_t words[1];
	} u;
};

class CommandProcessor
{
public:
	CommandProcessor(Vulkan::Device &device,
	                 void *rdram_ptr,
	                 size_t rdram_offset,
	                 size_t rdram_size,
	                 size_t hidden_rdram_size,
	                 CommandProcessorFlags flags);

	~CommandProcessor();

	bool device_is_supported() const;

	// Synchronization.
	void flush();
	uint64_t signal_timeline();
	void wait_for_timeline(uint64_t index);
	void idle();
	void begin_frame_context();

	// Queues up state and drawing commands.
	void enqueue_command(unsigned num_words, const uint32_t *words);
	void enqueue_command_direct(unsigned num_words, const uint32_t *words);

	void set_quirks(const Quirks &quirks);

	// Interact with memory.
	void *begin_read_rdram();
	void end_write_rdram();
	void *begin_read_hidden_rdram();
	void end_write_hidden_rdram();
	size_t get_rdram_size() const;
	size_t get_hidden_rdram_size() const;
	void *get_tmem();

	// Sets VI register
	void set_vi_register(VIRegister reg, uint32_t value);

	Vulkan::ImageHandle scanout(const ScanoutOptions &opts = {});
	void scanout_sync(std::vector<RGBA> &colors, unsigned &width, unsigned &height);
	void scanout_async_buffer(VIScanoutBuffer &buffer, const ScanoutOptions &opts = {});

private:
	Vulkan::Device &device;
	Vulkan::BufferHandle rdram;
	Vulkan::BufferHandle hidden_rdram;
	Vulkan::BufferHandle tmem;
	size_t rdram_offset;
	size_t rdram_size;
	CommandProcessorFlags flags;
#ifndef PARALLEL_RDP_SHADER_DIR
	std::unique_ptr<ShaderBank> shader_bank;
#endif

	CommandRing ring;

	VideoInterface vi;
	Renderer renderer;

	void clear_hidden_rdram();
	void clear_tmem();
	void clear_buffer(Vulkan::Buffer &buffer, uint32_t value);
	void init_renderer();
	void enqueue_command_inner(unsigned num_words, const uint32_t *words);

	Vulkan::ImageHandle scanout(const ScanoutOptions &opts, VkImageLayout target_layout);

#define OP(x) void op_##x(const uint32_t *words)
	OP(fill_triangle); OP(fill_z_buffer_triangle); OP(texture_triangle); OP(texture_z_buffer_triangle);
	OP(shade_triangle); OP(shade_z_buffer_triangle); OP(shade_texture_triangle); OP(shade_texture_z_buffer_triangle);
	OP(texture_rectangle); OP(texture_rectangle_flip); OP(sync_load); OP(sync_pipe);
	OP(sync_tile); OP(sync_full); OP(set_key_gb); OP(set_key_r);
	OP(set_convert); OP(set_scissor); OP(set_prim_depth); OP(set_other_modes);
	OP(load_tlut); OP(set_tile_size); OP(load_block);
	OP(load_tile); OP(set_tile); OP(fill_rectangle); OP(set_fill_color);
	OP(set_fog_color); OP(set_blend_color); OP(set_prim_color); OP(set_env_color);
	OP(set_combine); OP(set_texture_image); OP(set_mask_image); OP(set_color_image);
#undef OP

	ScissorState scissor_state = {};
	StaticRasterizationState static_state = {};
	DepthBlendState depth_blend = {};

	struct
	{
		uint32_t addr;
		uint32_t width;
		TextureFormat fmt;
		TextureSize size;
	} texture_image = {};

	uint64_t timeline_value = 0;
	uint64_t thread_timeline_value = 0;

	struct FenceExecutor
	{
		explicit inline FenceExecutor(Vulkan::Device *device_, uint64_t *ptr)
			: device(device_), value(ptr)
		{
		}

		Vulkan::Device *device;
		uint64_t *value;
		bool is_sentinel(const CoherencyOperation &work) const;
		void perform_work(CoherencyOperation &work);
		void notify_work_locked(const CoherencyOperation &work);
	};
	WorkerThread<CoherencyOperation, FenceExecutor> timeline_worker;

	uint8_t *host_rdram = nullptr;
	bool measure_stall_time = false;
	bool single_threaded_processing = false;
	bool is_supported = false;
	bool is_host_coherent = true;
	bool timestamp = false;

	friend class Renderer;

	void enqueue_coherency_operation(CoherencyOperation &&op);
	void drain_command_ring();
	void decode_triangle_setup(TriangleSetup &setup, const uint32_t *words) const;

	Quirks quirks;

	std::unique_ptr<RDPDumpWriter> dump_writer;
	bool dump_in_command_list = false;
};
}
