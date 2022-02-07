auto SPC7110::aluMultiply() -> void {
  addClocks(30);

  if(r482e & 1) {
    //signed 16-bit x 16-bit multiplication
    int16 r0 = (int16)(r4824 | r4825 << 8);
    int16 r1 = (int16)(r4820 | r4821 << 8);

    int result = r0 * r1;
    r4828 = result;
    r4829 = result >> 8;
    r482a = result >> 16;
    r482b = result >> 24;
  } else {
    //unsigned 16-bit x 16-bit multiplication
    uint16 r0 = (uint16)(r4824 | r4825 << 8);
    uint16 r1 = (uint16)(r4820 | r4821 << 8);

    uint result = r0 * r1;
    r4828 = result;
    r4829 = result >> 8;
    r482a = result >> 16;
    r482b = result >> 24;
  }

  r482f &= 0x7f;
}

auto SPC7110::aluDivide() -> void {
  addClocks(40);

  if(r482e & 1) {
    //signed 32-bit x 16-bit division
    int32 dividend = (int32)(r4820 | r4821 << 8 | r4822 << 16 | r4823 << 24);
    int16 divisor  = (int16)(r4826 | r4827 << 8);

    int32 quotient;
    int16 remainder;

    if(divisor) {
      quotient  = (int32)(dividend / divisor);
      remainder = (int32)(dividend % divisor);
    } else {
      //illegal division by zero
      quotient  = 0;
      remainder = dividend;
    }

    r4828 = quotient;
    r4829 = quotient >> 8;
    r482a = quotient >> 16;
    r482b = quotient >> 24;

    r482c = remainder;
    r482d = remainder >> 8;
  } else {
    //unsigned 32-bit x 16-bit division
    uint32 dividend = (uint32)(r4820 | r4821 << 8 | r4822 << 16 | r4823 << 24);
    uint16 divisor  = (uint16)(r4826 | r4827 << 8);

    uint32 quotient;
    uint16 remainder;

    if(divisor) {
      quotient  = (uint32)(dividend / divisor);
      remainder = (uint16)(dividend % divisor);
    } else {
      //illegal division by zero
      quotient  = 0;
      remainder = dividend;
    }

    r4828 = quotient;
    r4829 = quotient >> 8;
    r482a = quotient >> 16;
    r482b = quotient >> 24;

    r482c = remainder;
    r482d = remainder >> 8;
  }

  r482f &= 0x7f;
}
