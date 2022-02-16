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

#include "device.hpp"
#include "renderdoc_app.h"
#include <mutex>

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#else
#include <dlfcn.h>
#endif

namespace Vulkan
{
static std::mutex module_lock;
#ifdef _WIN32
static HMODULE renderdoc_module;
#else
static void *renderdoc_module;
#endif

static RENDERDOC_API_1_0_0 *renderdoc_api;

bool Device::init_renderdoc_capture()
{
	std::lock_guard<std::mutex> holder{module_lock};
	if (renderdoc_module)
		return true;

#ifdef _WIN32
	renderdoc_module = GetModuleHandleA("renderdoc.dll");
#elif defined(ANDROID)
	renderdoc_module = dlopen("libVkLayer_GLES_RenderDoc.so", RTLD_NOW | RTLD_NOLOAD);
#else
	renderdoc_module = dlopen("librenderdoc.so", RTLD_NOW | RTLD_NOLOAD);
#endif

	if (!renderdoc_module)
	{
		LOGE("Failed to load RenderDoc, make sure RenderDoc started the application in capture mode.\n");
		return false;
	}

#ifdef _WIN32
	// Workaround GCC warning about FARPROC mismatch.
	auto *gpa = GetProcAddress(renderdoc_module, "RENDERDOC_GetAPI");
	pRENDERDOC_GetAPI func;
	memcpy(&func, &gpa, sizeof(func));

	if (!func)
	{
		LOGE("Failed to load RENDERDOC_GetAPI function.\n");
		return false;
	}
#else
	auto *func = reinterpret_cast<pRENDERDOC_GetAPI>(dlsym(renderdoc_module, "RENDERDOC_GetAPI"));
	if (!func)
	{
		LOGE("Failed to load RENDERDOC_GetAPI function.\n");
		return false;
	}
#endif

	if (!func(eRENDERDOC_API_Version_1_0_0, reinterpret_cast<void **>(&renderdoc_api)))
	{
		LOGE("Failed to obtain RenderDoc 1.0.0 API.\n");
		return false;
	}
	else
	{
		int major, minor, patch;
		renderdoc_api->GetAPIVersion(&major, &minor, &patch);
		LOGI("Initialized RenderDoc API %d.%d.%d.\n", major, minor, patch);
	}

	return true;
}

void Device::begin_renderdoc_capture()
{
	std::lock_guard<std::mutex> holder{module_lock};
	if (!renderdoc_api)
	{
		LOGE("RenderDoc API is not loaded, cannot trigger capture.\n");
		return;
	}
	next_frame_context();

	LOGI("Starting RenderDoc frame capture.\n");
	renderdoc_api->StartFrameCapture(nullptr, nullptr);
}

void Device::end_renderdoc_capture()
{
	std::lock_guard<std::mutex> holder{module_lock};
	if (!renderdoc_api)
	{
		LOGE("RenderDoc API is not loaded, cannot trigger capture.\n");
		return;
	}
	next_frame_context();
	renderdoc_api->EndFrameCapture(nullptr, nullptr);
	LOGI("Ended RenderDoc frame capture.\n");
}

}
