namespace Filter::None {

auto size(uint& width, uint& height) -> void {
  width  = width;
  height = height;
}

auto render(
  uint32_t* colortable, uint32_t* output, uint outpitch,
  const uint16_t* input, uint pitch, uint width, uint height
) -> void {
  pitch    >>= 1;
  outpitch >>= 2;

  for(uint y = 0; y < height; y++) {
    const uint16_t* in = input + y * pitch;
    uint32_t* out = output + y * outpitch;
    for(uint x = 0; x < width; x++) {
      *out++ = colortable[*in++];
    }
  }
}

}
