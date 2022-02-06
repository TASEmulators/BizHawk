#include <n64/n64.hpp>

namespace ares::Nintendo64 {

auto enumerate() -> vector<string> {
  return {
    "[Nintendo] Nintendo 64 (NTSC)",
    "[Nintendo] Nintendo 64 (PAL)",
  };
}

auto load(Node::System& node, string name) -> bool {
  if(!enumerate().find(name)) return false;
  return system.load(node, name);
}

auto option(string name, string value) -> bool {
  #if defined(VULKAN)
  if(name == "Enable Vulkan") vulkan.enable = value.boolean();
  if(name == "Quality" && value == "SD" ) vulkan.internalUpscale = 1;
  if(name == "Quality" && value == "HD" ) vulkan.internalUpscale = 2;
  if(name == "Quality" && value == "UHD") vulkan.internalUpscale = 4;
  if(name == "Supersampling") vulkan.supersampleScanout = value.boolean();
  if(vulkan.internalUpscale == 1) vulkan.supersampleScanout = false;
  vulkan.outputUpscale = vulkan.supersampleScanout ? 1 : vulkan.internalUpscale;
  #endif
  return true;
}

System system;
Queue queue;
#include "serialization.cpp"

auto System::game() -> string {
  if(cartridge.node) {
    return cartridge.title();
  }

  return "(no cartridge connected)";
}

auto System::run() -> void {
  while(!vi.refreshed) cpu.main();
  vi.refreshed = false;
  si.run();
}

auto System::load(Node::System& root, string name) -> bool {
  if(node) unload();

  information = {};
  if(name.find("Nintendo 64")) {
    information.name = "Nintendo 64";
  }
  if(name.find("NTSC")) {
    information.region = Region::NTSC;
  }
  if(name.find("PAL")) {
    information.region = Region::PAL;
  }

  node = Node::System::create(information.name);
  node->setGame({&System::game, this});
  node->setRun({&System::run, this});
  node->setPower({&System::power, this});
  node->setSave({&System::save, this});
  node->setUnload({&System::unload, this});
  node->setSerialize({&System::serialize, this});
  node->setUnserialize({&System::unserialize, this});
  root = node;
  puts("setting node pak");
  if(!node->setPak(pak = platform->pak(node))) return false;
  puts("loading cart slot");
  cartridgeSlot.load(node);
  puts("loading port 1");
  controllerPort1.load(node);
  puts("loading port 2");
  controllerPort2.load(node);
  puts("loading port 3");
  controllerPort3.load(node);
  puts("loading port 4");
  controllerPort4.load(node);
  puts("loading rdram");
  rdram.load(node);
  puts("loading mi");
  mi.load(node);
  puts("loading vi");
  vi.load(node);
  puts("loading ai");
  ai.load(node);
  puts("loading pi");
  pi.load(node);
  puts("loading ri");
  ri.load(node);
  puts("loading si");
  si.load(node);
  puts("loading cpu");
  cpu.load(node);
  puts("loading rdp");
  rdp.load(node);
  puts("loading rsp");
  rsp.load(node);
  puts("loading dd");
  dd.load(node);
  puts("loading done");
  #if defined(VULKAN)
  vulkan.load(node);
  #endif
  return true;
}

auto System::unload() -> void {
  if(!node) return;
  save();
  #if defined(VULKAN)
  vulkan.unload();
  #endif
  cartridgeSlot.unload();
  controllerPort1.unload();
  controllerPort2.unload();
  controllerPort3.unload();
  controllerPort4.unload();
  rdram.unload();
  mi.unload();
  vi.unload();
  ai.unload();
  pi.unload();
  ri.unload();
  si.unload();
  cpu.unload();
  rdp.unload();
  rsp.unload();
  dd.unload();
  pak.reset();
  node.reset();
}

auto System::save() -> void {
  if(!node) return;
  cartridge.save();
  controllerPort1.save();
  controllerPort2.save();
  controllerPort3.save();
  controllerPort4.save();
}

auto System::power(bool reset) -> void {
  for(auto& setting : node->find<Node::Setting::Setting>()) setting->setLatch();

  if constexpr(Accuracy::CPU::Recompiler || Accuracy::RSP::Recompiler) {
    ares::Memory::FixedAllocator::get().release();
  }
  queue.reset();
  cartridge.power(reset);
  rdram.power(reset);
  dd.power(reset);
  mi.power(reset);
  vi.power(reset);
  ai.power(reset);
  pi.power(reset);
  ri.power(reset);
  si.power(reset);
  cpu.power(reset);
  rdp.power(reset);
  rsp.power(reset);
}

}
