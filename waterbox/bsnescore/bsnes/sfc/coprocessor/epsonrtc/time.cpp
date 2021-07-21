auto EpsonRTC::irq(uint2 period) -> void {
  if(stop || pause) return;

  if(period == irqperiod) irqflag = 1;
}

auto EpsonRTC::duty() -> void {
  if(irqduty) irqflag = 0;
}

auto EpsonRTC::roundSeconds() -> void {
  if(roundseconds == 0) return;
  roundseconds = 0;

  if(secondhi >= 3) tickMinute();
  secondlo = 0;
  secondhi = 0;
}

auto EpsonRTC::tick() -> void {
  if(stop || pause) return;

  if(hold) {
    holdtick = 1;
    return;
  }

  resync = 1;
  tickSecond();
}

//below code provides bit-perfect emulation of invalid BCD values on the RTC-4513
//code makes extensive use of variable-length integers (see epsonrtc.hpp for sizes)

auto EpsonRTC::tickSecond() -> void {
  if(secondlo <= 8 || secondlo == 12) {
    secondlo++;
  } else {
    secondlo = 0;
    if(secondhi < 5) {
      secondhi++;
    } else {
      secondhi = 0;
      tickMinute();
    }
  }
}

auto EpsonRTC::tickMinute() -> void {
  if(minutelo <= 8 || minutelo == 12) {
    minutelo++;
  } else {
    minutelo = 0;
    if(minutehi < 5) {
      minutehi++;
    } else {
      minutehi = 0;
      tickHour();
    }
  }
}

auto EpsonRTC::tickHour() -> void {
  if(atime) {
    if(hourhi < 2) {
      if(hourlo <= 8 || hourlo == 12) {
        hourlo++;
      } else {
        hourlo = !(hourlo & 1);
        hourhi++;
      }
    } else {
      if(hourlo != 3 && !(hourlo & 4)) {
        if(hourlo <= 8 || hourlo >= 12) {
          hourlo++;
        } else {
          hourlo = !(hourlo & 1);
          hourhi++;
        }
      } else {
        hourlo = !(hourlo & 1);
        hourhi = 0;
        tickDay();
      }
    }
  } else {
    if(hourhi == 0) {
      if(hourlo <= 8 || hourlo == 12) {
        hourlo++;
      } else {
        hourlo = !(hourlo & 1);
        hourhi ^= 1;
      }
    } else {
      if(hourlo & 1) meridian ^= 1;
      if(hourlo < 2 || hourlo == 4 || hourlo == 5 || hourlo == 8 || hourlo == 12) {
        hourlo++;
      } else {
        hourlo = !(hourlo & 1);
        hourhi ^= 1;
      }
      if(meridian == 0 && !(hourlo & 1)) tickDay();
    }
  }
}

auto EpsonRTC::tickDay() -> void {
  if(calendar == 0) return;
  weekday = (weekday + 1) + (weekday == 6);

  //January - December = 0x01 - 0x09; 0x10 - 0x12
  static const uint daysinmonth[32] = {
    30, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31, 30, 31, 30,
    31, 30, 31, 30, 31, 30, 31, 30, 31, 30, 31, 30, 31, 30, 31, 30,
  };

  uint days = daysinmonth[monthhi << 4 | monthlo];
  if(days == 28) {
    //add one day for leap years
    if((yearhi & 1) == 0 && ((yearlo - 0) & 3) == 0) days++;
    if((yearhi & 1) == 1 && ((yearlo - 2) & 3) == 0) days++;
  }

  if(days == 28 && (dayhi == 3 || (dayhi == 2 && daylo >= 8))) {
    daylo = 1;
    dayhi = 0;
    return tickMonth();
  }

  if(days == 29 && (dayhi == 3 || (dayhi == 2 && (daylo > 8 && daylo != 12)))) {
    daylo = 1;
    dayhi = 0;
    return tickMonth();
  }

  if(days == 30 && (dayhi == 3 || (dayhi == 2 && (daylo == 10 || daylo == 14)))) {
    daylo = 1;
    dayhi = 0;
    return tickMonth();
  }

  if(days == 31 && (dayhi == 3 && (daylo & 3))) {
    daylo = 1;
    dayhi = 0;
    return tickMonth();
  }

  if(daylo <= 8 || daylo == 12) {
    daylo++;
  } else {
    daylo = !(daylo & 1);
    dayhi++;
  }
}

auto EpsonRTC::tickMonth() -> void {
  if(monthhi == 0 || !(monthlo & 2)) {
    if(monthlo <= 8 || monthlo == 12) {
      monthlo++;
    } else {
      monthlo = !(monthlo & 1);
      monthhi ^= 1;
    }
  } else {
    monthlo = !(monthlo & 1);
    monthhi = 0;
    tickYear();
  }
}

auto EpsonRTC::tickYear() -> void {
  if(yearlo <= 8 || yearlo == 12) {
    yearlo++;
  } else {
    yearlo = !(yearlo & 1);
    if(yearhi <= 8 || yearhi == 12) {
      yearhi++;
    } else {
      yearhi = !(yearhi & 1);
    }
  }
}
