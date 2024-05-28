auto DD::serialize(serializer& s) -> void {
  s(rtc);

  s(irq.bm.line);
  s(irq.bm.mask);
  s(irq.mecha.line);
  s(irq.mecha.mask);

  s(ctl.diskType);
  s(ctl.error.selfDiagnostic);
  s(ctl.error.servoNG);
  s(ctl.error.indexGapNG);
  s(ctl.error.timeout);
  s(ctl.error.undefinedCommand);
  s(ctl.error.invalidParam);
  s(ctl.error.unknown);
  s(ctl.standbyDelayDisable);
  s(ctl.standbyDelay);
  s(ctl.sleepDelayDisable);
  s(ctl.sleepDelay);
  s(ctl.ledOnTime);
  s(ctl.ledOffTime);

  s(io.data);

  s(io.status.requestUserSector);
  s(io.status.requestC2Sector);
  s(io.status.busyState);
  s(io.status.resetState);
  s(io.status.spindleMotorStopped);
  s(io.status.headRetracted);
  s(io.status.writeProtect);
  s(io.status.mechaError);
  s(io.status.diskChanged);

  s(io.currentTrack);
  s(io.currentSector);

  s(io.sectorSizeBuf);
  s(io.sectorSize);
  s(io.sectorBlock);
  s(io.id);

  s(io.bm.start);
  s(io.bm.reset);
  s(io.bm.error);
  s(io.bm.blockTransfer);
  s(io.bm.c1Correct);
  s(io.bm.c1Double);
  s(io.bm.c1Single);
  s(io.bm.c1Error);
  s(io.bm.readMode);
  s(io.bm.disableORcheck);
  s(io.bm.disableC1Correction);

  s(io.error.am);
  s(io.error.spindle);
  s(io.error.overrun);
  s(io.error.offTrack);
  s(io.error.clockUnlock);
  s(io.error.selfStop);
  s(io.error.sector);

  s(io.micro.enable);
  s(io.micro.error);

  s(state.seek);
}
