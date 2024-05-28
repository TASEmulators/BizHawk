#include <n64/n64.hpp>

namespace ares::Nintendo64 {

DD dd;
#include "controller.cpp"
#include "drive.cpp"
#include "rtc.cpp"
#include "io.cpp"
#include "debugger.cpp"
#include "serialization.cpp"

auto DD::load(Node::Object parent) -> void {
  obj = parent->append<Node::Object>("Nintendo 64DD");
  port = obj->append<Node::Port>("Disk Drive");
  port->setFamily("Nintendo 64DD");
  port->setType("Floppy Disk");
  port->setHotSwappable(true);
  port->setAllocate([&](auto name) { return allocate(port); });
  port->setConnect([&] { return connect(); });
  port->setDisconnect([&] { return disconnect(); });

  iplrom.allocate(4_MiB);
  c2s.allocate(0x400);
  ds.allocate(0x100);
  ms.allocate(0x40);

  // TODO: Detect correct CIC from ipl rom
  if(auto fp = system.pak->read("64dd.ipl.rom")) {
    iplrom.load(fp);
  }

  rtc.load();

  debugger.load(parent->append<Node::Object>("Nintendo 64DD"));
}

auto DD::unload() -> void {
  if(!node) return;
  disconnect();

  debugger = {};
  iplrom.reset();
  c2s.reset();
  ds.reset();
  ms.reset();
  rtc.reset();
  disk.reset();
  port.reset();
  node.reset();
  obj.reset();
}

auto DD::allocate(Node::Port parent) -> Node::Peripheral {
  return node = parent->append<Node::Peripheral>("Nintendo 64DD Disk");
}

auto DD::connect() -> void {
  if(!node->setPak(pak = platform->pak(node))) return;

  information = {};
  information.title = pak->attribute("title");

  if(iplrom) {
    string id;
    id.append((char)iplrom.read<Byte>(0x3b));
    id.append((char)iplrom.read<Byte>(0x3c));
    id.append((char)iplrom.read<Byte>(0x3d));
    id.append((char)iplrom.read<Byte>(0x3e));
    if(id.match("NDDJ")) dd.information.cic = "CIC-NUS-8303";
    if(id.match("NDDE")) dd.information.cic = "CIC-NUS-DDUS";
    if(id.match("NDXJ")) dd.information.cic = "CIC-NUS-8401";
  }

  if(auto fp = pak->read("program.disk.error")) {
    error.allocate(fp->size());
    error.load(fp);
  }

  if(auto fp = pak->read("program.disk")) {
    disk.allocate(fp->size());
    disk.load(fp);
    io.status.diskChanged = 1;
    io.status.diskPresent = 1;
  }
}

auto DD::disconnect() -> void {
  if(!port) return;

  save();
  pak.reset();
  disk.reset();
  information = {};

  if(iplrom) {
    string id;
    id.append((char)iplrom.read<Byte>(0x3b));
    id.append((char)iplrom.read<Byte>(0x3c));
    id.append((char)iplrom.read<Byte>(0x3d));
    id.append((char)iplrom.read<Byte>(0x3e));
    if(id.match("NDDJ")) dd.information.cic = "CIC-NUS-8303";
    if(id.match("NDDE")) dd.information.cic = "CIC-NUS-DDUS";
    if(id.match("NDXJ")) dd.information.cic = "CIC-NUS-8401";
  }

  io.status.diskPresent = 0;

  //Deal with cases when the disk is removed while in use
  if(io.status.busyState) {
    //MECHA
    io.status.mechaError = 1;
  }

  if(io.bm.start) {
    //BM
    io.bm.start = 0;
    io.bm.error = 1;
  }
  motorStop();
}

auto DD::save() -> void {
#if false
  if(disk)
  if(auto fp = pak->write("program.disk")) {
    disk.save(fp);
  }
#endif
  
  rtc.save();
}

auto DD::power(bool reset) -> void {
  c2s.fill();
  ds.fill();
  ms.fill();

  irq = {};
  ctl = {};
  io = {};
  state = {};

  io.status.resetState = 1;
  io.status.diskChanged = 1;
  if(disk) io.status.diskPresent = 1;
  
  io.id = 3;
  if(dd.information.cic.match("CIC-NUS-8401")) io.id = 4;
  
  motorStop();

  queue.insert(Queue::DD_Clock_Tick, 187'500'000);
  queue.remove(Queue::DD_MECHA_Response);
  queue.remove(Queue::DD_BM_Request);
  lower(IRQ::MECHA);
  lower(IRQ::BM);
}

auto DD::raise(IRQ source) -> void {
  debugger.interrupt((u32)source);
  switch(source) {
  case IRQ::MECHA: irq.mecha.line = 1; break;
  case IRQ::BM: irq.bm.line = 1; break;
  }
  poll();
}

auto DD::lower(IRQ source) -> void {
  switch(source) {
  case IRQ::MECHA: irq.mecha.line = 0; break;
  case IRQ::BM: irq.bm.line = 0; break;
  }
  poll();
}

auto DD::poll() -> void {
  bool line = 0;
  line |= irq.mecha.line & irq.mecha.mask;
  line |= irq.bm.line & irq.bm.mask;
  cpu.scc.cause.interruptPending.bit(3) = line;
}

}
