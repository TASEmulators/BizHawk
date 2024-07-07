
auto CPU::FPU::setFloatingPointMode(bool mode) -> void {
  if(mode == 0) {
    //32x64-bit -> 16x64-bit
  } else {
    //16x64-bit -> 32x64-bit
  }
}

template<> auto CPU::fgr_t<s32>(u32 index) -> s32& {
  if(scc.status.floatingPointMode) {
    return fpu.r[index].s32;
  } else if(index & 1) {
    return fpu.r[index & ~1].s32h;
  } else {
    return fpu.r[index & ~1].s32;
  }
}

template<> auto CPU::fgr_s<s32>(u32 index) -> s32& {
  if(scc.status.floatingPointMode) {
    return fpu.r[index].s32;
  } else {
    return fpu.r[index & ~1].s32;
  }
}

template<> auto CPU::fgr_d<s32>(u32 index) -> s32& {
  fpu.r[index].s32h = 0;
  return fpu.r[index].s32;
}

template<> auto CPU::fgr_t<u32>(u32 index) -> u32& {
  return (u32&)fgr_t<s32>(index);
}

template<> auto CPU::fgr_t<f32>(u32 index) -> f32& {
  return fpu.r[index].f32;
}

template<> auto CPU::fgr_d<f32>(u32 index) -> f32& {
  fpu.r[index].f32h = 0;
  return fpu.r[index].f32;
}

template<> auto CPU::fgr_s<f32>(u32 index) -> f32& {
  if(scc.status.floatingPointMode) {
    return fpu.r[index].f32;
  } else {
    return fpu.r[index & ~1].f32;
  }
}

template<> auto CPU::fgr_t<s64>(u32 index) -> s64& {
  if(scc.status.floatingPointMode) {
    return fpu.r[index].s64;
  } else {
    return fpu.r[index & ~1].s64;
  }
}

template<> auto CPU::fgr_d<s64>(u32 index) -> s64& {
  return fpu.r[index].s64;
}

template<> auto CPU::fgr_s<s64>(u32 index) -> s64& {
  return fgr_t<s64>(index);
}

template<> auto CPU::fgr_t<u64>(u32 index) -> u64& {
  return (u64&)fgr_t<s64>(index);
}

template<> auto CPU::fgr_s<u64>(u32 index) -> u64& {
  return fgr_t<u64>(index);
}

template<> auto CPU::fgr_t<f64>(u32 index) -> f64& {
  return fpu.r[index].f64;
}

template<> auto CPU::fgr_d<f64>(u32 index) -> f64& {
  return fgr_t<f64>(index);
}

template<> auto CPU::fgr_s<f64>(u32 index) -> f64& {
  if(scc.status.floatingPointMode) {
    return fpu.r[index].f64;
  } else {
    return fpu.r[index & ~1].f64;
  }
}

auto CPU::getControlRegisterFPU(n5 index) -> u32 {
  n32 data;
  switch(index) {
  case  0:  //coprocessor revision identifier
    data.bit(0, 7) = fpu.coprocessor.revision;
    data.bit(8,15) = fpu.coprocessor.implementation;
    break;
  case 31:  //control / status register
    data.bit( 0) = fpu.csr.roundMode.bit(0);
    data.bit( 1) = fpu.csr.roundMode.bit(1);
    data.bit( 2) = fpu.csr.flag.inexact;
    data.bit( 3) = fpu.csr.flag.underflow;
    data.bit( 4) = fpu.csr.flag.overflow;
    data.bit( 5) = fpu.csr.flag.divisionByZero;
    data.bit( 6) = fpu.csr.flag.invalidOperation;
    data.bit( 7) = fpu.csr.enable.inexact;
    data.bit( 8) = fpu.csr.enable.underflow;
    data.bit( 9) = fpu.csr.enable.overflow;
    data.bit(10) = fpu.csr.enable.divisionByZero;
    data.bit(11) = fpu.csr.enable.invalidOperation;
    data.bit(12) = fpu.csr.cause.inexact;
    data.bit(13) = fpu.csr.cause.underflow;
    data.bit(14) = fpu.csr.cause.overflow;
    data.bit(15) = fpu.csr.cause.divisionByZero;
    data.bit(16) = fpu.csr.cause.invalidOperation;
    data.bit(17) = fpu.csr.cause.unimplementedOperation;
    data.bit(23) = fpu.csr.compare;
    data.bit(24) = fpu.csr.flushSubnormals;
    break;
  }
  return data;
}

auto CPU::setControlRegisterFPU(n5 index, n32 data) -> void {
  //read-only variables are defined but commented out for documentation purposes
  switch(index) {
  case  0:  //coprocessor revision identifier
  //fpu.coprocessor.revision       = data.bit(0, 7);
  //fpu.coprocessor.implementation = data.bit(8,15);
    break;
  case 31: {//control / status register
    u32 roundModePrevious = fpu.csr.roundMode;
    u32 flushSubnormalsPrevious = fpu.csr.flushSubnormals;
    fpu.csr.roundMode.bit(0)             = data.bit( 0);
    fpu.csr.roundMode.bit(1)             = data.bit( 1);
    fpu.csr.flag.inexact                 = data.bit( 2);
    fpu.csr.flag.underflow               = data.bit( 3);
    fpu.csr.flag.overflow                = data.bit( 4);
    fpu.csr.flag.divisionByZero          = data.bit( 5);
    fpu.csr.flag.invalidOperation        = data.bit( 6);
    fpu.csr.enable.inexact               = data.bit( 7);
    fpu.csr.enable.underflow             = data.bit( 8);
    fpu.csr.enable.overflow              = data.bit( 9);
    fpu.csr.enable.divisionByZero        = data.bit(10);
    fpu.csr.enable.invalidOperation      = data.bit(11);
    fpu.csr.cause.inexact                = data.bit(12);
    fpu.csr.cause.underflow              = data.bit(13);
    fpu.csr.cause.overflow               = data.bit(14);
    fpu.csr.cause.divisionByZero         = data.bit(15);
    fpu.csr.cause.invalidOperation       = data.bit(16);
    fpu.csr.cause.unimplementedOperation = data.bit(17);
    fpu.csr.compare                      = data.bit(23);
    fpu.csr.flushSubnormals              = data.bit(24);

    if(fpu.csr.roundMode != roundModePrevious) {
      switch(fpu.csr.roundMode) {
      case 0: fenv.setRound(float_env::toNearest);  break;
      case 1: fenv.setRound(float_env::towardZero); break;
      case 2: fenv.setRound(float_env::upward);     break;
      case 3: fenv.setRound(float_env::downward);   break;
      }
    }

    if(fpu.csr.cause.inexact          && fpu.csr.enable.inexact)          return exception.floatingPoint();
    if(fpu.csr.cause.underflow        && fpu.csr.enable.underflow)        return exception.floatingPoint();
    if(fpu.csr.cause.overflow         && fpu.csr.enable.overflow)         return exception.floatingPoint();
    if(fpu.csr.cause.divisionByZero   && fpu.csr.enable.divisionByZero)   return exception.floatingPoint();
    if(fpu.csr.cause.invalidOperation && fpu.csr.enable.invalidOperation) return exception.floatingPoint();
    if(fpu.csr.cause.unimplementedOperation)                              return exception.floatingPoint();

  } break;
  }
}

auto CPU::fpeDivisionByZero() -> bool {
  fpu.csr.cause.divisionByZero = 1;
  if(fpu.csr.enable.divisionByZero) return true;
  fpu.csr.flag.divisionByZero = 1;
  return false;
}

auto CPU::fpeInexact() -> bool {
  fpu.csr.cause.inexact = 1;
  if(fpu.csr.enable.inexact) return true;
  fpu.csr.flag.inexact = 1;
  return false;
}

auto CPU::fpeUnderflow() -> bool {
  fpu.csr.cause.underflow = 1;
  if(fpu.csr.enable.underflow) return true;
  fpu.csr.flag.underflow = 1;
  return false;
}

auto CPU::fpeOverflow() -> bool {
  fpu.csr.cause.overflow = 1;
  if(fpu.csr.enable.overflow) return true;
  fpu.csr.flag.overflow = 1;
  return false;
}

auto CPU::fpeInvalidOperation() -> bool {
  fpu.csr.cause.invalidOperation = 1;
  if(fpu.csr.enable.invalidOperation) return true;
  fpu.csr.flag.invalidOperation = 1;
  return false;
}

auto CPU::fpeUnimplemented() -> bool {
  fpu.csr.cause.unimplementedOperation = 1;
  return true;
}

template<bool CVT>
auto CPU::checkFPUExceptions() -> bool {
  u32 exc = fenv.testExcept(float_env::divByZero
                          | float_env::inexact
                          | float_env::underflow
                          | float_env::overflow
                          | float_env::invalid);
  if (!exc) return false;

  if constexpr(CVT) {
    if(exc & float_env::invalid) {
      if(fpeUnimplemented()) exception.floatingPoint();
      return true;
    }
  }

  if(exc & float_env::underflow) {
    if(!fpu.csr.flushSubnormals || fpu.csr.enable.underflow || fpu.csr.enable.inexact) {
      if(fpeUnimplemented()) exception.floatingPoint();
      return true;
    }
  }

  bool raise = false;
  if(exc & float_env::divByZero) raise |= fpeDivisionByZero();
  if(exc & float_env::inexact)   raise |= fpeInexact();
  if(exc & float_env::underflow) raise |= fpeUnderflow();
  if(exc & float_env::overflow)  raise |= fpeOverflow();
  if(exc & float_env::invalid)   raise |= fpeInvalidOperation();
  if(raise) exception.floatingPoint();
  return raise;
}

#define CHECK_FPE_IMPL(type, res, operation, convert) \
  fenv.clearExcept(); \
  volatile type v##res = [&]() noinline -> type { return operation; }(); \
  if (checkFPUExceptions<convert>()) return; \
  type res = v##res;

#define CHECK_FPE(type, res, operation)      CHECK_FPE_IMPL(type, res, operation, false)
#define CHECK_FPE_CONV(type, res, operation) CHECK_FPE_IMPL(type, res, operation, true)

auto f32repr(f32 f) -> n32 {
  uint32_t v; memcpy(&v, &f, 4);
  return n32(v);
}

auto f64repr(f64 f) -> n64 {
  uint64_t v; memcpy(&v, &f, 8);
  return n64(v);
}

auto qnan(f32 f) -> bool {
  return f32repr(f).bit(22); 
}

auto qnan(f64 f) -> bool {
  return f64repr(f).bit(51); 
}

auto CPU::fpuCheckStart() -> bool {
  if(!scc.status.enable.coprocessor1) return exception.coprocessor1(), false;
  fpu.csr.cause = {0};
  return true;
}

template <typename T>
auto CPU::fpuCheckInput(T& f) -> bool {
  static_assert(std::is_same_v<T, f32> || std::is_same_v<T, f64>);
  switch (fpclassify(f)) {
  case FP_SUBNORMAL:
    if(fpeUnimplemented()) return exception.floatingPoint(), false;
    return true;
  case FP_NAN:
    if(qnan(f) ? fpeInvalidOperation() : fpeUnimplemented())
      return exception.floatingPoint(), false;
    return true;
  }
  return true;
}

template <typename T>
auto CPU::fpuCheckInputs(T& f1, T& f2) -> bool {
  static_assert(std::is_same_v<T, f32> || std::is_same_v<T, f64>);
  int cl1 = fpclassify(f1), cl2 = fpclassify(f2);
  if((cl1 == FP_NAN && !qnan(f1)) || (cl2 == FP_NAN && !qnan(f2))) {
    if(fpeUnimplemented()) return exception.floatingPoint(), false;
  }
  if(cl1 == FP_SUBNORMAL || cl2 == FP_SUBNORMAL) {
    if(fpeUnimplemented()) return exception.floatingPoint(), false;
  }
  if((cl1 == FP_NAN && qnan(f1)) || (cl2 == FP_NAN && qnan(f2))) {
    if(fpeInvalidOperation()) return exception.floatingPoint(), false;
  }
  return true;
}


template<typename T>
auto fpuFlushResult(T f, u32 roundMode) -> T
{
  switch(roundMode)
  {
  case float_env::toNearest: //RN
  case float_env::towardZero: //RZ
    return copysign(T(), f);
  case float_env::upward: //RP
    return signbit(f) ? -T() : std::numeric_limits<T>::min();
  case float_env::downward: //RM
    return signbit(f) ? -std::numeric_limits<T>::min() : T();
  }
  unreachable;
}
auto CPU::fpuCheckOutput(f32& f) -> bool {
  switch (fpclassify(f)) {
  case FP_SUBNORMAL:
    if(!fpu.csr.flushSubnormals || fpu.csr.enable.underflow || fpu.csr.enable.inexact) {
      if(fpeUnimplemented()) exception.floatingPoint();
      return false;
    }
    fpeUnderflow(); fpeInexact();
    f = fpuFlushResult(f, fenv.getRound());
    return true;
  case FP_NAN: {
    // TODO: why __builtin_nanf doesn't work?
    uint32_t v = 0x7fbf'ffff;
    memcpy(&f, &v, 4);
  } return true;
  }
  return true;
}

auto CPU::fpuCheckOutput(f64& f) -> bool {
  switch (fpclassify(f)) {
  case FP_SUBNORMAL:
    if(!fpu.csr.flushSubnormals || fpu.csr.enable.underflow || fpu.csr.enable.inexact) {
      if(fpeUnimplemented()) exception.floatingPoint();
      return false;
    }
    fpeUnderflow(); fpeInexact();
    f = fpuFlushResult(f, fenv.getRound());
    return true;
  case FP_NAN: {
    // TODO: why __builtin_nanf doesn't work?
    uint64_t v = 0x7ff7'ffff'ffff'ffff;
    memcpy(&f, &v, 8);
  } return true;
  }
  return true;
}

template<>
auto CPU::fpuCheckInputConv<s32>(f32& f) -> bool {
  switch (fpclassify(f)) {
  case FP_SUBNORMAL: case FP_INFINITE: case FP_NAN:
    if (fpeUnimplemented()) return exception.floatingPoint(), false;
  }
  if((f >= 0x1p+31f || f < -0x1p+31f) && fpeUnimplemented())
    return exception.floatingPoint(), false;
  return true;
}

template<>
auto CPU::fpuCheckInputConv<s32>(f64& f) -> bool {
  switch (fpclassify(f)) {
  case FP_SUBNORMAL: case FP_INFINITE: case FP_NAN:
    if (fpeUnimplemented()) return exception.floatingPoint(), false;
  }
  if((f >= 0x1p+31 || f < -0x1p+31) && fpeUnimplemented())
    return exception.floatingPoint(), false;
  return true;
}

template<>
auto CPU::fpuCheckInputConv<s64>(f32& f) -> bool {
  switch (fpclassify(f)) {
  case FP_SUBNORMAL: case FP_INFINITE: case FP_NAN:
    if (fpeUnimplemented()) return exception.floatingPoint(), false;
  }
  if((f >= 0x1p+53f || f <= -0x1p+53f) && fpeUnimplemented())
    return exception.floatingPoint(), false;
  return true;
}

template<>
auto CPU::fpuCheckInputConv<s64>(f64& f) -> bool {
  switch (fpclassify(f)) {
  case FP_SUBNORMAL: case FP_INFINITE: case FP_NAN:
    if (fpeUnimplemented()) return exception.floatingPoint(), false;
  }
  if((f >= 0x1p+53 || f <= -0x1p+53) && fpeUnimplemented())
    return exception.floatingPoint(), false;
  return true;
}

#define CF fpu.csr.compare
#define FD(type) fgr_d<type>(fd)
#define FS(type) fgr_s<type>(fs)
#define FT(type) fgr_t<type>(ft)

auto CPU::BC1(bool value, bool likely, s16 imm) -> void {
  if(!fpuCheckStart()) return;
  if(CF == value) branch.take(ipu.pc + 4 + (imm << 2));
  else if(likely) branch.discard();
  else branch.notTaken();
}

auto CPU::CFC1(r64& rt, u8 rd) -> void {
  if(!scc.status.enable.coprocessor1) return exception.coprocessor1();
  rt.u64 = s32(getControlRegisterFPU(rd));
}

auto CPU::CTC1(cr64& rt, u8 rd) -> void {
  if(!scc.status.enable.coprocessor1) return exception.coprocessor1();
  setControlRegisterFPU(rd, rt.u32);
}

auto CPU::DMFC1(r64& rt, u8 fs) -> void {
  if(!scc.status.enable.coprocessor1) return exception.coprocessor1();
  rt.u64 = FS(u64);
}

auto CPU::DMTC1(cr64& rt, u8 fs) -> void {
  if(!scc.status.enable.coprocessor1) return exception.coprocessor1();
  FS(u64) = rt.u64;
}

auto CPU::FABS_S(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f32);
  if(!fpuCheckInput(ffs)) return;
  auto ffd = fabs(ffs);
  if(!fpuCheckOutput(ffd)) return;
  FD(f32) = ffd;
}

auto CPU::FABS_D(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f64);
  if(!fpuCheckInput(ffs)) return;
  auto ffd = fabs(ffs);
  if(!fpuCheckOutput(ffd)) return;
  FD(f64) = ffd;
}

auto CPU::FADD_S(u8 fd, u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  f32 ffs = FS(f32), fft = FT(f32);
  if(!fpuCheckInputs(ffs, fft)) return;
  CHECK_FPE(f32, ffd, FS(f32) + FT(f32));
  if(!fpuCheckOutput(ffd)) return;
  FD(f32) = ffd;
  step((3 - 1) * 2);
}

auto CPU::FADD_D(u8 fd, u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f64), fft = FT(f64);
  if(!fpuCheckInputs(ffs, fft)) return;
  CHECK_FPE(f64, ffd, ffs + fft);
  if(!fpuCheckOutput(ffd)) return;
  FD(f64) = ffd;
  step((3 - 1) * 2);
}

auto CPU::FCEIL_L_S(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f32);
  if(!fpuCheckInputConv<s64>(ffs)) return;
  CHECK_FPE(s64, ffd, roundCeil<s64>(ffs));
  FD(s64) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FCEIL_L_D(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f64);
  if(!fpuCheckInputConv<s64>(ffs)) return;
  CHECK_FPE(s64, ffd, roundCeil<s64>(ffs));
  FD(s64) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FCEIL_W_S(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f32);
  if(!fpuCheckInputConv<s32>(ffs)) return;
  CHECK_FPE_CONV(s32, ffd, roundCeil<s32>(ffs));
  FD(s32) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FCEIL_W_D(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f64);
  if(!fpuCheckInputConv<s32>(ffs)) return;
  CHECK_FPE_CONV(s32, ffd, roundCeil<s32>(ffs));
  FD(s32) = ffd;
  step((5 - 1) * 2);
}

#define  XORDERED(type, value, quiet) \
  if(isnan(FS(type)) || isnan(FT(type))) { \
    if(isnan(FS(type)) && (!quiet || qnan(FS(type))) && fpeInvalidOperation()) \
      return exception.floatingPoint(); \
    if(isnan(FT(type)) && (!quiet || qnan(FT(type))) && fpeInvalidOperation()) \
      return exception.floatingPoint(); \
    CF = value; \
    return; \
  }
#define   ORDERED(type, value) XORDERED(type, value, 0)
#define UNORDERED(type, value) XORDERED(type, value, 1)

auto CPU::FC_EQ_S(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  UNORDERED(f32, 0); CF = FS(f32) == FT(f32);
}

auto CPU::FC_EQ_D(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  UNORDERED(f64, 0); CF = FS(f64) == FT(f64);
}

auto CPU::FC_F_S(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  UNORDERED(f32, 0); CF = 0;
}

auto CPU::FC_F_D(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  UNORDERED(f64, 0); CF = 0;
}

auto CPU::FC_LE_S(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  ORDERED(f32, 0); CF = FS(f32) <= FT(f32);
}

auto CPU::FC_LE_D(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  ORDERED(f64, 0); CF = FS(f64) <= FT(f64);
}

auto CPU::FC_LT_S(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  ORDERED(f32, 0); CF = FS(f32) < FT(f32);
}

auto CPU::FC_LT_D(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  ORDERED(f64, 0); CF = FS(f64) < FT(f64);
}

auto CPU::FC_NGE_S(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  ORDERED(f32, 1); CF = FS(f32) < FT(f32);
}

auto CPU::FC_NGE_D(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  ORDERED(f64, 1); CF = FS(f64) < FT(f64);
}

auto CPU::FC_NGL_S(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  ORDERED(f32, 1); CF = FS(f32) == FT(f32);
}

auto CPU::FC_NGL_D(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  ORDERED(f64, 1); CF = FS(f64) == FT(f64);
}

auto CPU::FC_NGLE_S(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  ORDERED(f32, 1); CF = 0;
}

auto CPU::FC_NGLE_D(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  ORDERED(f64, 1); CF = 0;
}

auto CPU::FC_NGT_S(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  ORDERED(f32, 1); CF = FS(f32) <= FT(f32);
}

auto CPU::FC_NGT_D(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  ORDERED(f64, 1); CF = FS(f64) <= FT(f64);
}

auto CPU::FC_OLE_S(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  UNORDERED(f32, 0); CF = FS(f32) <= FT(f32);
}

auto CPU::FC_OLE_D(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  UNORDERED(f64, 0); CF = FS(f64) <= FT(f64);
}

auto CPU::FC_OLT_S(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  UNORDERED(f32, 0); CF = FS(f32) < FT(f32);
}

auto CPU::FC_OLT_D(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  UNORDERED(f64, 0); CF = FS(f64) < FT(f64);
}

auto CPU::FC_SEQ_S(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  ORDERED(f32, 0); CF = FS(f32) == FT(f32);
}

auto CPU::FC_SEQ_D(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  ORDERED(f64, 0); CF = FS(f64) == FT(f64);
}

auto CPU::FC_SF_S(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  ORDERED(f32, 0); CF = 0;
}

auto CPU::FC_SF_D(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  ORDERED(f64, 0); CF = 0;
}

auto CPU::FC_UEQ_S(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  UNORDERED(f32, 1); CF = FS(f32) == FT(f32);
}

auto CPU::FC_UEQ_D(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  UNORDERED(f64, 1); CF = FS(f64) == FT(f64);
}

auto CPU::FC_ULE_S(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  UNORDERED(f32, 1); CF = FS(f32) <= FT(f32);
}

auto CPU::FC_ULE_D(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  UNORDERED(f64, 1); CF = FS(f64) <= FT(f64);
}

auto CPU::FC_ULT_S(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  UNORDERED(f32, 1); CF = FS(f32) < FT(f32);
}

auto CPU::FC_ULT_D(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  UNORDERED(f64, 1); CF = FS(f64) < FT(f64);
}

auto CPU::FC_UN_S(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  UNORDERED(f32, 1); CF = 0;
}

auto CPU::FC_UN_D(u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  UNORDERED(f64, 1); CF = 0;
}

#undef   ORDERED
#undef UNORDERED

auto CPU::FCVT_S_S(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  if(fpeUnimplemented()) return exception.floatingPoint();
}

auto CPU::FCVT_S_D(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f64);
  if(!fpuCheckInput(ffs)) return;
  CHECK_FPE(f32, ffd, (f32)ffs);
  if(!fpuCheckOutput(ffd)) return;
  FD(f32) = ffd;
  step((2 - 1) * 2);
}

auto CPU::FCVT_S_W(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(s32);
  CHECK_FPE(f32, ffd, ffs);
  if(!fpuCheckOutput(ffd)) return;
  FD(f32) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FCVT_S_L(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(s64);
  if (ffs >= (s64)0x0080'0000'0000'0000ull || ffs < (s64)0xff80'0000'0000'0000ull) {
    if (fpeUnimplemented()) return exception.floatingPoint();
    return;
  }
  CHECK_FPE(f32, ffd, (f32)ffs);
  if(!fpuCheckOutput(ffd)) return;
  FD(f32) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FCVT_D_S(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f32);
  if(!fpuCheckInput(ffs)) return;
  CHECK_FPE(f64, ffd, ffs);
  if(!fpuCheckOutput(ffd)) return;
  FD(f64) = ffd;
}

auto CPU::FCVT_D_D(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  if(fpeUnimplemented()) return exception.floatingPoint();
}

auto CPU::FCVT_D_W(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(s32);
  CHECK_FPE(f64, ffd, (f64)ffs);
  if(!fpuCheckOutput(ffd)) return;
  FD(f64) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FCVT_D_L(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(s64);
  if (ffs >= (s64)0x0080'0000'0000'0000ull || ffs < (s64)0xff80'0000'0000'0000ull) {
    if (fpeUnimplemented()) return exception.floatingPoint();
    return;
  }
  CHECK_FPE(f64, ffd, (f64)ffs);
  if(!fpuCheckOutput(ffd)) return;
  FD(f64) = ffs;
  step((5 - 1) * 2);
}

auto CPU::FCVT_L_S(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f32);
  if(!fpuCheckInputConv<s64>(ffs)) return;
  CHECK_FPE(s64, ffd, roundCurrent<s64>(ffs));
  FD(s64) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FCVT_L_D(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f64);
  if(!fpuCheckInputConv<s64>(ffs)) return;
  CHECK_FPE(s64, ffd, roundCurrent<s64>(ffs));
  FD(s64) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FCVT_W_S(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f32);
  if(!fpuCheckInputConv<s32>(ffs)) return;
  CHECK_FPE_CONV(s32, ffd, roundCurrent<s32>(ffs));
  FD(s32) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FCVT_W_D(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f64);
  if(!fpuCheckInputConv<s32>(ffs)) return;
  CHECK_FPE_CONV(s32, ffd, roundCurrent<s32>(ffs));
  FD(s32) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FDIV_S(u8 fd, u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f32), fft = FT(f32);
  if(!fpuCheckInputs(ffs, fft)) return;
  CHECK_FPE(f32, ffd, ffs / fft);
  if(!fpuCheckOutput(ffd)) return;
  FD(f32) = ffd;
  step((29 - 1) * 2);
}

auto CPU::FDIV_D(u8 fd, u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f64), fft = FT(f64);
  if(!fpuCheckInputs(ffs, fft)) return;
  CHECK_FPE(f64, ffd, ffs / fft);
  if(!fpuCheckOutput(ffd)) return;
  FD(f64) = ffd;
  step((58 - 1) * 2);
}

auto CPU::FFLOOR_L_S(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f32);
  if(!fpuCheckInputConv<s64>(ffs)) return;
  CHECK_FPE(s64, ffd, roundFloor<s64>(ffs));
  FD(s64) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FFLOOR_L_D(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f64);
  if(!fpuCheckInputConv<s64>(ffs)) return;
  CHECK_FPE(s64, ffd, roundFloor<s64>(ffs));
  FD(s64) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FFLOOR_W_S(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f32);
  if(!fpuCheckInputConv<s32>(ffs)) return;
  CHECK_FPE_CONV(s32, ffd, roundFloor<s32>(ffs));
  FD(s32) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FFLOOR_W_D(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f64);
  if(!fpuCheckInputConv<s32>(ffs)) return;
  CHECK_FPE_CONV(s32, ffd, roundFloor<s32>(ffs));
  FD(s32) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FMOV_S(u8 fd, u8 fs) -> void {
  return FMOV_D(fd, fs);
}

auto CPU::FMOV_D(u8 fd, u8 fs) -> void {
  if(!scc.status.enable.coprocessor1) return exception.coprocessor1();
  FD(f64) = FS(f64);
}

auto CPU::FMUL_S(u8 fd, u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f32), fft = FT(f32);
  if(!fpuCheckInputs(ffs, fft)) return;
  CHECK_FPE(f32, ffd, ffs * fft);
  if(!fpuCheckOutput(ffd)) return;
  FD(f32) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FMUL_D(u8 fd, u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f64), fft = FT(f64);
  if(!fpuCheckInputs(ffs, fft)) return;
  CHECK_FPE(f64, ffd, ffs * fft);
  if(!fpuCheckOutput(ffd)) return;
  FD(f64) = ffd;
  step((8 - 1) * 2);
}

auto CPU::FNEG_S(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f32);
  if(!fpuCheckInput(ffs)) return;
  CHECK_FPE(f32, ffd, -ffs);
  if(!fpuCheckOutput(ffd)) return;
  FD(f32) = ffd;
}

auto CPU::FNEG_D(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f64);
  if(!fpuCheckInput(ffs)) return;
  CHECK_FPE(f64, ffd, -ffs);
  if(!fpuCheckOutput(ffd)) return;
  FD(f64) = ffd;
}

auto CPU::FROUND_L_S(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f32);
  if(!fpuCheckInputConv<s64>(ffs)) return;
  CHECK_FPE(s64, ffd, roundNearest<s64>(ffs));
  if(ffd != ffs && fpeInexact()) return exception.floatingPoint();
  FD(s64) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FROUND_L_D(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f64);
  if(!fpuCheckInputConv<s64>(ffs)) return;
  CHECK_FPE(s64, ffd, roundNearest<s64>(ffs));
  if(ffd != ffs && fpeInexact()) return exception.floatingPoint();
  FD(s64) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FROUND_W_S(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f32);
  if(!fpuCheckInputConv<s32>(ffs)) return;
  CHECK_FPE_CONV(s32, ffd, roundNearest<s32>(ffs));
  if(ffd != ffs && fpeInexact()) return exception.floatingPoint();
  FD(s32) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FROUND_W_D(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f64);
  if(!fpuCheckInputConv<s32>(ffs)) return;
  CHECK_FPE_CONV(s32, ffd, roundNearest<s32>(ffs));
  if(ffd != ffs && fpeInexact()) return exception.floatingPoint();
  FD(s32) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FSQRT_S(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f32);
  if(!fpuCheckInput(ffs)) return;
  CHECK_FPE(f32, ffd, squareRoot(ffs));
  if(!fpuCheckOutput(ffd)) return;
  FD(f32) = ffd;
  step((29 - 1) * 2);
}

auto CPU::FSQRT_D(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f64);
  if(!fpuCheckInput(ffs)) return;
  CHECK_FPE(f64, ffd, squareRoot(ffs));
  if(!fpuCheckOutput(ffd)) return;
  FD(f64) = ffd;
  step((58 - 1) * 2);
}

auto CPU::FSUB_S(u8 fd, u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f32), fft = FT(f32);
  if(!fpuCheckInputs(ffs, fft)) return;
  CHECK_FPE(f32, ffd, ffs - fft);
  if(!fpuCheckOutput(ffd)) return;
  FD(f32) = ffd;
  step((3 - 1) * 2);
}

auto CPU::FSUB_D(u8 fd, u8 fs, u8 ft) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f64), fft = FT(f64);
  if(!fpuCheckInputs(ffs, fft)) return;
  CHECK_FPE(f64, ffd, ffs - fft);
  if(!fpuCheckOutput(ffd)) return;
  FD(f64) = ffd;
  step((3 - 1) * 2);
}

auto CPU::FTRUNC_L_S(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f32);
  if(!fpuCheckInputConv<s64>(ffs)) return;
  CHECK_FPE(s64, ffd, roundTrunc<s64>(ffs));
  if((f32)ffd != ffs && fpeInexact()) return exception.floatingPoint();
  FD(s64) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FTRUNC_L_D(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f64);
  if(!fpuCheckInputConv<s64>(ffs)) return;
  CHECK_FPE(s64, ffd, roundTrunc<s64>(ffs));
  if((f64)ffd != ffs && fpeInexact()) return exception.floatingPoint();
  FD(s64) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FTRUNC_W_S(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f32);
  if(!fpuCheckInputConv<s32>(ffs)) return;
  CHECK_FPE_CONV(s32, ffd, roundTrunc<s32>(ffs));
  if((f32)ffd != ffs && fpeInexact()) return exception.floatingPoint();
  FD(s32) = ffd;
  step((5 - 1) * 2);
}

auto CPU::FTRUNC_W_D(u8 fd, u8 fs) -> void {
  if(!fpuCheckStart()) return;
  auto ffs = FS(f64);
  if(!fpuCheckInputConv<s32>(ffs)) return;
  CHECK_FPE_CONV(s32, ffd, roundTrunc<s32>(ffs));
  if((f64)ffd != ffs && fpeInexact()) return exception.floatingPoint();
  FD(s32) = ffd;
  step((5 - 1) * 2);
}

auto CPU::LDC1(u8 ft, cr64& rs, s16 imm) -> void {
  if(!scc.status.enable.coprocessor1) return exception.coprocessor1();
  if(auto data = read<Dual>(rs.u64 + imm)) FT(u64) = *data;
}

auto CPU::LWC1(u8 ft, cr64& rs, s16 imm) -> void {
  if(!scc.status.enable.coprocessor1) return exception.coprocessor1();
  if(auto data = read<Word>(rs.u64 + imm)) FT(u32) = *data;
}

auto CPU::MFC1(r64& rt, u8 ft) -> void {
  if(!scc.status.enable.coprocessor1) return exception.coprocessor1();
  rt.u64 = FT(s32);
}

auto CPU::MTC1(cr64& rt, u8 ft) -> void {
  if(!scc.status.enable.coprocessor1) return exception.coprocessor1();
  FT(s32) = rt.u32;
}

auto CPU::SDC1(u8 ft, cr64& rs, s16 imm) -> void {
  if(!scc.status.enable.coprocessor1) return exception.coprocessor1();
  write<Dual>(rs.u64 + imm, FT(u64));
}

auto CPU::SWC1(u8 ft, cr64& rs, s16 imm) -> void {
  if(!scc.status.enable.coprocessor1) return exception.coprocessor1();
  write<Word>(rs.u64 + imm, FT(u32));
}

auto CPU::COP1UNIMPLEMENTED() -> void {
  if(!fpuCheckStart()) return;
  if(fpeUnimplemented()) return exception.floatingPoint();
}

auto CPU::FCVT_L_W(u8 fd, u8 fs) -> void { COP1UNIMPLEMENTED(); }
auto CPU::FCVT_L_L(u8 fd, u8 fs) -> void { COP1UNIMPLEMENTED(); }
auto CPU::FCVT_W_W(u8 fd, u8 fs) -> void { COP1UNIMPLEMENTED(); }
auto CPU::FCVT_W_L(u8 fd, u8 fs) -> void { COP1UNIMPLEMENTED(); }

auto CPU::FROUND_L_W(u8 fd, u8 fs) -> void { COP1UNIMPLEMENTED(); }
auto CPU::FTRUNC_L_W(u8 fd, u8 fs) -> void { COP1UNIMPLEMENTED(); }
auto CPU::FCEIL_L_W(u8 fd, u8 fs) -> void { COP1UNIMPLEMENTED(); }
auto CPU::FFLOOR_L_W(u8 fd, u8 fs) -> void { COP1UNIMPLEMENTED(); }
auto CPU::FROUND_W_W(u8 fd, u8 fs) -> void { COP1UNIMPLEMENTED(); }
auto CPU::FTRUNC_W_W(u8 fd, u8 fs) -> void { COP1UNIMPLEMENTED(); }
auto CPU::FCEIL_W_W(u8 fd, u8 fs) -> void { COP1UNIMPLEMENTED(); }
auto CPU::FFLOOR_W_W(u8 fd, u8 fs) -> void { COP1UNIMPLEMENTED(); }
auto CPU::FROUND_L_L(u8 fd, u8 fs) -> void { COP1UNIMPLEMENTED(); }
auto CPU::FTRUNC_L_L(u8 fd, u8 fs) -> void { COP1UNIMPLEMENTED(); }
auto CPU::FCEIL_L_L(u8 fd, u8 fs) -> void { COP1UNIMPLEMENTED(); }
auto CPU::FFLOOR_L_L(u8 fd, u8 fs) -> void { COP1UNIMPLEMENTED(); }
auto CPU::FROUND_W_L(u8 fd, u8 fs) -> void { COP1UNIMPLEMENTED(); }
auto CPU::FTRUNC_W_L(u8 fd, u8 fs) -> void { COP1UNIMPLEMENTED(); }
auto CPU::FCEIL_W_L(u8 fd, u8 fs) -> void { COP1UNIMPLEMENTED(); }
auto CPU::FFLOOR_W_L(u8 fd, u8 fs) -> void { COP1UNIMPLEMENTED(); }

auto CPU::DCFC1(r64& rt, u8 rd) -> void { COP1UNIMPLEMENTED(); }
auto CPU::DCTC1(cr64& rt, u8 rd) -> void { COP1UNIMPLEMENTED(); }

#undef CF
#undef FD
#undef FS
#undef FT
#undef CHECK_FPE
