namespace Filter::ScanlinesLight {

uint16_t adjust[32768];

void initialize() {
  static bool initialized = false;
  if(initialized == true) return;
  initialized = true;

  for(unsigned i = 0; i < 32768; i++) {
    uint8_t r = (i >> 10) & 31;
    uint8_t g = (i >>  5) & 31;
    uint8_t b = (i >>  0) & 31;
    r *= 0.666;
    g *= 0.666;
    b *= 0.666;
    adjust[i] = (r << 10) + (g << 5) + (b << 0);
  }
}

auto size(uint& width, uint& height) -> void {
  width  = width;
  height = height * 2;
}

auto render(
  uint32_t* palette, uint32_t* output, uint outpitch,
  const uint16_t* input, uint pitch, uint width, uint height
) -> void {
  initialize();

  pitch    >>= 1;
  outpitch >>= 2;

  for(unsigned y = 0; y < height; y++) {
    const uint16_t *in = input + y * pitch;
    uint32_t *out0 = output + y * outpitch * 2;
    uint32_t *out1 = output + y * outpitch * 2 + outpitch;

    for(unsigned x = 0; x < width; x++) {
      uint16_t color = *in++;
      *out0++ = palette[color];
      *out1++ = palette[adjust[color]];
    }
  }
}

}
