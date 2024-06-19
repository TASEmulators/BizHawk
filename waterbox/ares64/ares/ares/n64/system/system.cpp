#include <n64/n64.hpp>

#include <nall/gdb/server.hpp>

namespace ares::Nintendo64 {

auto enumerate() -> vector<string> {
  return {
    "[Nintendo] Nintendo 64 (NTSC)",
    "[Nintendo] Nintendo 64 (PAL)",
    "[Nintendo] Nintendo 64DD (NTSC-U)",
    "[Nintendo] Nintendo 64DD (NTSC-J)",
    "[Nintendo] Nintendo 64DD (NTSC-DEV)",
  };
}

auto load(Node::System& node, string name) -> bool {
  if(!enumerate().find(name)) return false;
  return system.load(node, name);
}

auto option(string name, string value) -> bool {
  #if defined(VULKAN)
  if(name == "Enable GPU acceleration") vulkan.enable = value.boolean();
  if(name == "Quality" && value == "SD" ) vulkan.internalUpscale = 1;
  if(name == "Quality" && value == "HD" ) vulkan.internalUpscale = 2;
  if(name == "Quality" && value == "UHD") vulkan.internalUpscale = 4;
  if(name == "Supersampling") vulkan.supersampleScanout = value.boolean();
  if(name == "Disable Video Interface Processing") vulkan.disableVideoInterfaceProcessing = value.boolean();
  if(name == "Weave Deinterlacing") vulkan.weaveDeinterlacing = value.boolean();
  if(vulkan.internalUpscale == 1) vulkan.supersampleScanout = false;
  vulkan.outputUpscale = vulkan.supersampleScanout ? 1 : vulkan.internalUpscale;
  #endif
  if(name == "Homebrew Mode") system.homebrewMode = value.boolean();
  if(name == "Expansion Pak") system.expansionPak = value.boolean();
  return true;
}

System system;
Queue queue;
#include "serialization.cpp"

auto System::game() -> string {
  if(dd.node && !cartridge.node) {
    return dd.title();
  }

  if(cartridge.node) {
    return cartridge.title();
  }

  return "(no cartridge connected)";
}

auto System::run() -> void {
  cpu.main();
}

auto System::load(Node::System& root, string name) -> bool {
  if(node) unload();

  information = {};
  if(name.match("[Nintendo] Nintendo 64 (*)")) {
    information.name = "Nintendo 64";
    information.dd = 0;
  }
  if(name.match("[Nintendo] Nintendo 64DD (*)")) {
    information.name = "Nintendo 64";
    information.dd = 1;
  }

  if(name.find("NTSC")) {
    information.region = Region::NTSC;
    information.videoFrequency = 48'681'812;
  }
  if(name.find("PAL")) {
    information.region = Region::PAL;
    information.videoFrequency = 49'656'530;
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
  if(!node->setPak(pak = platform->pak(node))) return false;

  cartridgeSlot.load(node);
  controllerPort1.load(node);
  controllerPort2.load(node);
  controllerPort3.load(node);
  controllerPort4.load(node);
  rdram.load(node);
  mi.load(node);
  vi.load(node);
  ai.load(node);
  pi.load(node);
  pif.load(node);
  ri.load(node);
  si.load(node);
  cpu.load(node);
  rsp.load(node);
  rdp.load(node);
  if(_DD()) dd.load(node);
  #if defined(VULKAN)
  vulkan.load(node);
  #endif
  return true;
}

auto System::unload() -> void {
  if(!node) return;
  save();
  if(vi.screen) vi.screen->quit(); //stop video thread
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
  pif.unload();
  ri.unload();
  si.unload();
  cpu.unload();
  rsp.unload();
  rdp.unload();
  if(_DD()) dd.unload();
  pak.reset();
  node.reset();
}

auto System::save() -> void {
#if false
  if(!node) return;
  cartridge.save();
  controllerPort1.save();
  controllerPort2.save();
  controllerPort3.save();
  controllerPort4.save();
  if(_DD()) dd.save();
#endif
}

auto System::power(bool reset) -> void {
  for(auto& setting : node->find<Node::Setting::Setting>()) setting->setLatch();

  if constexpr(Accuracy::CPU::Recompiler || Accuracy::RSP::Recompiler) {
    ares::Memory::FixedAllocator::get().release();
  }
  queue.reset();
  cartridge.power(reset);
  rdram.power(reset);
  if(_DD()) dd.power(reset);
  mi.power(reset);
  vi.power(reset);
  ai.power(reset);
  pi.power(reset);
  pif.power(reset);
  cic.power(reset);
  ri.power(reset);
  si.power(reset);
  cpu.power(reset);
  rsp.power(reset);
  rdp.power(reset);
}

}
