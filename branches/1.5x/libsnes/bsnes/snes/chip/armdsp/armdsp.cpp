#include <snes/snes.hpp>

#define ARMDSP_CPP
namespace SNES {

static bool trace = 0;

#include "opcodes.cpp"
#include "memory.cpp"
#include "disassembler.cpp"
#include "serialization.cpp"
ArmDSP armdsp;

void ArmDSP::Enter() { armdsp.enter(); }

void ArmDSP::enter() {
  //reset hold delay
  while(bridge.reset) {
    tick();
    continue;
  }

  //reset sequence delay
  if(bridge.ready == false) {
    tick(65536);
    bridge.ready = true;
  }

  while(true) {
    if(scheduler.sync == Scheduler::SynchronizeMode::All) {
      scheduler.exit(Scheduler::ExitReason::SynchronizeEvent);
    }

    if(exception) {
      print("* ARM unknown instruction\n");
      print("\n", disassemble_registers());
      print("\n", disassemble_opcode(pipeline.instruction.address), "\n");
      while(true) tick(frequency);
    }

    if(pipeline.reload) {
      pipeline.reload = false;
      pipeline.prefetch.address = r[15];
      pipeline.prefetch.opcode = bus_readword(r[15]);
      r[15].step();
    }

    pipeline.instruction = pipeline.prefetch;
    pipeline.prefetch.address = r[15];
    pipeline.prefetch.opcode = bus_readword(r[15]);
    r[15].step();

  //todo: bus_readword() calls tick(); so we need to prefetch this as well
  //pipeline.mdr.address = r[15];
  //pipeline.mdr.opcode = bus_readword(r[15]);

  //if(pipeline.instruction.address == 0x00000208) trace = 1;
    if(trace) {
      print("\n", disassemble_registers(), "\n");
      print(disassemble_opcode(pipeline.instruction.address), "\n");
      usleep(200000);
    }
  //trace = 0;

    instruction = pipeline.instruction.opcode;
    if(!condition()) continue;
    if((instruction & 0x0fc000f0) == 0x00000090) { op_multiply(); continue; }
    if((instruction & 0x0fb000f0) == 0x01000000) { op_move_to_register_from_status_register(); continue; }
    if((instruction & 0x0fb000f0) == 0x01200000) { op_move_to_status_register_from_register(); continue; }
    if((instruction & 0x0e000010) == 0x00000000) { op_data_immediate_shift(); continue; }
    if((instruction & 0x0e000090) == 0x00000010) { op_data_register_shift(); continue; }
    if((instruction & 0x0e000000) == 0x02000000) { op_data_immediate(); continue; }
    if((instruction & 0x0e000000) == 0x04000000) { op_move_immediate_offset(); continue; }
    if((instruction & 0x0e000010) == 0x06000000) { op_move_register_offset(); continue; }
    if((instruction & 0x0e000000) == 0x08000000) { op_move_multiple(); continue; }
    if((instruction & 0x0e000000) == 0x0a000000) { op_branch(); continue; }

    exception = true;
  }
}

void ArmDSP::tick(unsigned clocks) {
  if(bridge.timer && --bridge.timer == 0) bridge.busy = false;

  step(clocks);
  synchronize_cpu();
}

//MMIO: $00-3f|80-bf:3800-38ff
//3800-3807 mirrored throughout
//a0 ignored

uint8 ArmDSP::mmio_read(unsigned addr) {
  cpu.synchronize_coprocessors();

  uint8 data = 0x00;
  addr &= 0xff06;

  if(addr == 0x3800) {
    if(bridge.armtocpu.ready) {
      bridge.armtocpu.ready = false;
      data = bridge.armtocpu.data;
    }
  }

  if(addr == 0x3802) {
    bridge.timer = 0;
    bridge.busy = false;
  }

  if(addr == 0x3804) {
    data = bridge.status();
  }

  return data;
}

void ArmDSP::mmio_write(unsigned addr, uint8 data) {
  cpu.synchronize_coprocessors();

  addr &= 0xff06;

  if(addr == 0x3802) {
    bridge.cputoarm.ready = true;
    bridge.cputoarm.data = data;
  }

  if(addr == 0x3804) {
    data &= 1;
    if(!bridge.reset && data) arm_reset();
    bridge.reset = data;
  }
}

void ArmDSP::init() {
}

void ArmDSP::load() {
}

void ArmDSP::unload() {
}

void ArmDSP::power() {
  for(unsigned n = 0; n < 16 * 1024; n++) programRAM[n] = random(0x00);
}

void ArmDSP::reset() {
  bridge.reset = false;
  arm_reset();
}

void ArmDSP::arm_reset() {
  create(ArmDSP::Enter, 21477272);

  bridge.ready = false;
  bridge.timer = 0;
  bridge.timerlatch = 0;
  bridge.busy = false;
  bridge.cputoarm.ready = false;
  bridge.armtocpu.ready = false;

  for(auto &rd : r) rd = 0;
  shiftercarry = 0;
  exception = 0;
  pipeline.reload = true;
  r[15].write = [&] { pipeline.reload = true; };
}

ArmDSP::ArmDSP() {
  firmware = new uint8[160 * 1024]();
  programRAM = new uint8[16 * 1024]();

  programROM = &firmware[0];
  dataROM = &firmware[128 * 1024];
}

ArmDSP::~ArmDSP() {
  delete[] firmware;
  delete[] programRAM;
}

}
