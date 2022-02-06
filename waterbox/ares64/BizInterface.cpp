#include <n64/n64.hpp>

#include "../emulibc/emulibc.h"
#include "../emulibc/waterboxcore.h"

#define EXPORT extern "C" ECL_EXPORT

struct BizPlatform : ares::Platform {
	auto attach(ares::Node::Object) -> void override;
	auto detach(ares::Node::Object) -> void override;
	auto pak(ares::Node::Object) -> shared_pointer<vfs::directory> override;
	auto event(ares::Event) -> void override;
	auto log(string_view message) -> void override;
	auto video(ares::Node::Video::Screen, const u32* data, u32 pitch, u32 width, u32 height) -> void override;
	auto audio(ares::Node::Audio::Stream) -> void override;
	auto input(ares::Node::Input::Input) -> void override;
	
	//VFS::Pak pak;
};

auto BizPlatform::attach(ares::Node::Object) -> void {}
auto BizPlatform::detach(ares::Node::Object) -> void {}
auto BizPlatform::pak(ares::Node::Object) -> shared_pointer<vfs::directory> { return NULL; }
auto BizPlatform::event(ares::Event) -> void {}
auto BizPlatform::log(string_view) -> void {}
auto BizPlatform::video(ares::Node::Video::Screen, const u32*, u32, u32, u32) -> void {};
auto BizPlatform::audio(ares::Node::Audio::Stream) -> void {};
auto BizPlatform::input(ares::Node::Input::Input) -> void {};

static ares::Node::System root;
static BizPlatform platform;

EXPORT bool Init()
{
	ares::platform = &platform;

	if (!ares::Nintendo64::load(root, "[Nintendo] Nintendo 64 (NTSC)"))
	{
		return false;
	}

	if (auto port = root->find<ares::Node::Port>("Cartridge Slot"))
	{
		port->allocate();
		port->connect();
	}
	else
	{
		return false;
	}

	return true;
}

EXPORT void GetMemoryAreas(MemoryArea *m)
{
	
}

struct MyFrameInfo : public FrameInfo
{
	// input
};

EXPORT void FrameAdvance(MyFrameInfo* f)
{
	// handle input
	// frame advance
	// handle a/v and lag (somehow)
}

EXPORT void SetInputCallback(void (*callback)())
{
	
}
