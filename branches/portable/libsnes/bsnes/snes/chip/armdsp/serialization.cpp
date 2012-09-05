#ifdef ARMDSP_CPP

void ArmDSP::serialize(serializer &s) {
  Processor::serialize(s);

  s.array(programRAM, 16 * 1024);

  s.integer(bridge.cputoarm.ready);
  s.integer(bridge.cputoarm.data);
  s.integer(bridge.armtocpu.ready);
  s.integer(bridge.armtocpu.data);
  s.integer(bridge.timer);
  s.integer(bridge.timerlatch);
  s.integer(bridge.reset);
  s.integer(bridge.ready);
  s.integer(bridge.busy);

  s.integer(cpsr.n);
  s.integer(cpsr.z);
  s.integer(cpsr.c);
  s.integer(cpsr.v);
  s.integer(cpsr.i);
  s.integer(cpsr.f);
  s.integer(cpsr.m);

  s.integer(spsr.n);
  s.integer(spsr.z);
  s.integer(spsr.c);
  s.integer(spsr.v);
  s.integer(spsr.i);
  s.integer(spsr.f);
  s.integer(spsr.m);

  s.integer(r[ 0].data);
  s.integer(r[ 1].data);
  s.integer(r[ 2].data);
  s.integer(r[ 3].data);
  s.integer(r[ 4].data);
  s.integer(r[ 5].data);
  s.integer(r[ 6].data);
  s.integer(r[ 7].data);
  s.integer(r[ 8].data);
  s.integer(r[ 9].data);
  s.integer(r[10].data);
  s.integer(r[11].data);
  s.integer(r[12].data);
  s.integer(r[13].data);
  s.integer(r[14].data);
  s.integer(r[15].data);

  s.integer(shiftercarry);
  s.integer(instruction);
  s.integer(exception);

  s.integer(pipeline.reload);
  s.integer(pipeline.instruction.opcode);
  s.integer(pipeline.instruction.address);
  s.integer(pipeline.prefetch.opcode);
  s.integer(pipeline.prefetch.address);
  s.integer(pipeline.mdr.opcode);
  s.integer(pipeline.mdr.address);
}

#endif
