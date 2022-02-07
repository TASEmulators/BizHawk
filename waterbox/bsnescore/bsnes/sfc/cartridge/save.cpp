auto Cartridge::saveCartridge(Markup::Node node) -> void {
  if(auto node = board["memory(type=RAM,content=Save)"]) saveRAM(node);
  if(auto node = board["processor(identifier=MCC)"]) saveMCC(node);
  if(auto node = board["processor(architecture=W65C816S)"]) saveSA1(node);
  if(auto node = board["processor(architecture=GSU)"]) saveSuperFX(node);
  if(auto node = board["processor(architecture=ARM6)"]) saveARMDSP(node);
  if(auto node = board["processor(architecture=HG51BS169)"]) saveHitachiDSP(node);
  if(auto node = board["processor(architecture=uPD7725)"]) saveuPD7725(node);
  if(auto node = board["processor(architecture=uPD96050)"]) saveuPD96050(node);
  if(auto node = board["rtc(manufacturer=Epson)"]) saveEpsonRTC(node);
  if(auto node = board["rtc(manufacturer=Sharp)"]) saveSharpRTC(node);
  if(auto node = board["processor(identifier=SPC7110)"]) saveSPC7110(node);
  if(auto node = board["processor(identifier=OBC1)"]) saveOBC1(node);
}

auto Cartridge::saveCartridgeBSMemory(Markup::Node node) -> void {
  if(auto memory = Emulator::Game::Memory{node["game/board/memory(type=Flash,content=Program)"]}) {
    if(auto fp = platform->open(bsmemory.pathID, memory.name(), File::Write)) {
      fp->write(bsmemory.memory.data(), memory.size);
    }
  }
}

auto Cartridge::saveCartridgeSufamiTurboA(Markup::Node node) -> void {
  if(auto memory = Emulator::Game::Memory{node["game/board/memory(type=RAM,content=Save)"]}) {
    if(memory.nonVolatile) {
      if(auto fp = platform->open(sufamiturboA.pathID, memory.name(), File::Write)) {
        fp->write(sufamiturboA.ram.data(), memory.size);
      }
    }
  }
}

auto Cartridge::saveCartridgeSufamiTurboB(Markup::Node node) -> void {
  if(auto memory = Emulator::Game::Memory{node["game/board/memory(type=RAM,content=Save)"]}) {
    if(memory.nonVolatile) {
      if(auto fp = platform->open(sufamiturboB.pathID, memory.name(), File::Write)) {
        fp->write(sufamiturboB.ram.data(), memory.size);
      }
    }
  }
}

//

auto Cartridge::saveMemory(Memory& ram, Markup::Node node) -> void {
  if(auto memory = game.memory(node)) {
    if(memory->type == "RAM" && !memory->nonVolatile) return;
    if(memory->type == "RTC" && !memory->nonVolatile) return;
    if(auto fp = platform->open(pathID(), memory->name(), File::Write)) {
      fp->write(ram.data(), ram.size());
    }
  }
}

//memory(type=RAM,content=Save)
auto Cartridge::saveRAM(Markup::Node node) -> void {
  saveMemory(ram, node);
}

//processor(identifier=MCC)
auto Cartridge::saveMCC(Markup::Node node) -> void {
  if(auto mcu = node["mcu"]) {
    if(auto memory = mcu["memory(type=RAM,content=Download)"]) {
      saveMemory(mcc.psram, memory);
    }
  }
}

//processor(architecture=W65C816S)
auto Cartridge::saveSA1(Markup::Node node) -> void {
  if(auto memory = node["memory(type=RAM,content=Save)"]) {
    saveMemory(sa1.bwram, memory);
  }

  if(auto memory = node["memory(type=RAM,content=Internal)"]) {
    saveMemory(sa1.iram, memory);
  }
}

//processor(architecture=GSU)
auto Cartridge::saveSuperFX(Markup::Node node) -> void {
  if(auto memory = node["memory(type=RAM,content=Save)"]) {
    saveMemory(superfx.ram, memory);
  }
}

//processor(architecture=ARM6)
auto Cartridge::saveARMDSP(Markup::Node node) -> void {
  if(auto memory = node["memory(type=RAM,content=Data,architecture=ARM6)"]) {
    if(auto file = game.memory(memory)) {
      if(file->nonVolatile) {
        if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Write)) {
          for(auto n : range(16 * 1024)) fp->write(armdsp.programRAM[n]);
        }
      }
    }
  }
}

//processor(architecture=HG51BS169)
auto Cartridge::saveHitachiDSP(Markup::Node node) -> void {
  saveMemory(hitachidsp.ram, node["ram"]);

  if(auto memory = node["memory(type=RAM,content=Save)"]) {
    saveMemory(hitachidsp.ram, memory);
  }

  if(auto memory = node["memory(type=RAM,content=Data,architecture=HG51BS169)"]) {
    if(auto file = game.memory(memory)) {
      if(file->nonVolatile) {
        if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Write)) {
          for(auto n : range(3 * 1024)) fp->write(hitachidsp.dataRAM[n]);
        }
      }
    }
  }
}

//processor(architecture=uPD7725)
auto Cartridge::saveuPD7725(Markup::Node node) -> void {
  if(auto memory = node["memory(type=RAM,content=Data,architecture=uPD7725)"]) {
    if(auto file = game.memory(memory)) {
      if(file->nonVolatile) {
        if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Write)) {
          for(auto n : range(256)) fp->writel(necdsp.dataRAM[n], 2);
        }
      }
    }
  }
}

//processor(architecture=uPD96050)
auto Cartridge::saveuPD96050(Markup::Node node) -> void {
  if(auto memory = node["memory(type=RAM,content=Data,architecture=uPD96050)"]) {
    if(auto file = game.memory(memory)) {
      if(file->nonVolatile) {
        if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Write)) {
          for(auto n : range(2 * 1024)) fp->writel(necdsp.dataRAM[n], 2);
        }
      }
    }
  }
}

//rtc(manufacturer=Epson)
auto Cartridge::saveEpsonRTC(Markup::Node node) -> void {
  if(auto memory = node["memory(type=RTC,content=Time,manufacturer=Epson)"]) {
    if(auto file = game.memory(memory)) {
      if(file->nonVolatile) {
        if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Write)) {
          uint8 data[16] = {0};
          epsonrtc.save(data);
          fp->write(data, 16);
        }
      }
    }
  }
}

//rtc(manufacturer=Sharp)
auto Cartridge::saveSharpRTC(Markup::Node node) -> void {
  if(auto memory = node["memory(type=RTC,content=Time,manufacturer=Sharp)"]) {
    if(auto file = game.memory(memory)) {
      if(file->nonVolatile) {
        if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Write)) {
          uint8 data[16] = {0};
          sharprtc.save(data);
          fp->write(data, 16);
        }
      }
    }
  }
}

//processor(identifier=SPC7110)
auto Cartridge::saveSPC7110(Markup::Node node) -> void {
  if(auto memory = node["memory(type=RAM,content=Save)"]) {
    saveMemory(spc7110.ram, memory);
  }
}

//processor(identifier=OBC1)
auto Cartridge::saveOBC1(Markup::Node node) -> void {
  if(auto memory = node["memory(type=RAM,content=Save)"]) {
    saveMemory(obc1.ram, memory);
  }
}
