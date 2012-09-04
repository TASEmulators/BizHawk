//ARMv3 (ARM6)

struct ArmDSP : public Coprocessor {
  uint8 *firmware;
  uint8 *programROM;
  uint8 *dataROM;
  uint8 *programRAM;

  #include "registers.hpp"

  static void Enter();
  void enter();
  void tick(unsigned clocks = 1);

  void init();
  void load();
  void unload();
  void power();
  void reset();
  void arm_reset();

  ArmDSP();
  ~ArmDSP();

  uint8 mmio_read(unsigned addr);
  void mmio_write(unsigned addr, uint8 data);

  //opcodes.cpp
  bool condition();
  void opcode(uint32 data);
  void lsl(bool &c, uint32 &rm, uint32 rs);
  void lsr(bool &c, uint32 &rm, uint32 rs);
  void asr(bool &c, uint32 &rm, uint32 rs);
  void ror(bool &c, uint32 &rm, uint32 rs);
  void rrx(bool &c, uint32 &rm);

  void op_multiply();
  void op_move_to_status_register_from_register();
  void op_move_to_register_from_status_register();
  void op_data_immediate_shift();
  void op_data_register_shift();
  void op_data_immediate();
  void op_move_immediate_offset();
  void op_move_register_offset();
  void op_move_multiple();
  void op_branch();

  //memory.cpp
  uint8 bus_read(uint32 addr);
  void bus_write(uint32 addr, uint8 data);

  uint32 bus_readbyte(uint32 addr);
  void bus_writebyte(uint32 addr, uint32 data);

  uint32 bus_readword(uint32 addr);
  void bus_writeword(uint32 addr, uint32 data);

  //disassembler.cpp
  string disassemble_opcode(uint32 pc);
  string disassemble_registers();

  //serialization.cpp
  void serialize(serializer&);
};

extern ArmDSP armdsp;
