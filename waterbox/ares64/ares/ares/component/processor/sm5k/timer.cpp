auto SM5K::timerStep() -> void {
  switch(RC & 3) {
  case 0: timerIncrement(); return;
  case 1: if(!n8 (++DIV)) timerIncrement(); return;
  case 2: if(!n16(++DIV)) timerIncrement(); return;
  case 3: return;  //falling edge of P1.1
  }
}

auto SM5K::timerIncrement() -> void {
  if(!++RA) {
    RA = RB;
    IFT = 1;
  }
}
