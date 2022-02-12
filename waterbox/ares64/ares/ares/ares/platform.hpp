#pragma once

namespace ares {

enum class Event : u32 {
  None,
  Step,
  Frame,
  Power,
  Synchronize,
};

struct Platform {
  virtual auto attach(Node::Object) -> void {}
  virtual auto detach(Node::Object) -> void {}
  virtual auto pak(Node::Object) -> shared_pointer<vfs::directory> { return {}; }
  virtual auto event(Event) -> void {}
  virtual auto log(string_view message) -> void {}
  virtual auto video(Node::Video::Screen, const u32* data, u32 pitch, u32 width, u32 height) -> void {}
  virtual auto audio(Node::Audio::Stream) -> void {}
  virtual auto input(Node::Input::Input) -> void {}
};

extern Platform* platform;

}

namespace ares::Core {
  // <ares/node/node.hpp> forward declarations
  auto PlatformAttach(Node::Object node) -> void { if(platform && node->name()) platform->attach(node); }
  auto PlatformDetach(Node::Object node) -> void { if(platform && node->name()) platform->detach(node); }
  auto PlatformLog(string_view text) -> void { if(platform) platform->log(text); }
}
