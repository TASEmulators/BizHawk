auto Cartridge::loadBoard(string board) -> Markup::Node {
  string output;

  if(board.beginsWith("SNSP-")) board.replace("SNSP-", "SHVC-", 1L);
  if(board.beginsWith("MAXI-")) board.replace("MAXI-", "SHVC-", 1L);
  if(board.beginsWith("MJSC-")) board.replace("MJSC-", "SHVC-", 1L);
  if(board.beginsWith("EA-"  )) board.replace("EA-",   "SHVC-", 1L);
  if(board.beginsWith("WEI-" )) board.replace("WEI-",  "SHVC-", 1L);

  if(auto fp = platform->open(ID::System, "boards.bml", File::Read, File::Required)) {
    auto document = BML::unserialize(fp->reads());
    for(auto leaf : document.find("board")) {
      auto id = leaf.text();
      bool matched = id == board;
      if(!matched && id.match("*(*)*")) {
        auto part = id.transform("()", "||").split("|");
        for(auto& revision : part(1).split(",")) {
          if(string{part(0), revision, part(2)} == board) matched = true;
        }
      }
      if(matched) return leaf;
    }
  }

  return {};
}

auto Cartridge::loadCartridge(Markup::Node node) -> void {
  board = node["board"];
  if(!board) board = loadBoard(game.board);

  if(region() == "Auto") {
    auto region = game.region;
    if(region.endsWith("BRA")
    || region.endsWith("CAN")
    || region.endsWith("HKG")
    || region.endsWith("JPN")
    || region.endsWith("KOR")
    || region.endsWith("LTN")
    || region.endsWith("ROC")
    || region.endsWith("USA")
    || region.beginsWith("SHVC-")
    || region == "NTSC") {
      information.region = "NTSC";
    } else {
      information.region = "PAL";
    }
  }

  if(auto node = board["memory(type=ROM,content=Program)"]) loadROM(node);
  if(auto node = board["memory(type=ROM,content=Expansion)"]) loadROM(node);  //todo: handle this better
  if(auto node = board["memory(type=RAM,content=Save)"]) loadRAM(node);
  if(auto node = board["processor(identifier=ICD)"]) loadICD(node);
  if(auto node = board["processor(identifier=MCC)"]) loadMCC(node);
  if(auto node = board["slot(type=BSMemory)"]) loadBSMemory(node);
  if(auto node = board["slot(type=SufamiTurbo)[0]"]) loadSufamiTurboA(node);
  if(auto node = board["slot(type=SufamiTurbo)[1]"]) loadSufamiTurboB(node);
  if(auto node = board["dip"]) loadDIP(node);
  if(auto node = board["processor(architecture=uPD78214)"]) loadEvent(node);
  if(auto node = board["processor(architecture=W65C816S)"]) loadSA1(node);
  if(auto node = board["processor(architecture=GSU)"]) loadSuperFX(node);
  if(auto node = board["processor(architecture=ARM6)"]) loadARMDSP(node);
  if(auto node = board["processor(architecture=HG51BS169)"]) loadHitachiDSP(node, game.board.match("2DC*") ? 2 : 1);
  if(auto node = board["processor(architecture=uPD7725)"]) loaduPD7725(node);
  if(auto node = board["processor(architecture=uPD96050)"]) loaduPD96050(node);
  if(auto node = board["rtc(manufacturer=Epson)"]) loadEpsonRTC(node);
  if(auto node = board["rtc(manufacturer=Sharp)"]) loadSharpRTC(node);
  if(auto node = board["processor(identifier=SPC7110)"]) loadSPC7110(node);
  if(auto node = board["processor(identifier=SDD1)"]) loadSDD1(node);
  if(auto node = board["processor(identifier=OBC1)"]) loadOBC1(node);

  if(auto fp = platform->open(pathID(), "msu1/data.rom", File::Read)) loadMSU1();
}

auto Cartridge::loadCartridgeBSMemory(Markup::Node node) -> void {
  if(auto memory = Emulator::Game::Memory{node["game/board/memory(content=Program)"]}) {
    bsmemory.ROM = memory.type == "ROM";
    bsmemory.memory.allocate(memory.size);
    if(auto fp = platform->open(bsmemory.pathID, memory.name(), File::Read, File::Required)) {
      fp->read(bsmemory.memory.data(), memory.size);
    }
  }
}

auto Cartridge::loadCartridgeSufamiTurboA(Markup::Node node) -> void {
  if(auto memory = Emulator::Game::Memory{node["game/board/memory(type=ROM,content=Program)"]}) {
    sufamiturboA.rom.allocate(memory.size);
    if(auto fp = platform->open(sufamiturboA.pathID, memory.name(), File::Read, File::Required)) {
      fp->read(sufamiturboA.rom.data(), memory.size);
    }
  }

  if(auto memory = Emulator::Game::Memory{node["game/board/memory(type=RAM,content=Save)"]}) {
    sufamiturboA.ram.allocate(memory.size);
    if(auto fp = platform->open(sufamiturboA.pathID, memory.name(), File::Read)) {
      fp->read(sufamiturboA.ram.data(), memory.size);
    }
  }
}

auto Cartridge::loadCartridgeSufamiTurboB(Markup::Node node) -> void {
  if(auto memory = Emulator::Game::Memory{node["game/board/memory(type=ROM,content=Program)"]}) {
    sufamiturboB.rom.allocate(memory.size);
    if(auto fp = platform->open(sufamiturboB.pathID, memory.name(), File::Read, File::Required)) {
      fp->read(sufamiturboB.rom.data(), memory.size);
    }
  }

  if(auto memory = Emulator::Game::Memory{node["game/board/memory(type=RAM,content=Save)"]}) {
    sufamiturboB.ram.allocate(memory.size);
    if(auto fp = platform->open(sufamiturboB.pathID, memory.name(), File::Read)) {
      fp->read(sufamiturboB.ram.data(), memory.size);
    }
  }
}

//

auto Cartridge::loadMemory(Memory& ram, Markup::Node node, bool required) -> void {
  if(auto memory = game.memory(node)) {
    ram.allocate(memory->size);
    if(memory->type == "RAM" && !memory->nonVolatile) return;
    if(memory->type == "RTC" && !memory->nonVolatile) return;
    if(auto fp = platform->open(pathID(), memory->name(), File::Read, required)) {
      fp->read(ram.data(), min(fp->size(), ram.size()));
    }
  }
}

template<typename T>  //T = ReadableMemory, WritableMemory, ProtectableMemory
auto Cartridge::loadMap(Markup::Node map, T& memory) -> uint {
  auto addr = map["address"].text();
  auto size = map["size"].natural();
  auto base = map["base"].natural();
  auto mask = map["mask"].natural();
  if(size == 0) size = memory.size();
  if(size == 0) return print("loadMap(): size=0\n"), 0;  //does this ever actually occur?
  return bus.map({&T::read, &memory}, {&T::write, &memory}, addr, size, base, mask);
}

auto Cartridge::loadMap(
  Markup::Node map,
  const function<uint8 (uint, uint8)>& reader,
  const function<void  (uint, uint8)>& writer
) -> uint {
  auto addr = map["address"].text();
  auto size = map["size"].natural();
  auto base = map["base"].natural();
  auto mask = map["mask"].natural();
  return bus.map(reader, writer, addr, size, base, mask);
}

//memory(type=ROM,content=Program)
auto Cartridge::loadROM(Markup::Node node) -> void {
  loadMemory(rom, node, File::Required);
  for(auto leaf : node.find("map")) loadMap(leaf, rom);
}

//memory(type=RAM,content=Save)
auto Cartridge::loadRAM(Markup::Node node) -> void {
  loadMemory(ram, node, File::Optional);
  for(auto leaf : node.find("map")) loadMap(leaf, ram);
}

//processor(identifier=ICD)
auto Cartridge::loadICD(Markup::Node node) -> void {
  has.GameBoySlot = true;
  has.ICD = true;

  icd.Revision = node["revision"].natural();
  if(auto oscillator = game.oscillator()) {
    icd.Frequency = oscillator->frequency;
  } else {
    icd.Frequency = 0;
  }

  //Game Boy core loads data through ICD interface
  for(auto map : node.find("map")) {
    loadMap(map, {&ICD::readIO, &icd}, {&ICD::writeIO, &icd});
  }
}

//processor(identifier=MCC)
auto Cartridge::loadMCC(Markup::Node node) -> void {
  has.MCC = true;

  for(auto map : node.find("map")) {
    loadMap(map, {&MCC::read, &mcc}, {&MCC::write, &mcc});
  }

  if(auto mcu = node["mcu"]) {
    for(auto map : mcu.find("map")) {
      loadMap(map, {&MCC::mcuRead, &mcc}, {&MCC::mcuWrite, &mcc});
    }
    if(auto memory = mcu["memory(type=ROM,content=Program)"]) {
      loadMemory(mcc.rom, memory, File::Required);
    }
    if(auto memory = mcu["memory(type=RAM,content=Download)"]) {
      loadMemory(mcc.psram, memory, File::Optional);
    }
    if(auto slot = mcu["slot(type=BSMemory)"]) {
      loadBSMemory(slot);
    }
  }
}

//slot(type=BSMemory)
auto Cartridge::loadBSMemory(Markup::Node node) -> void {
  has.BSMemorySlot = true;

  if(auto loaded = platform->load(ID::BSMemory, "BS Memory", "bs")) {
    bsmemory.pathID = loaded.pathID;
    loadBSMemory();

    for(auto map : node.find("map")) {
      loadMap(map, bsmemory);
    }
  }
}

//slot(type=SufamiTurbo)[0]
auto Cartridge::loadSufamiTurboA(Markup::Node node) -> void {
  has.SufamiTurboSlotA = true;

  if(auto loaded = platform->load(ID::SufamiTurboA, "Sufami Turbo", "st")) {
    sufamiturboA.pathID = loaded.pathID;
    loadSufamiTurboA();

    for(auto map : node.find("rom/map")) {
      loadMap(map, sufamiturboA.rom);
    }

    for(auto map : node.find("ram/map")) {
      loadMap(map, sufamiturboA.ram);
    }
  }
}

//slot(type=SufamiTurbo)[1]
auto Cartridge::loadSufamiTurboB(Markup::Node node) -> void {
  has.SufamiTurboSlotB = true;

  if(auto loaded = platform->load(ID::SufamiTurboB, "Sufami Turbo", "st")) {
    sufamiturboB.pathID = loaded.pathID;
    loadSufamiTurboB();

    for(auto map : node.find("rom/map")) {
      loadMap(map, sufamiturboB.rom);
    }

    for(auto map : node.find("ram/map")) {
      loadMap(map, sufamiturboB.ram);
    }
  }
}

//dip
auto Cartridge::loadDIP(Markup::Node node) -> void {
  has.DIP = true;
  dip.value = platform->dipSettings(node);

  for(auto map : node.find("map")) {
    loadMap(map, {&DIP::read, &dip}, {&DIP::write, &dip});
  }
}

//processor(architecture=uPD78214)
auto Cartridge::loadEvent(Markup::Node node) -> void {
  has.Event = true;
  event.board = Event::Board::Unknown;
  if(node["identifier"].text() == "Campus Challenge '92") event.board = Event::Board::CampusChallenge92;
  if(node["identifier"].text() == "PowerFest '94") event.board = Event::Board::PowerFest94;

  for(auto map : node.find("map")) {
    loadMap(map, {&Event::read, &event}, {&Event::write, &event});
  }

  if(auto mcu = node["mcu"]) {
    for(auto map : mcu.find("map")) {
      loadMap(map, {&Event::mcuRead, &event}, {&Event::mcuWrite, &event});
    }
    if(auto memory = mcu["memory(type=ROM,content=Program)"]) {
      loadMemory(event.rom[0], memory, File::Required);
    }
    if(auto memory = mcu["memory(type=ROM,content=Level-1)"]) {
      loadMemory(event.rom[1], memory, File::Required);
    }
    if(auto memory = mcu["memory(type=ROM,content=Level-2)"]) {
      loadMemory(event.rom[2], memory, File::Required);
    }
    if(auto memory = mcu["memory(type=ROM,content=Level-3)"]) {
      loadMemory(event.rom[3], memory, File::Required);
    }
  }
}

//processor(architecture=W65C816S)
auto Cartridge::loadSA1(Markup::Node node) -> void {
  has.SA1 = true;

  for(auto map : node.find("map")) {
    loadMap(map, {&SA1::readIOCPU, &sa1}, {&SA1::writeIOCPU, &sa1});
  }

  if(auto mcu = node["mcu"]) {
    for(auto map : mcu.find("map")) {
      loadMap(map, {&SA1::ROM::readCPU, &sa1.rom}, {&SA1::ROM::writeCPU, &sa1.rom});
    }
    if(auto memory = mcu["memory(type=ROM,content=Program)"]) {
      loadMemory(sa1.rom, memory, File::Required);
    }
    if(auto slot = mcu["slot(type=BSMemory)"]) {
      loadBSMemory(slot);
    }
  }

  if(auto memory = node["memory(type=RAM,content=Save)"]) {
    loadMemory(sa1.bwram, memory, File::Optional);
    for(auto map : memory.find("map")) {
      loadMap(map, {&SA1::BWRAM::readCPU, &sa1.bwram}, {&SA1::BWRAM::writeCPU, &sa1.bwram});
    }
  }

  if(auto memory = node["memory(type=RAM,content=Internal)"]) {
    loadMemory(sa1.iram, memory, File::Optional);
    for(auto map : memory.find("map")) {
      loadMap(map, {&SA1::IRAM::readCPU, &sa1.iram}, {&SA1::IRAM::writeCPU, &sa1.iram});
    }
  }
}

//processor(architecture=GSU)
auto Cartridge::loadSuperFX(Markup::Node node) -> void {
  has.SuperFX = true;

  if(auto oscillator = game.oscillator()) {
    superfx.Frequency = oscillator->frequency;  //GSU-1, GSU-2
  } else {
    superfx.Frequency = system.cpuFrequency();  //MARIO CHIP 1
  }

  for(auto map : node.find("map")) {
    loadMap(map, {&SuperFX::readIO, &superfx}, {&SuperFX::writeIO, &superfx});
  }

  if(auto memory = node["memory(type=ROM,content=Program)"]) {
    loadMemory(superfx.rom, memory, File::Required);
    for(auto map : memory.find("map")) {
      loadMap(map, superfx.cpurom);
    }
  }

  if(auto memory = node["memory(type=RAM,content=Save)"]) {
    loadMemory(superfx.ram, memory, File::Optional);
    for(auto map : memory.find("map")) {
      loadMap(map, superfx.cpuram);
    }
  }
}

//processor(architecture=ARM6)
auto Cartridge::loadARMDSP(Markup::Node node) -> void {
  has.ARMDSP = true;

  for(auto& word : armdsp.programROM) word = 0x00;
  for(auto& word : armdsp.dataROM) word = 0x00;
  for(auto& word : armdsp.programRAM) word = 0x00;

  if(auto oscillator = game.oscillator()) {
    armdsp.Frequency = oscillator->frequency;
  } else {
    armdsp.Frequency = 21'440'000;
  }

  for(auto map : node.find("map")) {
    loadMap(map, {&ArmDSP::read, &armdsp}, {&ArmDSP::write, &armdsp});
  }

  if(auto memory = node["memory(type=ROM,content=Program,architecture=ARM6)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read, File::Required)) {
        for(auto n : range(128 * 1024)) armdsp.programROM[n] = fp->read();
      }
    }
  }

  if(auto memory = node["memory(type=ROM,content=Data,architecture=ARM6)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read, File::Required)) {
        for(auto n : range(32 * 1024)) armdsp.dataROM[n] = fp->read();
      }
    }
  }

  if(auto memory = node["memory(type=RAM,content=Data,architecture=ARM6)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        for(auto n : range(16 * 1024)) armdsp.programRAM[n] = fp->read();
      }
    }
  }
}

//processor(architecture=HG51BS169)
auto Cartridge::loadHitachiDSP(Markup::Node node, uint roms) -> void {
  for(auto& word : hitachidsp.dataROM) word = 0x000000;
  for(auto& word : hitachidsp.dataRAM) word = 0x00;

  if(auto oscillator = game.oscillator()) {
    hitachidsp.Frequency = oscillator->frequency;
  } else {
    hitachidsp.Frequency = 20'000'000;
  }
  hitachidsp.Roms = roms;  //1 or 2
  hitachidsp.Mapping = 0;  //0 or 1

  if(auto memory = node["memory(type=ROM,content=Program)"]) {
    loadMemory(hitachidsp.rom, memory, File::Required);
    for(auto map : memory.find("map")) {
      loadMap(map, {&HitachiDSP::readROM, &hitachidsp}, {&HitachiDSP::writeROM, &hitachidsp});
    }
  }

  if(auto memory = node["memory(type=RAM,content=Save)"]) {
    loadMemory(hitachidsp.ram, memory, File::Optional);
    for(auto map : memory.find("map")) {
      loadMap(map, {&HitachiDSP::readRAM, &hitachidsp}, {&HitachiDSP::writeRAM, &hitachidsp});
    }
  }

  if(configuration.hacks.coprocessor.preferHLE) {
    has.Cx4 = true;
    for(auto map : node.find("map")) {
      loadMap(map, {&Cx4::read, &cx4}, {&Cx4::write, &cx4});
    }
    if(auto memory = node["memory(type=RAM,content=Data,architecture=HG51BS169)"]) {
      for(auto map : memory.find("map")) {
        loadMap(map, {&Cx4::read, &cx4}, {&Cx4::write, &cx4});
      }
    }
    return;
  }

  if(auto memory = node["memory(type=ROM,content=Data,architecture=HG51BS169)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        for(auto n : range(1 * 1024)) hitachidsp.dataROM[n] = fp->readl(3);
      } else {
        for(auto n : range(1 * 1024)) {
          hitachidsp.dataROM[n]  = hitachidsp.staticDataROM[n * 3 + 0] <<  0;
          hitachidsp.dataROM[n] |= hitachidsp.staticDataROM[n * 3 + 1] <<  8;
          hitachidsp.dataROM[n] |= hitachidsp.staticDataROM[n * 3 + 2] << 16;
        }
      }
    }
  }

  if(auto memory = node["memory(type=RAM,content=Data,architecture=HG51BS169)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        for(auto n : range(3 * 1024)) hitachidsp.dataRAM[n] = fp->readl(1);
      }
    }
    for(auto map : memory.find("map")) {
      loadMap(map, {&HitachiDSP::readDRAM, &hitachidsp}, {&HitachiDSP::writeDRAM, &hitachidsp});
    }
  }

  has.HitachiDSP = true;

  for(auto map : node.find("map")) {
    loadMap(map, {&HitachiDSP::readIO, &hitachidsp}, {&HitachiDSP::writeIO, &hitachidsp});
  }
}

//processor(architecture=uPD7725)
auto Cartridge::loaduPD7725(Markup::Node node) -> void {
  for(auto& word : necdsp.programROM) word = 0x000000;
  for(auto& word : necdsp.dataROM) word = 0x0000;
  for(auto& word : necdsp.dataRAM) word = 0x0000;

  if(auto oscillator = game.oscillator()) {
    necdsp.Frequency = oscillator->frequency;
  } else {
    necdsp.Frequency = 7'600'000;
  }

  bool failed = false;

  if(auto memory = node["memory(type=ROM,content=Program,architecture=uPD7725)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        for(auto n : range(2048)) necdsp.programROM[n] = fp->readl(3);
      } else failed = true;
    }
  }

  if(auto memory = node["memory(type=ROM,content=Data,architecture=uPD7725)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        for(auto n : range(1024)) necdsp.dataROM[n] = fp->readl(2);
      } else failed = true;
    }
  }

  if(failed || configuration.hacks.coprocessor.preferHLE) {
    auto manifest = BML::serialize(game.document);
    if(manifest.find("identifier: DSP1")) {  //also matches DSP1B
      has.DSP1 = true;
      for(auto map : node.find("map")) {
        loadMap(map, {&DSP1::read, &dsp1}, {&DSP1::write, &dsp1});
      }
      return;
    }
    if(manifest.find("identifier: DSP2")) {
      has.DSP2 = true;
      for(auto map : node.find("map")) {
        loadMap(map, {&DSP2::read, &dsp2}, {&DSP2::write, &dsp2});
      }
      return;
    }
    if(manifest.find("identifier: DSP4")) {
      has.DSP4 = true;
      for(auto map : node.find("map")) {
        loadMap(map, {&DSP4::read, &dsp4}, {&DSP4::write, &dsp4});
      }
      return;
    }
  }

  if(failed) {
    //throw an error to the user
    platform->open(ID::SuperFamicom, "DSP3", File::Read, File::Required);
    return;
  }

  if(auto memory = node["memory(type=RAM,content=Data,architecture=uPD7725)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        for(auto n : range(256)) necdsp.dataRAM[n] = fp->readl(2);
      }
    }
    for(auto map : memory.find("map")) {
      loadMap(map, {&NECDSP::readRAM, &necdsp}, {&NECDSP::writeRAM, &necdsp});
    }
  }

  has.NECDSP = true;
  necdsp.revision = NECDSP::Revision::uPD7725;

  for(auto map : node.find("map")) {
    loadMap(map, {&NECDSP::read, &necdsp}, {&NECDSP::write, &necdsp});
  }
}

//processor(architecture=uPD96050)
auto Cartridge::loaduPD96050(Markup::Node node) -> void {
  for(auto& word : necdsp.programROM) word = 0x000000;
  for(auto& word : necdsp.dataROM) word = 0x0000;
  for(auto& word : necdsp.dataRAM) word = 0x0000;

  if(auto oscillator = game.oscillator()) {
    necdsp.Frequency = oscillator->frequency;
  } else {
    necdsp.Frequency = 11'000'000;
  }

  bool failed = false;

  if(auto memory = node["memory(type=ROM,content=Program,architecture=uPD96050)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        for(auto n : range(16384)) necdsp.programROM[n] = fp->readl(3);
      } else failed = true;
    }
  }

  if(auto memory = node["memory(type=ROM,content=Data,architecture=uPD96050)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        for(auto n : range(2048)) necdsp.dataROM[n] = fp->readl(2);
      } else failed = true;
    }
  }

  if(failed || configuration.hacks.coprocessor.preferHLE) {
    auto manifest = BML::serialize(game.document);
    if(manifest.find("identifier: ST010")) {
      has.ST0010 = true;
      if(auto memory = node["memory(type=RAM,content=Data,architecture=uPD96050)"]) {
        for(auto map : memory.find("map")) {
          loadMap(map, {&ST0010::read, &st0010}, {&ST0010::write, &st0010});
        }
      }
      return;
    }
  }

  if(failed) {
    //throw an error to the user
    platform->open(ID::SuperFamicom, "ST011", File::Read, File::Required);
    return;
  }

  if(auto memory = node["memory(type=RAM,content=Data,architecture=uPD96050)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        for(auto n : range(2048)) necdsp.dataRAM[n] = fp->readl(2);
      }
    }
    for(auto map : memory.find("map")) {
      loadMap(map, {&NECDSP::readRAM, &necdsp}, {&NECDSP::writeRAM, &necdsp});
    }
  }

  has.NECDSP = true;
  necdsp.revision = NECDSP::Revision::uPD96050;

  for(auto map : node.find("map")) {
    loadMap(map, {&NECDSP::read, &necdsp}, {&NECDSP::write, &necdsp});
  }
}

//rtc(manufacturer=Epson)
auto Cartridge::loadEpsonRTC(Markup::Node node) -> void {
  has.EpsonRTC = true;

  epsonrtc.initialize();

  for(auto map : node.find("map")) {
    loadMap(map, {&EpsonRTC::read, &epsonrtc}, {&EpsonRTC::write, &epsonrtc});
  }

  if(auto memory = node["memory(type=RTC,content=Time,manufacturer=Epson)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        uint8 data[16] = {0};
        for(auto& byte : data) byte = fp->read();
        epsonrtc.load(data);
      }
    }
  }
}

//rtc(manufacturer=Sharp)
auto Cartridge::loadSharpRTC(Markup::Node node) -> void {
  has.SharpRTC = true;

  sharprtc.initialize();

  for(auto map : node.find("map")) {
    loadMap(map, {&SharpRTC::read, &sharprtc}, {&SharpRTC::write, &sharprtc});
  }

  if(auto memory = node["memory(type=RTC,content=Time,manufacturer=Sharp)"]) {
    if(auto file = game.memory(memory)) {
      if(auto fp = platform->open(ID::SuperFamicom, file->name(), File::Read)) {
        uint8 data[16] = {0};
        for(auto& byte : data) byte = fp->read();
        sharprtc.load(data);
      }
    }
  }
}

//processor(identifier=SPC7110)
auto Cartridge::loadSPC7110(Markup::Node node) -> void {
  has.SPC7110 = true;

  for(auto map : node.find("map")) {
    loadMap(map, {&SPC7110::read, &spc7110}, {&SPC7110::write, &spc7110});
  }

  if(auto mcu = node["mcu"]) {
    for(auto map : mcu.find("map")) {
      loadMap(map, {&SPC7110::mcuromRead, &spc7110}, {&SPC7110::mcuromWrite, &spc7110});
    }
    if(auto memory = mcu["memory(type=ROM,content=Program)"]) {
      loadMemory(spc7110.prom, memory, File::Required);
    }
    if(auto memory = mcu["memory(type=ROM,content=Data)"]) {
      loadMemory(spc7110.drom, memory, File::Required);
    }
  }

  if(auto memory = node["memory(type=RAM,content=Save)"]) {
    loadMemory(spc7110.ram, memory, File::Optional);
    for(auto map : memory.find("map")) {
      loadMap(map, {&SPC7110::mcuramRead, &spc7110}, {&SPC7110::mcuramWrite, &spc7110});
    }
  }
}

//processor(identifier=SDD1)
auto Cartridge::loadSDD1(Markup::Node node) -> void {
  has.SDD1 = true;

  for(auto map : node.find("map")) {
    loadMap(map, {&SDD1::ioRead, &sdd1}, {&SDD1::ioWrite, &sdd1});
  }

  if(auto mcu = node["mcu"]) {
    for(auto map : mcu.find("map")) {
      loadMap(map, {&SDD1::mcuRead, &sdd1}, {&SDD1::mcuWrite, &sdd1});
    }
    if(auto memory = mcu["memory(type=ROM,content=Program)"]) {
      loadMemory(sdd1.rom, memory, File::Required);
    }
  }
}

//processor(identifier=OBC1)
auto Cartridge::loadOBC1(Markup::Node node) -> void {
  has.OBC1 = true;

  for(auto map : node.find("map")) {
    loadMap(map, {&OBC1::read, &obc1}, {&OBC1::write, &obc1});
  }

  if(auto memory = node["memory(type=RAM,content=Save)"]) {
    loadMemory(obc1.ram, memory, File::Optional);
  }
}

//file::exists("msu1/data.rom")
auto Cartridge::loadMSU1() -> void {
  has.MSU1 = true;

  bus.map({&MSU1::readIO, &msu1}, {&MSU1::writeIO, &msu1}, "00-3f,80-bf:2000-2007");
}
