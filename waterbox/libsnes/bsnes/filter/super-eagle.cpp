namespace Filter::SuperEagle {

auto size(uint& width, uint& height) -> void {
  width  *= 2;
  height *= 2;
}

uint32_t temp[512 * 480];

auto render(
  uint32_t* colortable, uint32_t* output, uint outpitch,
  const uint16_t* input, uint pitch, uint width, uint height
) -> void {
  for(unsigned y = 0; y < height; y++) {
    const uint16_t *line_in = (const uint16_t*)(((const uint8_t*)input) + pitch * y);
    uint32_t *line_out = temp + y * width;
    for(unsigned x = 0; x < width; x++) {
      line_out[x] = colortable[line_in[x]];
    }
  }

  SuperEagle32((unsigned char*)temp, width * sizeof(uint32_t), 0, (unsigned char*)output, outpitch, width, height);
}

}
