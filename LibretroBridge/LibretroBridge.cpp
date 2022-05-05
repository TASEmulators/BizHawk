#include "LibretroBridge.h"

#include <cassert>
#include <cstdarg>
#include <cstring>
#include <memory>
#include <string>
#include <vector>

namespace LibretroBridge {

class CallbackHandler;

class CallbackHandler {
public:
	CallbackHandler()
	: supportsNoGame(false)
	, retroMessageString()
	, retroMessageTime(0)
	, variablesDirty(false)
	, variableCount(0)
	, variableKeys()
	, variableComments()
	, variables()
	, systemDirectory()
	, saveDirectory()
	, coreDirectory()
	, coreAssetsDirectory()
	, rotation(0)
	, pixelFormat(RETRO_PIXEL_FORMAT::ZRGB1555)
	, width(0)
	, height(0)
	, videoBuf()
	, videoBufSz(0)
	, numSamples(0)
	, sampleBuf()
	{
		std::memset(joypads[0], 0, sizeof (joypads[0]));
		std::memset(joypads[1], 0, sizeof (joypads[1]));
		std::memset(mouse, 0, sizeof (mouse));
		std::memset(keyboard, 0, sizeof (keyboard));
		std::memset(lightGun, 0, sizeof (lightGun));
		std::memset(analog, 0, sizeof (analog));
		std::memset(pointer, 0, sizeof (pointer));
	}

	static void RetroLog(RETRO_LOG level, const char* fmt, ...) {
		va_list args;
		va_start(args, fmt);
		std::size_t sz = std::vsnprintf(NULL, 0, fmt, args);
		if (static_cast<s64>(sz) < 0) {
			std::puts("vsnprintf failed!");
			std::fflush(stdout);
			va_end(args);
			return;
		}

		std::unique_ptr<char[]> msg(new char[sz + 1]);
		std::vsnprintf(msg.get(), sz + 1, fmt, args);
		va_end(args);

		std::string finalMsg;
		switch (level) {
			case RETRO_LOG::DEBUG:
				finalMsg = "[RETRO_LOG_DEBUG] ";
				break;
			case RETRO_LOG::INFO:
				finalMsg = "[RETRO_LOG_INFO] ";
				break;
			case RETRO_LOG::WARN:
				finalMsg = "[RETRO_LOG_WARN] ";
				break;
			case RETRO_LOG::ERROR:
				finalMsg = "[RETRO_LOG_ERROR] ";
				break;
			default:
				finalMsg = "[RETRO_LOG_UNKNOWN] ";
				break;
		}

		finalMsg += std::string(msg.get());
		std::printf("%s", finalMsg.c_str());
		std::fflush(stdout);
	}

	boolean RetroEnvironment(u32 cmd, void* data) {
		switch (static_cast<RETRO_ENVIRONMENT>(cmd)) {
			case RETRO_ENVIRONMENT::SET_ROTATION:
				rotation = *static_cast<const u32*>(const_cast<const void*>(data));
				assert(rotation < 4);
				rotation *= 90;
				return true;
			case RETRO_ENVIRONMENT::GET_OVERSCAN:
				*static_cast<boolean*>(data) = false;
				return true;
			case RETRO_ENVIRONMENT::GET_CAN_DUPE:
				*static_cast<boolean*>(data) = true;
				return true;
			case RETRO_ENVIRONMENT::SET_MESSAGE:
			{
				const retro_message* message = static_cast<retro_message*>(data);
				retroMessageString = message->msg;
				retroMessageTime = message->frames;
				return true;
			}
			case RETRO_ENVIRONMENT::SHUTDOWN:
				//TODO low priority
				//maybe we can tell the frontend to stop frame advancing?
				return false;
			case RETRO_ENVIRONMENT::SET_PERFORMANCE_LEVEL:
				//unneeded
				return false;
			case RETRO_ENVIRONMENT::GET_SYSTEM_DIRECTORY:
				*static_cast<const char**>(data) = systemDirectory.c_str();
				return true;
			case RETRO_ENVIRONMENT::SET_PIXEL_FORMAT:
			{
				const u32 tmp = *static_cast<const u32*>(const_cast<const void*>(data));
				assert(tmp < 3);
				pixelFormat = static_cast<RETRO_PIXEL_FORMAT>(tmp);
				return true;
			}
			case RETRO_ENVIRONMENT::SET_INPUT_DESCRIPTORS:
				//TODO medium priority
				//would need some custom form displaying these probably
				return false;
			case RETRO_ENVIRONMENT::SET_KEYBOARD_CALLBACK:
				//TODO high priority (to support keyboard consoles, probably high value for us. but that may take a lot of infrastructure work)
				//the callback set is meant to be called from frontend side, so perhaps we should just call this on a SetInput call with keyboard
				return false;
			case RETRO_ENVIRONMENT::SET_DISK_CONTROL_INTERFACE:
				//TODO high priority (to support disc systems)
				return false;
			case RETRO_ENVIRONMENT::SET_HW_RENDER:
				//TODO high priority (to support 3d renderers)
				return false;
			case RETRO_ENVIRONMENT::GET_VARIABLE:
			{
				//according to retroarch's `core_option_manager_get` this is what we should do

				variablesDirty = false;

				retro_variable* req = static_cast<retro_variable*>(data);
				req->value = nullptr;

				for (u32 i = 0; i < variableCount; i++) {
					if (variableKeys[i] == std::string(req->key)) {
						req->value = variables[i].c_str();
						return true;
					}
				}

				return true;
			}

			case RETRO_ENVIRONMENT::SET_VARIABLES:
			{
				const retro_variable* var = static_cast<const retro_variable*>(data);
				u32 nVars = 0;
				while (var->key) {
					var++;
					nVars++;
				}

				variableCount = nVars;
				variables.reset(new std::string[nVars]);
				variableKeys.reset(new std::string[nVars]);
				variableComments.reset(new std::string[nVars]);
				var = static_cast<const retro_variable*>(data);

				for (u32 i = 0; i < nVars; i++) {
					variableKeys[i] = std::string(var[i].key);
					variableComments[i] = std::string(var[i].value);

					//analyze to find default and save it
					std::string comment = variableComments[i];
					std::size_t ofs = comment.find_first_of(';') + 2;
					std::size_t pipe = comment.find('|', ofs);
					if (pipe == std::string::npos) {
						variables[i] = comment.substr(ofs);
					} else {
						variables[i] = comment.substr(ofs, pipe - ofs);
					}
				}

				return true;
			}

			case RETRO_ENVIRONMENT::GET_VARIABLE_UPDATE:
				*static_cast<boolean*>(data) = variablesDirty;
				return true;
			case RETRO_ENVIRONMENT::SET_SUPPORT_NO_GAME:
				supportsNoGame = *static_cast<const boolean*>(data);
				return true;
			case RETRO_ENVIRONMENT::GET_LIBRETRO_PATH:
				*static_cast<const char**>(data) = coreDirectory.c_str();
				return true;
			case RETRO_ENVIRONMENT::SET_AUDIO_CALLBACK:
				//seems to be meant for async sound?
				//the callback is meant to be called frontend side
				//so in practice this callback is useless
				return false;
			case RETRO_ENVIRONMENT::SET_FRAME_TIME_CALLBACK:
				//the frontend can send real time to the core
				//and the frontend is meant to tamper with
				//this timing for fast forward/slow motion?
				//just no, those are pure frontend responsibilities
				return false;
			case RETRO_ENVIRONMENT::GET_RUMBLE_INTERFACE:
				//TODO low priority
				return false;
			case RETRO_ENVIRONMENT::GET_INPUT_DEVICE_CAPABILITIES:
				//TODO medium priority - other input methods
				*static_cast<u64*>(data) = 1 << static_cast<u32>(RETRO_DEVICE::JOYPAD)
				| 1 << static_cast<u32>(RETRO_DEVICE::KEYBOARD)
				| 1 << static_cast<u32>(RETRO_DEVICE::POINTER);
				return true;
			case RETRO_ENVIRONMENT::GET_LOG_INTERFACE:
			{
				retro_log_callback* cb = static_cast<retro_log_callback*>(data);
				cb->log = &RetroLog;
				return true;
			}
			case RETRO_ENVIRONMENT::GET_PERF_INTERFACE:
				//callbacks for performance counters?
				//for performance logging???
				//just no
				return false;
			case RETRO_ENVIRONMENT::GET_LOCATION_INTERFACE:
				//the frontend is not a GPS
				//the core should not need this info
				return false;
			case RETRO_ENVIRONMENT::GET_CORE_ASSETS_DIRECTORY:
				*static_cast<const char**>(data) = coreAssetsDirectory.c_str();
				return true;
			case RETRO_ENVIRONMENT::GET_SAVE_DIRECTORY:
				*static_cast<const char**>(data) = saveDirectory.c_str();
				return true;
			case RETRO_ENVIRONMENT::SET_SYSTEM_AV_INFO:
				RetroLog(RETRO_LOG::WARN, "NEED RETRO_ENVIRONMENT::SET_SYSTEM_AV_INFO");
				return false;
			case RETRO_ENVIRONMENT::SET_PROC_ADDRESS_CALLBACK:
				//this is some way to get symbols for API extensions
				//of which none exist
				//so this is useless
				return false;
			case RETRO_ENVIRONMENT::SET_SUBSYSTEM_INFO:
				//needs retro_load_game_special to be useful; not supported yet
				return false;
			case RETRO_ENVIRONMENT::SET_CONTROLLER_INFO:
				//TODO medium priority probably
				return false;
			case RETRO_ENVIRONMENT::SET_GEOMETRY:
				//TODO medium priority probably
				//this is essentially just set system av info, except only for video
				return false;
			case RETRO_ENVIRONMENT::GET_USERNAME:
				//we definitely want to return false here so the core will do something deterministic
				return false;
			case RETRO_ENVIRONMENT::GET_LANGUAGE:
				*static_cast<RETRO_LANGUAGE*>(data) = RETRO_LANGUAGE::ENGLISH;
				return true;
			default:
				return false;
		}
	}

	template <u32 rot>
	static u32* PixelAddress(u32 width, u32 height, u32 x, u32 y, u32* dstBuf, u32* dst) {
		switch (rot) {
			case 0:
				return dst;
			case 90:
			{
				u32 dx = y;
				u32 dy = height - x - 1;
				return dstBuf + dy * width + dx;
			}
			case 180:
			{
				u32 dx = width - y - 1;
				u32 dy = height - x - 1;
				return dstBuf + dy * width + dx;
			}
			case 270:
			{
				u32 dx = width - y - 1;
				u32 dy = x;
				return dstBuf + dy * width + dx;
			}
			default:
				__builtin_unreachable();
		}
	}

	template <u32 rot>
	static void Blit555(const u16* srcBuf, u32* dstBuf, u32 width, u32 height, std::size_t pitch) {
		u32* dst = dstBuf;
		for (u32 y = 0; y < height; y++) {
			const u16* row = srcBuf;
			for (u32 x = 0; x < width; x++) {
				u16 ci = *row;
				u32 r = (ci & 0x001F) >> 0;
				u32 g = (ci & 0x03E0) >> 5;
				u32 b = (ci & 0x7C00) >> 10;

				r = (r << 3) | (r >> 2);
				g = (g << 3) | (g >> 2);
				b = (b << 3) | (b >> 2);
				u32 co = r | (g << 8) | (b << 16) | 0xFF000000U;

				*PixelAddress<rot>(width, height, x, y, dstBuf, dst) = co;
				dst++;
				row++;
			}
			srcBuf += pitch / 2;
		}
	}

	template <u32 rot>
	static void Blit565(const u16* srcBuf, u32* dstBuf, u32 width, u32 height, std::size_t pitch) {
		u32* dst = dstBuf;
		for (u32 y = 0; y < height; y++) {
			const u16* row = srcBuf;
			for (u32 x = 0; x < width; x++) {
				u16 ci = *row;
				u32 r = (ci & 0x001F) >> 0;
				u32 g = (ci & 0x07E0) >> 5;
				u32 b = (ci & 0xF800) >> 11;

				r = (r << 3) | (r >> 2);
				g = (g << 2) | (g >> 4);
				b = (b << 3) | (b >> 2);
				u32 co = r | (g << 8) | (b << 16) | 0xFF000000U;

				*PixelAddress<rot>(width, height, x, y, dstBuf, dst) = co;
				dst++;
				row++;
			}
			srcBuf += pitch / 2;
		}
	}

	template <u32 rot>
	static void Blit888(const u32* srcBuf, u32* dstBuf, u32 width, u32 height, std::size_t pitch) {
		u32* dst = dstBuf;
		for (u32 y = 0; y < height; y++) {
			const u32* row = srcBuf;
			for (u32 x = 0; x < width; x++) {
				u32 ci = *row;
				u32 co = ci | 0xFF000000U;
				*PixelAddress<rot>(width, height, x, y, dstBuf, dst) = co;
				dst++;
				row++;
			}
			srcBuf += pitch / 4;
		}
	}

	void RetroVideoRefresh(const void* data, u32 width, u32 height, std::size_t pitch) {
		if (!data) {
			return;
		}

		assert((width * height) <= videoBufSz);
		this->width = width;
		this->height = height;

		switch (pixelFormat) {
			case RETRO_PIXEL_FORMAT::ZRGB1555:
				switch (rotation) {
					case 0: return Blit555<0>(static_cast<const u16*>(data), videoBuf.get(), width, height, pitch);
					case 90: return Blit555<90>(static_cast<const u16*>(data), videoBuf.get(), width, height, pitch);
					case 180: return Blit555<180>(static_cast<const u16*>(data), videoBuf.get(), width, height, pitch);
					case 270: return Blit555<270>(static_cast<const u16*>(data), videoBuf.get(), width, height, pitch);
					default: __builtin_unreachable();
				}
			case RETRO_PIXEL_FORMAT::XRGB8888:
				switch (rotation) {
					case 0: return Blit888<0>(static_cast<const u32*>(data), videoBuf.get(), width, height, pitch);
					case 90: return Blit888<90>(static_cast<const u32*>(data), videoBuf.get(), width, height, pitch);
					case 180: return Blit888<180>(static_cast<const u32*>(data), videoBuf.get(), width, height, pitch);
					case 270: return Blit888<270>(static_cast<const u32*>(data), videoBuf.get(), width, height, pitch);
					default: __builtin_unreachable();
				}
			case RETRO_PIXEL_FORMAT::RGB565:
				switch (rotation) {
					case 0: return Blit565<0>(static_cast<const u16*>(data), videoBuf.get(), width, height, pitch);
					case 90: return Blit565<90>(static_cast<const u16*>(data), videoBuf.get(), width, height, pitch);
					case 180: return Blit565<180>(static_cast<const u16*>(data), videoBuf.get(), width, height, pitch);
					case 270: return Blit565<270>(static_cast<const u16*>(data), videoBuf.get(), width, height, pitch);
					default: __builtin_unreachable();
				}
			default:
				__builtin_unreachable();
		}
	}

	void RetroAudioSample(s16 left, s16 right) {
		sampleBuf.push_back(left);
		sampleBuf.push_back(right);
		numSamples++;
	}
	
	std::size_t RetroAudioSampleBatch(const s16* data, std::size_t frames) {
		const std::size_t ret = frames;
		while (frames--) {
			sampleBuf.push_back(*data++);
			sampleBuf.push_back(*data++);
			numSamples++;
		}
		return ret;
	}

	void RetroInputPoll() {
		// this is useless
	}

	s16 RetroInputState(u32 port, u32 device, __attribute__((unused)) u32 index, u32 id) {
		assert(device < static_cast<u32>(RETRO_DEVICE::LAST));
		switch (static_cast<RETRO_DEVICE>(device)) {
			case RETRO_DEVICE::NONE:
				return 0;
			case RETRO_DEVICE::JOYPAD:
				if (port < 2) {
					assert(id < sizeof (joypads[port]));
					return joypads[port][id];
				}
				return 0; // todo: is this valid?
			case RETRO_DEVICE::MOUSE:
				assert(id < sizeof (mouse));
				return mouse[id];
			case RETRO_DEVICE::KEYBOARD:
				assert(id < sizeof (keyboard));
				return keyboard[id];
			case RETRO_DEVICE::LIGHTGUN:
				assert(id < sizeof (lightGun));
				return lightGun[id];
			case RETRO_DEVICE::ANALOG:
				assert(id < sizeof (analog));
				return analog[id];
			case RETRO_DEVICE::POINTER:
				assert(id < sizeof (pointer));
				return pointer[id];
			default:
				__builtin_unreachable();
		}
	}

	bool GetSupportsNoGame() {
		return supportsNoGame;
	}

	void GetRetroMessage(retro_message* m) {
		m->msg = retroMessageString.c_str();
		m->frames = retroMessageTime;

		if (retroMessageTime > 0) {
			retroMessageTime--;
		}
	}

	// need some way to communicate with retro variables later

	void SetDirectories(const char* systemDirectory, const char* saveDirectory, const char* coreDirectory, const char* coreAssetsDirectory) {
		this->systemDirectory = systemDirectory;
		this->saveDirectory = saveDirectory;
		this->coreDirectory = coreDirectory;
		this->coreAssetsDirectory = coreAssetsDirectory;
	}

	void SetVideoSize(u32 sz) {
		videoBuf.reset(new u32[sz]);
		videoBufSz = sz;
	}
	
	void GetVideo(u32* width, u32* height, u32* videoBuf) {
		*width = this->width;
		*height = this->height;
		std::memcpy(videoBuf, this->videoBuf.get(), videoBufSz * sizeof (u32));
	}

	u32 GetAudioSize() {
		return sampleBuf.size();
	}

	void GetAudio(u32* numSamples, s16* sampleBuf) {
		*numSamples = this->numSamples;
		std::memcpy(sampleBuf, this->sampleBuf.data(), this->sampleBuf.size() * sizeof (s16));

		this->numSamples = 0;
		this->sampleBuf.clear();
	}

	void SetInput(RETRO_DEVICE device, u32 port, s16* input) {
		switch (device) {
			case RETRO_DEVICE::NONE:
				break;
			case RETRO_DEVICE::JOYPAD:
				assert(port < 2);
				std::memcpy(joypads[port], input, sizeof (joypads[port]));
				break;
			case RETRO_DEVICE::MOUSE:
				std::memcpy(mouse, input, sizeof (mouse));
				break;
			case RETRO_DEVICE::KEYBOARD:
				std::memcpy(keyboard, input, sizeof (keyboard));
				break;
			case RETRO_DEVICE::LIGHTGUN:
				std::memcpy(lightGun, input, sizeof (lightGun));
				break;
			case RETRO_DEVICE::ANALOG:
				std::memcpy(analog, input, sizeof (analog));
				break;
			case RETRO_DEVICE::POINTER:
				std::memcpy(pointer, input, sizeof (pointer));
				break;
			default:
				__builtin_unreachable();
		}
	}

private:
	// environment vars
	bool supportsNoGame;

	std::string retroMessageString;
	u32 retroMessageTime;

	bool variablesDirty;
	u32 variableCount;
	std::unique_ptr<std::string[]> variableKeys;
	std::unique_ptr<std::string[]> variableComments;
	std::unique_ptr<std::string[]> variables;

	std::string systemDirectory;
	std::string saveDirectory;
	std::string coreDirectory;
	std::string coreAssetsDirectory;

	// video vars
	u32 rotation; // also an environ var
	RETRO_PIXEL_FORMAT pixelFormat; // also an environ var

	u32 width;
	u32 height;
	std::unique_ptr<u32[]> videoBuf;
	u32 videoBufSz;

	// audio vars
	u32 numSamples;
	std::vector<s16> sampleBuf;

	// input vars
	s16 joypads[2][static_cast<u32>(RETRO_DEVICE_ID_JOYPAD::LAST)];
	s16 mouse[static_cast<u32>(RETRO_DEVICE_ID_MOUSE::LAST)];
	s16 keyboard[static_cast<u32>(RETRO_KEY::LAST)];
	s16 lightGun[static_cast<u32>(RETRO_DEVICE_ID_LIGHTGUN::LAST)];
	s16 analog[static_cast<u32>(RETRO_DEVICE_ID_ANALOG::LAST)];
	s16 pointer[static_cast<u32>(RETRO_DEVICE_ID_POINTER::LAST)];
};

static CallbackHandler * gCbHandler = nullptr;

// make a callback handler
EXPORT CallbackHandler * LibretroBridge_CreateCallbackHandler() {
	return new CallbackHandler();
}

// destroy a callback handler
// this will clear the global callback handler if the passed pointer equals the global pointer
EXPORT void LibretroBridge_DestroyCallbackHandler(CallbackHandler* cbHandler) {
	if (cbHandler == gCbHandler) {
		gCbHandler = nullptr;
	}

	delete cbHandler;
}

// set a "global" callback handler
EXPORT void LibretroBridge_SetGlobalCallbackHandler(CallbackHandler* cbHandler) {
	gCbHandler = cbHandler;
}

// get whether the core has reported support for no game loaded
EXPORT bool LibretroBridge_GetSupportsNoGame(CallbackHandler* cbHandler) {
	return cbHandler->GetSupportsNoGame();
}

// get current retro_message set by the core
EXPORT void LibretroBridge_GetRetroMessage(CallbackHandler* cbHandler, retro_message* m) {
	cbHandler->GetRetroMessage(m);
}

// set directories for the core
EXPORT void LibretroBridge_SetDirectories(CallbackHandler* cbHandler, const char* systemDirectory, const char* saveDirectory, const char* coreDirectory, const char* coreAssetsDirectory) {
	cbHandler->SetDirectories(systemDirectory, saveDirectory, coreDirectory, coreAssetsDirectory);
}

// set size of video in pixels
// this should equal max_width * max_height
EXPORT void LibretroBridge_SetVideoSize(CallbackHandler* cbHandler, u32 sz) {
	cbHandler->SetVideoSize(sz);
}

// get video width/height and a copy of the video buffer
EXPORT void LibretroBridge_GetVideo(CallbackHandler* cbHandler, u32* width, u32* height, u32* videoBuf) {
	cbHandler->GetVideo(width, height, videoBuf);
}

// get size of audio in 16 bit units
// this should be used to allocate a buffer for retrieving the audio buffer
EXPORT u32 LibretroBridge_GetAudioSize(CallbackHandler* cbHandler) {
	return cbHandler->GetAudioSize();
}

// get number of stereo samples and a copy of the audio buffer
// calling this function will also reset the current audio buffer
EXPORT void LibretroBridge_GetAudio(CallbackHandler* cbHandler, u32* numSamples, s16* sampleBuf) {
	cbHandler->GetAudio(numSamples, sampleBuf);
}

// set input for specific device and port
// input is expected to be sent through an array of signed 16 bit integers, the positions of input in this array defined by RETRO_DEVICE_ID_* or RETRO_KEY
EXPORT void LibretroBridge_SetInput(CallbackHandler* cbHandler, RETRO_DEVICE device, u32 port, s16* input) {
	cbHandler->SetInput(device, port, input);
}

// retro callbacks

static boolean retro_environment(u32 cmd, void* data) {
	assert(gCbHandler);
	return gCbHandler->RetroEnvironment(cmd, data);
}

static void retro_video_refresh(const void* data, u32 width, u32 height, std::size_t pitch) {
	assert(gCbHandler);
	return gCbHandler->RetroVideoRefresh(data, width, height, pitch);
}

static void retro_audio_sample(s16 left, s16 right) {
	assert(gCbHandler);
	return gCbHandler->RetroAudioSample(left, right);
}

static std::size_t retro_audio_sample_batch(const s16* data, std::size_t frames) {
	assert(gCbHandler);
	return gCbHandler->RetroAudioSampleBatch(data, frames);
}

static void retro_input_poll() {
	assert(gCbHandler);
	return gCbHandler->RetroInputPoll();
}

static s16 retro_input_state(u32 port, u32 device, u32 index, u32 id) {
	assert(gCbHandler);
	return gCbHandler->RetroInputState(port, device, index, id);
}

struct RetroProcs {
	decltype(&retro_environment) retro_environment_proc;
	decltype(&retro_video_refresh) retro_video_refresh_proc;
	decltype(&retro_audio_sample) retro_audio_sample_proc;
	decltype(&retro_audio_sample_batch) retro_audio_sample_batch_proc;
	decltype(&retro_input_poll) retro_input_poll_proc;
	decltype(&retro_input_state) retro_input_state_proc;
};

// get procs for retro callbacks
// these are not linked to any specific callback handler
// please call LibretroBridge_SetCallbackHandler to set a global callback handler
// as you can guess, this means only a single instance can use this at a time :/
// (not like you can multi-instance libretro cores in the first place)
EXPORT void LibretroBridge_GetRetroProcs(RetroProcs* retroProcs) {
	retroProcs->retro_environment_proc = &retro_environment;
	retroProcs->retro_video_refresh_proc = &retro_video_refresh;
	retroProcs->retro_audio_sample_proc = &retro_audio_sample;
	retroProcs->retro_audio_sample_batch_proc = &retro_audio_sample_batch;
	retroProcs->retro_input_poll_proc = &retro_input_poll;
	retroProcs->retro_input_state_proc = &retro_input_state;
}

}
