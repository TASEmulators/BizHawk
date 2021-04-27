namespace Filter::NTSC_RF {

struct snes_ntsc_t *ntsc;
snes_ntsc_setup_t setup;
int burst;
int burst_toggle;

void initialize() {
  static bool initialized = false;
  if(initialized == true) return;
  initialized = true;

  ntsc = (snes_ntsc_t*)malloc(sizeof *ntsc);
  setup = snes_ntsc_composite;
  setup.merge_fields = 0;
  snes_ntsc_init(ntsc, &setup);

  burst = 0;
  burst_toggle = (setup.merge_fields ? 0 : 1);
}

void terminate() {
  if(ntsc) free(ntsc);
}

auto size(uint& width, uint& height) -> void {
  width  = SNES_NTSC_OUT_WIDTH(256);
  height = height;
}

auto render(
  uint32_t* colortable_, uint32_t* output, uint outpitch,
  const uint16_t* input, uint pitch, uint width, uint height
) -> void {
  initialize();
  colortable = colortable_;

  pitch    >>= 1;
  outpitch >>= 2;

  if(width <= 256) {
    snes_ntsc_blit      (ntsc, input, pitch, burst, width, height, output, outpitch << 2);
  } else {
    snes_ntsc_blit_hires(ntsc, input, pitch, burst, width, height, output, outpitch << 2);
  }

  burst ^= burst_toggle;
}

}
