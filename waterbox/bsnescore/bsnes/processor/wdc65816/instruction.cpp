auto WDC65816::interrupt() -> void {
  read(PC.d);
  idle();
N push(PC.b);
  push(PC.h);
  push(PC.l);
  push(EF ? P & ~0x10 : P);
  IF = 1;
  DF = 0;
  PC.l = read(r.vector + 0);
L PC.h = read(r.vector + 1);
  PC.b = 0x00;
  idleJump();
}

//both the accumulator and index registers can independently be in either 8-bit or 16-bit mode.
//controlled via the M/X flags, this changes the execution details of various instructions.
//rather than implement four instruction tables for all possible combinations of these bits,
//instead use macro abuse to generate all four tables based off of a single template table.
auto WDC65816::instruction() -> void {
  //a = instructions unaffected by M/X flags
  //m = instructions affected by M flag (1 = 8-bit; 0 = 16-bit)
  //x = instructions affected by X flag (1 = 8-bit; 0 = 16-bit)

  #define opA(id, name, ...) case id: return instruction##name(__VA_ARGS__);
  if(MF) {
    #define opM(id, name, ...) case id: return instruction##name##8(__VA_ARGS__);
    #define m(name) &WDC65816::algorithm##name##8
    if(XF) {
      #define opX(id, name, ...) case id: return instruction##name##8(__VA_ARGS__);
      #define x(name) &WDC65816::algorithm##name##8
      #include "instruction.hpp"
      #undef opX
      #undef x
    } else {
      #define opX(id, name, ...) case id: return instruction##name##16(__VA_ARGS__);
      #define x(name) &WDC65816::algorithm##name##16
      #include "instruction.hpp"
      #undef opX
      #undef x
    }
    #undef opM
    #undef m
  } else {
    #define opM(id, name, ...) case id: return instruction##name##16(__VA_ARGS__);
    #define m(name) &WDC65816::algorithm##name##16
    if(XF) {
      #define opX(id, name, ...) case id: return instruction##name##8(__VA_ARGS__);
      #define x(name) &WDC65816::algorithm##name##8
      #include "instruction.hpp"
      #undef opX
      #undef x
    } else {
      #define opX(id, name, ...) case id: return instruction##name##16(__VA_ARGS__);
      #define x(name) &WDC65816::algorithm##name##16
      #include "instruction.hpp"
      #undef opX
      #undef x
    }
    #undef opM
    #undef m
  }
  #undef opA
}
