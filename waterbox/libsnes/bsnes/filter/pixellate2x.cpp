namespace Filter::Pixellate2x {

auto size(uint& width, uint& height) -> void {
  width  = (width  <= 256) ? width  * 2 : width;
  height = (height <= 240) ? height * 2 : height;
}

auto render(
  uint32_t* colortable, uint32_t* output, uint outpitch,
  const uint16_t* input, uint pitch, uint width, uint height
) -> void {
  pitch >>= 1;
  outpitch >>= 2;

  uint32_t *out0 = output;
  uint32_t *out1 = output + outpitch;

  for(unsigned y = 0; y < height; y++) {
    for(unsigned x = 0; x < width; x++) {
      uint32_t p = colortable[*input++];

      *out0++ = p;
      if(height <= 240) *out1++ = p;
      if(width > 256) continue;

      *out0++ = p;
      if(height <= 240) *out1++ = p;
    }

    input += pitch - width;
    if(height <= 240) {
      out0 += outpitch + outpitch - 512;
      out1 += outpitch + outpitch - 512;
    } else {
      out0 += outpitch - 512;
    }
  }
}

}
