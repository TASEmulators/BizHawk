auto DD::command(n16 command) -> void {
  ctl.error.selfDiagnostic = 0;
  ctl.error.servoNG = 0;
  ctl.error.indexGapNG = 0;
  ctl.error.timeout = 0;
  ctl.error.undefinedCommand = 0;
  ctl.error.invalidParam = 0;
  io.status.mechaError = 0;
  io.status.writeProtect = 0;

  if(io.status.busyState) return;
  io.status.busyState = 1;

  //most simple command timing response, based on Command::ReadProgramVersion
  u32 count = 8000;

  switch(command) {
    case Command::Nop: {} break;
    case Command::ReadSeek: {
      if(disk) {
        if((io.data.bit(12,15) > 1) || (io.data.bit(0,11) > 0x5D7)) {
          ctl.error.invalidParam = 1;
        } else {
          //TODO: proper research into seek and access times
          count = 993'000;
          if(io.status.headRetracted || io.status.spindleMotorStopped)
            count += 200'700'000;
          count += 19300 * abs(io.data.bit(0,11) - io.currentTrack.bit(0,11));
          io.currentTrack = io.data | 0x6000;
          seekTrack();
          queue.remove(Queue::DD_Motor_Mode);
          io.status.headRetracted = 0;
          io.status.spindleMotorStopped = 0;
          state.seek = 1;
        }
      } else {
        ctl.error.selfDiagnostic = 1;
      }
    } break;
    case Command::WriteSeek: {
      if(disk) {
        if((io.data.bit(12,15) > 1) || (io.data.bit(0,11) > 0x5D7)) {
          ctl.error.invalidParam = 1;
        } else {
          //TODO: proper research into seek and access times
          count = 993'000;
          if(io.status.headRetracted || io.status.spindleMotorStopped)
            count += 200'700'000;
          count += 19300 * abs(io.data.bit(0,11) - io.currentTrack.bit(0,11));
          io.currentTrack = io.data | 0x6000;
          io.status.writeProtect = seekTrack();
          queue.remove(Queue::DD_Motor_Mode);
          io.status.headRetracted = 0;
          io.status.spindleMotorStopped = 0;
          state.seek = 1;
        }
      } else {
        ctl.error.selfDiagnostic = 1;
      }
    } break;
    case Command::Recalibration:
    case Command::Start: {
      //identical commands
      if(disk) {
        //TODO: proper research into seek and access times
        //seek to head 0 track 0
        count = 993'000;
        if(io.status.headRetracted || io.status.spindleMotorStopped)
            count += 200'700'000;
        count += 19300 * abs(0 - io.currentTrack.bit(0,11));
        io.currentTrack = 0x6000;
        seekTrack();
        queue.remove(Queue::DD_Motor_Mode);
        io.status.headRetracted = 0;
        io.status.spindleMotorStopped = 0;
        state.seek = 1;
      } else {
        ctl.error.selfDiagnostic = 1;
      }
    } break;
    case Command::Sleep: {
      if(!io.status.headRetracted || !io.status.spindleMotorStopped)
        count = 83'000'000;
      motorStop();
    } break;
    case Command::SetStandby: {
      if (!io.data.bit(24)) {
        ctl.standbyDelayDisable = 1;
      } else {
        ctl.standbyDelayDisable = 0;

        ctl.standbyDelay = io.data.bit(16,23);
        if (ctl.standbyDelay < 1) ctl.standbyDelay = 1;
        if (ctl.standbyDelay > 0x10) ctl.standbyDelay = 0x10;
        ctl.standbyDelay *= 0x17;
      }
    } break;
    case Command::SetSleep: {
      if (!io.data.bit(24)) {
        ctl.sleepDelayDisable = 1;
      } else {
        ctl.sleepDelayDisable = 0;

        ctl.sleepDelay = io.data.bit(16,23);
        if (ctl.sleepDelay > 0x96) ctl.sleepDelay = 0x96;
        ctl.sleepDelay *= 0x17;
      }
    } break;
    case Command::ClearChangeFlag: {
      io.status.diskChanged = 0;
    } break;
    case Command::ClearResetFlag: {
      io.status.diskChanged = 0;  //that's how it works
      io.status.resetState = 0;
    } break;
    case Command::ReadVersion: {
      if (!io.data.bit(0)) io.data = 0x0114;
      else io.data = 0x5300;
    } break;
    case Command::SetDiskType: {
      ctl.diskType = io.data.bit(0, 3);
    } break;
    case Command::RequestStatus: {
      io.data.bit(0) = ctl.error.selfDiagnostic;
      io.data.bit(1) = ctl.error.servoNG;
      io.data.bit(2) = ctl.error.indexGapNG;
      io.data.bit(3) = ctl.error.timeout;
      io.data.bit(4) = ctl.error.undefinedCommand;
      io.data.bit(5) = ctl.error.invalidParam;
      io.data.bit(6) = ctl.error.unknown;
    } break;
    case Command::Standby: {
      if(!disk) {
        ctl.error.selfDiagnostic = 1;
      } else {
        if(!io.status.headRetracted || !io.status.spindleMotorStopped)
          count = 64'000'000;
        motorStandby();
      }
    } break;
    case Command::IndexLockRetry: {
      if(!disk) {
        ctl.error.selfDiagnostic = 1;
      }
    } break;
    case Command::SetRTCYearMonth: {
      rtc.ram.write<Half>(0, io.data);
    } break;
    case Command::SetRTCDayHour: {
      rtc.ram.write<Half>(2, io.data);
    } break;
    case Command::SetRTCMinuteSecond: {
      rtc.ram.write<Half>(4, io.data);
    } break;
    case Command::GetRTCYearMonth: {
      io.data = rtc.ram.read<Half>(0);
      } break;
    case Command::GetRTCDayHour: {
      io.data = rtc.ram.read<Half>(2);
      } break;
    case Command::GetRTCMinuteSecond: {
      io.data = rtc.ram.read<Half>(4);
      } break;
    case Command::SetLEDBlinkRate: {
      if (io.data.bit(24,31) != 0) ctl.ledOnTime = io.data.bit(24,31);
      if (io.data.bit(16,23) != 0) ctl.ledOffTime = io.data.bit(16,23);
    } break;
    case Command::ReadProgramVersion: {
      io.data = 0x0003;
    } break;
    default: {
      ctl.error.undefinedCommand = 1;
    } break;
  }

  if(ctl.error.selfDiagnostic)        io.status.mechaError = 1;
  else if(ctl.error.servoNG)          io.status.mechaError = 1;
  else if(ctl.error.indexGapNG)       io.status.mechaError = 1;
  else if(ctl.error.timeout)          io.status.mechaError = 1;
  else if(ctl.error.undefinedCommand) io.status.mechaError = 1;
  else if(ctl.error.invalidParam)     io.status.mechaError = 1;
  else if(io.status.writeProtect)     io.status.mechaError = 1;

  queue.insert(Queue::DD_MECHA_Response, count);
}

auto DD::mechaResponse() -> void {
  if(state.seek) {
    state.seek = 0;
    if (io.status.diskPresent) {
      motorActive();
    } else {
      motorStop();
    }
  }
  io.status.busyState = 0;
  raise(IRQ::MECHA);
}
