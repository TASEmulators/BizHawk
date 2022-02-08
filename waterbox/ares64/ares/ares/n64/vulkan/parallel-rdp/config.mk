# For use in standalone implementations.

PARALLEL_RDP_CFLAGS :=
PARALLEL_RDP_CXXFLAGS := -DGRANITE_VULKAN_MT

PARALLEL_RDP_SOURCES_CXX := \
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
        $(PARALLEL_RDP_IMPLEMENTATION)/util/thread_name.cpp

PARALLEL_RDP_SOURCES_C := \
        $(PARALLEL_RDP_IMPLEMENTATION)/volk/volk.c

PARALLEL_RDP_INCLUDE_DIRS := \
        -I$(PARALLEL_RDP_IMPLEMENTATION)/parallel-rdp \
        -I$(PARALLEL_RDP_IMPLEMENTATION)/volk \
        -I$(PARALLEL_RDP_IMPLEMENTATION)/vulkan \
        -I$(PARALLEL_RDP_IMPLEMENTATION)/vulkan-headers/include \
        -I$(PARALLEL_RDP_IMPLEMENTATION)/util

PARALLEL_RDP_LDFLAGS := -pthread
ifeq (,$(findstring win,$(platform)))
    PARALLEL_RDP_LDFLAGS += -ldl
else
    PARALLEL_RDP_CFLAGS += -DVK_USE_PLATFORM_WIN32_KHR
    PARALLEL_RDP_LDFLAGS += -lwinmm
endif

