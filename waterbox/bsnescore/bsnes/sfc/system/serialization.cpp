auto System::serialize(bool synchronize) -> serializer {
  //deterministic serialization (synchronize=false) is only possible with select libco methods
  if(!co_serializable()) synchronize = true;

  if(!information.serializeSize[synchronize]) return {};  //should never occur
  if(synchronize) runToSave();

  uint signature = 0x31545342;
  uint serializeSize = information.serializeSize[synchronize];
  char version[16] = {};
  char description[512] = {};
  memory::copy(&version, (const char*)Emulator::SerializerVersion, Emulator::SerializerVersion.size());

  serializer s(serializeSize);
  s.integer(signature);
  s.integer(serializeSize);
  s.array(version);
  s.array(description);
  s.boolean(synchronize);
  s.boolean(hacks.fastPPU);
  serializeAll(s, synchronize);
  return s;
}

auto System::unserialize(serializer& s) -> bool {
  uint signature = 0;
  uint serializeSize = 0;
  char version[16] = {};
  char description[512] = {};
  bool synchronize = false;
  bool fastPPU = false;

  s.integer(signature);
  s.integer(serializeSize);
  s.array(version);
  s.array(description);
  s.boolean(synchronize);
  s.boolean(fastPPU);

  if(signature != 0x31545342) return false;
  if(serializeSize != information.serializeSize[synchronize]) return false;
  if(string{version} != Emulator::SerializerVersion) return false;
  if(fastPPU != hacks.fastPPU) return false;

  if(synchronize) power(/* reset = */ false);
  serializeAll(s, synchronize);
  return true;
}

//internal

auto System::serializeAll(serializer& s, bool synchronize) -> void {
  random.serialize(s);
  cartridge.serialize(s);
  cpu.serialize(s);
  smp.serialize(s);
  ppu.serialize(s);
  dsp.serialize(s);

  if(cartridge.has.ICD) icd.serialize(s);
  if(cartridge.has.MCC) mcc.serialize(s);
  if(cartridge.has.DIP) dip.serialize(s);
  if(cartridge.has.Event) event.serialize(s);
  if(cartridge.has.SA1) sa1.serialize(s);
  if(cartridge.has.SuperFX) superfx.serialize(s);
  if(cartridge.has.ARMDSP) armdsp.serialize(s);
  if(cartridge.has.HitachiDSP) hitachidsp.serialize(s);
  if(cartridge.has.NECDSP) necdsp.serialize(s);
  if(cartridge.has.EpsonRTC) epsonrtc.serialize(s);
  if(cartridge.has.SharpRTC) sharprtc.serialize(s);
  if(cartridge.has.SPC7110) spc7110.serialize(s);
  if(cartridge.has.SDD1) sdd1.serialize(s);
  if(cartridge.has.OBC1) obc1.serialize(s);
  if(cartridge.has.MSU1) msu1.serialize(s);

  if(cartridge.has.Cx4) cx4.serialize(s);
  if(cartridge.has.DSP1) dsp1.serialize(s);
  if(cartridge.has.DSP2) dsp2.serialize(s);
  if(cartridge.has.DSP4) dsp4.serialize(s);
  if(cartridge.has.ST0010) st0010.serialize(s);

  if(cartridge.has.BSMemorySlot) bsmemory.serialize(s);
  if(cartridge.has.SufamiTurboSlotA) sufamiturboA.serialize(s);
  if(cartridge.has.SufamiTurboSlotB) sufamiturboB.serialize(s);

  controllerPort1.serialize(s);
  controllerPort2.serialize(s);
  expansionPort.serialize(s);

  if(!synchronize) {
    cpu.serializeStack(s);
    smp.serializeStack(s);
    ppu.serializeStack(s);
    for(auto coprocessor : cpu.coprocessors) {
      coprocessor->serializeStack(s);
    }
  }
}

//perform dry-run state save:
//determines exactly how many bytes are needed to save state for this cartridge,
//as amount varies per game (eg different RAM sizes, special chips, etc.)
auto System::serializeInit(bool synchronize) -> uint {
  serializer s;

  uint signature = 0;
  uint serializeSize = 0;
  char version[16] = {};
  char description[512] = {};

  s.integer(signature);
  s.integer(serializeSize);
  s.array(version);
  s.array(description);
  s.boolean(synchronize);
  s.boolean(hacks.fastPPU);
  serializeAll(s, synchronize);
  return s.size();
}
