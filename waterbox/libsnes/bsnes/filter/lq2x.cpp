namespace Filter::LQ2x {

auto size(uint& width, uint& height) -> void {
  width  *= 2;
  height *= 2;
}

auto render(
  uint32_t* colortable, uint32_t* output, uint outpitch,
  const uint16_t* input, uint pitch, uint width, uint height
) -> void {
  pitch    >>= 1;
  outpitch >>= 2;

  for(uint y = 0; y < height; y++) {
    const uint16_t* in = input + y * pitch;
    uint32_t* out0 = output + y * outpitch * 2;
    uint32_t* out1 = output + y * outpitch * 2 + outpitch;

    int prevline = (y == 0 ? 0 : pitch);
    int nextline = (y == height - 1 ? 0 : pitch);

    for(uint x = 0; x < width; x++) {
      uint16_t A = *(in - prevline);
      uint16_t B = (x > 0) ? *(in - 1) : *in;
      uint16_t C = *in;
      uint16_t D = (x < width - 1) ? *(in + 1) : *in;
      uint16_t E = *(in++ + nextline);
      uint32_t c = colortable[C];

      if(A != E && B != D) {
        *out0++ = (A == B ? colortable[C + A - ((C ^ A) & 0x0421) >> 1] : c);
        *out0++ = (A == D ? colortable[C + A - ((C ^ A) & 0x0421) >> 1] : c);
        *out1++ = (E == B ? colortable[C + E - ((C ^ E) & 0x0421) >> 1] : c);
        *out1++ = (E == D ? colortable[C + E - ((C ^ E) & 0x0421) >> 1] : c);
      } else {
        *out0++ = c;
        *out0++ = c;
        *out1++ = c;
        *out1++ = c;
      }
    }
  }
}

}
