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

#include <stdio.h>
#include <string.h>
#include <stdarg.h>

namespace Util
{
class LoggingInterface
{
public:
	virtual ~LoggingInterface() = default;
	virtual bool log(const char *tag, const char *fmt, va_list va) = 0;
};

bool interface_log(const char *tag, const char *fmt, ...);
void set_thread_logging_interface(LoggingInterface *iface);
}

#if defined(_MSC_VER)
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#define LOGE_FALLBACK(...) do { \
    fprintf(stderr, "[ERROR]: " __VA_ARGS__); \
    fflush(stderr); \
    char buffer[16 * 1024]; \
    snprintf(buffer, sizeof(buffer), "[ERROR]: " __VA_ARGS__); \
    OutputDebugStringA(buffer); \
} while(false)

#define LOGW_FALLBACK(...) do { \
    fprintf(stderr, "[WARN]: " __VA_ARGS__); \
    fflush(stderr); \
    char buffer[16 * 1024]; \
    snprintf(buffer, sizeof(buffer), "[WARN]: " __VA_ARGS__); \
    OutputDebugStringA(buffer); \
} while(false)

#define LOGI_FALLBACK(...) do { \
    fprintf(stderr, "[INFO]: " __VA_ARGS__); \
    fflush(stderr); \
    char buffer[16 * 1024]; \
    snprintf(buffer, sizeof(buffer), "[INFO]: " __VA_ARGS__); \
    OutputDebugStringA(buffer); \
} while(false)
#elif defined(ANDROID)
#include <android/log.h>
#define LOGE_FALLBACK(...) do { __android_log_print(ANDROID_LOG_ERROR, "Granite", __VA_ARGS__); } while(0)
#define LOGW_FALLBACK(...) do { __android_log_print(ANDROID_LOG_WARN, "Granite", __VA_ARGS__); } while(0)
#define LOGI_FALLBACK(...) do { __android_log_print(ANDROID_LOG_INFO, "Granite", __VA_ARGS__); } while(0)
#else
#define LOGE_FALLBACK(...)                        \
	do                                            \
	{                                             \
		fprintf(stderr, "[ERROR]: " __VA_ARGS__); \
		fflush(stderr);                           \
	} while (false)

#define LOGW_FALLBACK(...)                       \
	do                                           \
	{                                            \
		fprintf(stderr, "[WARN]: " __VA_ARGS__); \
		fflush(stderr);                          \
	} while (false)

#define LOGI_FALLBACK(...)                       \
	do                                           \
	{                                            \
		fprintf(stderr, "[INFO]: " __VA_ARGS__); \
		fflush(stderr);                          \
	} while (false)
#endif

#define LOGE(...) do { if (!::Util::interface_log("[ERROR]: ", __VA_ARGS__)) { LOGE_FALLBACK(__VA_ARGS__); }} while(0)
#define LOGW(...) do { if (!::Util::interface_log("[WARN]: ", __VA_ARGS__)) { LOGW_FALLBACK(__VA_ARGS__); }} while(0)
#define LOGI(...) do { if (!::Util::interface_log("[INFO]: ", __VA_ARGS__)) { LOGI_FALLBACK(__VA_ARGS__); }} while(0)

