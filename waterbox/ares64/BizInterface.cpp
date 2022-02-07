#include <n64/n64.hpp>

#include "../emulibc/emulibc.h"
#include "../emulibc/waterboxcore.h"

#define EXPORT extern "C" ECL_EXPORT

struct BizPlatform : ares::Platform {
	auto attach(ares::Node::Object) -> void override;
	auto pak(ares::Node::Object) -> shared_pointer<vfs::directory> override;
	auto video(ares::Node::Video::Screen, const u32* data, u32 pitch, u32 width, u32 height) -> void override;
	auto input(ares::Node::Input::Input) -> void override;

	shared_pointer<vfs::directory> bizpak = new vfs::directory;
	ares::Node::Audio::Stream stream;
	u32* videobuf = nullptr;
	u32 pitch = 0;
	u32 width = 0;
	u32 height = 0;
};

auto BizPlatform::attach(ares::Node::Object node) -> void {
	if (auto stream = node->cast<ares::Node::Audio::Stream>()) {
		stream->setResamplerFrequency(44100);
		this->stream = stream;
	}
}
auto BizPlatform::pak(ares::Node::Object) -> shared_pointer<vfs::directory> { return bizpak; }
auto BizPlatform::video(ares::Node::Video::Screen, const u32* data, u32 pitch, u32 width, u32 height) -> void {
	videobuf = (u32*)data;
	this->pitch = pitch;
	this->width = width;
	this->height = height;
}
auto BizPlatform::input(ares::Node::Input::Input) -> void {};

static ares::Node::System root;
static BizPlatform platform;

EXPORT bool Init(bool pal)
{
	FILE* f;
	array_view<u8>* data;
	u32 len;
	string name;

	name = pal ? "pif.pal.rom" : "pif.ntsc.rom";
	f = fopen(name, "rb");
	fseek(f, 0, SEEK_END);
	len = ftell(f);
	data = new array_view<u8>(new u8[len], len);
	fseek(f, 0, SEEK_SET);
	fread((void*)data->data(), 1, len, f);
	fclose(f);
	platform.bizpak->append(name, *data);

	name = "program.rom";
	f = fopen(name, "rb");
	fseek(f, 0, SEEK_END);
	len = ftell(f);
	data = new array_view<u8>(new u8[len], len);
	fseek(f, 0, SEEK_SET);
	fread((void*)data->data(), 1, len, f);
	fclose(f);
	platform.bizpak->append(name, *data);

	string region = pal ? "PAL" : "NTSC";
	platform.bizpak->setAttribute("region", region);

	ares::platform = &platform;

	if (!ares::Nintendo64::load(root, {"[Nintendo] Nintendo 64 (", region, ")"}))
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

	root->power();
	root->run();
	root->run();
	f64 buf[2];
	while (platform.stream->pending()) platform.stream->read(buf);
	return true;
}

u8 dummy[1];

EXPORT void GetMemoryAreas(MemoryArea *m)
{
	m[0].Data = dummy;
	m[0].Name = "Dummy";
	m[0].Size = 1;
	m[0].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_PRIMARY;
}

struct MyFrameInfo : public FrameInfo
{
	// input
};

EXPORT void FrameAdvance(MyFrameInfo* f)
{
	// handle input
	root->run();
	f->Width = platform.width;
	f->Height = platform.height;
	u32 pitch = platform.pitch;
	u32* src = platform.videobuf;
	u32* dst = f->VideoBuffer;
	for (int i = 0; i < f->Height; i++)
	{
		memcpy(dst, src, f->Width * 4);
		src += pitch;
		dst += 640;
	}
	s16* soundbuf = f->SoundBuffer;
	while (platform.stream->pending())
	{
		f64 buf[2] = { 0.0, 0.0 };
		platform.stream->read(buf);
		*soundbuf++ = (s16)std::clamp(buf[0] * 32768, -32768.0, 32767.0);
		*soundbuf++ = (s16)std::clamp(buf[1] * 32768, -32768.0, 32767.0);
		f->Samples++;
	}
	// handle a/v and lag (somehow)
}

EXPORT void SetInputCallback(void (*callback)())
{
	
}
