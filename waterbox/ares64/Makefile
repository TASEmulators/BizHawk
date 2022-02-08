NEED_LIBCO := 1

ARES_PATH = $(ROOT_DIR)/ares/ares
MAME_PATH = $(ROOT_DIR)/ares/thirdparty/mame

CXXFLAGS := -std=c++17 -msse4.2 \
	-I../libco -I.$(ROOT_DIR)/ares/ -I.$(ROOT_DIR)/ares/thirdparty/ -I.$(ARES_PATH) \
	-Werror=int-to-pointer-cast -Wno-unused-but-set-variable \
	-Wno-parentheses -Wno-reorder -Wno-unused-variable \
	-Wno-sign-compare -Wno-switch -Wno-unused-local-typedefs \
	-fno-strict-aliasing -fwrapv -fno-operator-names \
	-I.$(MAME_PATH)/devices -I.$(MAME_PATH)/emu \
	-I.$(MAME_PATH)/lib/util -I.$(MAME_PATH)/mame \
	-I.$(MAME_PATH)/osd -DMAME_RDP -DLSB_FIRST -DPTR64 -DSDLMAME_EMSCRIPTEN

TARGET = ares64.wbx

SRCS_PROCESSORS = \
	$(ARES_PATH)/component/processor/sm5k/sm5k.cpp

SRCS_ARES = \
	$(ARES_PATH)/ares/ares.cpp \
	$(ARES_PATH)/ares/memory/fixed-allocator.cpp

SRCS_N64 = \
	$(ARES_PATH)/n64/memory/memory.cpp \
	$(ARES_PATH)/n64/system/system.cpp \
	$(ARES_PATH)/n64/cartridge/cartridge.cpp \
	$(ARES_PATH)/n64/controller/controller.cpp \
	$(ARES_PATH)/n64/dd/dd.cpp \
	$(ARES_PATH)/n64/sp/sp.cpp \
	$(ARES_PATH)/n64/dp/dp.cpp \
	$(ARES_PATH)/n64/mi/mi.cpp \
	$(ARES_PATH)/n64/vi/vi.cpp \
	$(ARES_PATH)/n64/ai/ai.cpp \
	$(ARES_PATH)/n64/pi/pi.cpp \
	$(ARES_PATH)/n64/ri/ri.cpp \
	$(ARES_PATH)/n64/si/si.cpp \
	$(ARES_PATH)/n64/rdram/rdram.cpp \
	$(ARES_PATH)/n64/cpu/cpu.cpp \
	$(ARES_PATH)/n64/rdp/rdp.cpp \
	$(ARES_PATH)/n64/rsp/rsp.cpp \
	$(ARES_PATH)/n64/vulkan/vulkan.cpp

PARALLEL_RDP_IMPLEMENTATION = $(ARES_PATH)/n64/vulkan/parallel-rdp

SRCS_PARALLEL_RDP = \
	$(wildcard $(PARALLEL_RDP_IMPLEMENTATION)/parallel-rdp/*.cpp) \
	$(PARALLEL_RDP_IMPLEMENTATION)/vulkan/buffer.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/vulkan/buffer_pool.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/vulkan/command_buffer.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/vulkan/command_pool.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/vulkan/context.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/vulkan/cookie.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/vulkan/descriptor_set.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/vulkan/device.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/vulkan/event_manager.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/vulkan/fence.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/vulkan/fence_manager.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/vulkan/image.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/vulkan/memory_allocator.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/vulkan/pipeline_event.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/vulkan/query_pool.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/vulkan/render_pass.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/vulkan/sampler.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/vulkan/semaphore.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/vulkan/semaphore_manager.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/vulkan/shader.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/vulkan/texture_format.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/util/logging.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/util/thread_id.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/util/aligned_alloc.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/util/timer.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/util/timeline_trace_file.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/util/thread_name.cpp \
	$(PARALLEL_RDP_IMPLEMENTATION)/volk/volk.c

PARALLEL_RDP_INCLUDE_DIRS = \
	-I.$(PARALLEL_RDP_IMPLEMENTATION)/parallel-rdp \
	-I.$(PARALLEL_RDP_IMPLEMENTATION)/volk \
	-I.$(PARALLEL_RDP_IMPLEMENTATION)/vulkan \
	-I.$(PARALLEL_RDP_IMPLEMENTATION)/vulkan-headers/include \
	-I.$(PARALLEL_RDP_IMPLEMENTATION)/util

CXXFLAGS := $(CXXFLAGS) $(PARALLEL_RDP_INCLUDE_DIRS) -DVULKAN -DGRANITE_VULKAN_MT
CCFLAGS := $(PARALLEL_RDP_INCLUDE_DIRS)

SRCS_MAME = \
	$(MAME_PATH)/emu/emucore.cpp \
	$(MAME_PATH)/lib/util/delegate.cpp \
	$(MAME_PATH)/lib/util/strformat.cpp \
	$(MAME_PATH)/mame/video/n64.cpp \
	$(MAME_PATH)/mame/video/pin64.cpp \
	$(MAME_PATH)/mame/video/rdpblend.cpp \
	$(MAME_PATH)/mame/video/rdptpipe.cpp \
	$(MAME_PATH)/osd/osdcore.cpp \
	$(MAME_PATH)/osd/osdsync.cpp

SRCS = $(SRCS_PARALLEL_RDP) $(SRCS_PROCESSORS) $(SRCS_ARES) $(SRCS_N64) $(SRCS_MAME) BizInterface.cpp

include ../common.mak
