namespace Filter::ScanlinesBlack {

auto size(uint& width, uint& height) -> void {
  width  = width;
  height = height * 2;
}

auto render(
  uint32_t* palette, uint32_t* output, uint outpitch,
  const uint16_t* input, uint pitch, uint width, uint height
) -> void {
  pitch    >>= 1;
  outpitch >>= 2;

  for(unsigned y = 0; y < height; y++) {
    const uint16_t *in = input + y * pitch;
    uint32_t *out0 = output + y * outpitch * 2;
    uint32_t *out1 = output + y * outpitch * 2 + outpitch;

    for(unsigned x = 0; x < width; x++) {
      uint16_t color = *in++;
      *out0++ = palette[color];
      *out1++ = 0;
    }
  }
}

}
