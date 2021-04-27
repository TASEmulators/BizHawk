#pragma once

#include <emulator/emulator.hpp>

namespace Filter {
  using Size = auto (*)(uint& width, uint& height) -> void;
  using Render = auto (*)(uint32_t* palette, uint32_t* output, uint outpitch,
    const uint16_t* input, uint pitch, uint width, uint height) -> void;
}

namespace Filter::None {
  auto size(uint& width, uint& height) -> void;
  auto render(
    uint32_t* colortable, uint32_t* output, uint outpitch,
    const uint16_t* input, uint pitch, uint width, uint height
  ) -> void;
}

namespace Filter::ScanlinesLight {
  auto size(uint& width, uint& height) -> void;
  auto render(
    uint32_t* colortable, uint32_t* output, uint outpitch,
    const uint16_t* input, uint pitch, uint width, uint height
  ) -> void;
}

namespace Filter::ScanlinesDark {
  auto size(uint& width, uint& height) -> void;
  auto render(
    uint32_t* colortable, uint32_t* output, uint outpitch,
    const uint16_t* input, uint pitch, uint width, uint height
  ) -> void;
}

namespace Filter::ScanlinesBlack {
  auto size(uint& width, uint& height) -> void;
  auto render(
    uint32_t* colortable, uint32_t* output, uint outpitch,
    const uint16_t* input, uint pitch, uint width, uint height
  ) -> void;
}

namespace Filter::Pixellate2x {
  auto size(uint& width, uint& height) -> void;
  auto render(
    uint32_t* colortable, uint32_t* output, uint outpitch,
    const uint16_t* input, uint pitch, uint width, uint height
  ) -> void;
}

namespace Filter::Scale2x {
  auto size(uint& width, uint& height) -> void;
  auto render(
    uint32_t* colortable, uint32_t* output, uint outpitch,
    const uint16_t* input, uint pitch, uint width, uint height
  ) -> void;
}

namespace Filter::_2xSaI {
  auto size(uint& width, uint& height) -> void;
  auto render(
    uint32_t* colortable, uint32_t* output, uint outpitch,
    const uint16_t* input, uint pitch, uint width, uint height
  ) -> void;
}

namespace Filter::Super2xSaI {
  auto size(uint& width, uint& height) -> void;
  auto render(
    uint32_t* colortable, uint32_t* output, uint outpitch,
    const uint16_t* input, uint pitch, uint width, uint height
  ) -> void;
}

namespace Filter::SuperEagle {
  auto size(uint& width, uint& height) -> void;
  auto render(
    uint32_t* colortable, uint32_t* output, uint outpitch,
    const uint16_t* input, uint pitch, uint width, uint height
  ) -> void;
}

namespace Filter::LQ2x {
  auto size(uint& width, uint& height) -> void;
  auto render(
    uint32_t* colortable, uint32_t* output, uint outpitch,
    const uint16_t* input, uint pitch, uint width, uint height
  ) -> void;
}

namespace Filter::HQ2x {
  auto size(uint& width, uint& height) -> void;
  auto render(
    uint32_t* colortable, uint32_t* output, uint outpitch,
    const uint16_t* input, uint pitch, uint width, uint height
  ) -> void;
}

namespace Filter::NTSC_RF {
  auto size(uint& width, uint& height) -> void;
  auto render(
    uint32_t* colortable, uint32_t* output, uint outpitch,
    const uint16_t* input, uint pitch, uint width, uint height
  ) -> void;
}

namespace Filter::NTSC_Composite {
  auto size(uint& width, uint& height) -> void;
  auto render(
    uint32_t* colortable, uint32_t* output, uint outpitch,
    const uint16_t* input, uint pitch, uint width, uint height
  ) -> void;
}

namespace Filter::NTSC_SVideo {
  auto size(uint& width, uint& height) -> void;
  auto render(
    uint32_t* colortable, uint32_t* output, uint outpitch,
    const uint16_t* input, uint pitch, uint width, uint height
  ) -> void;
}

namespace Filter::NTSC_RGB {
  auto size(uint& width, uint& height) -> void;
  auto render(
    uint32_t* colortable, uint32_t* output, uint outpitch,
    const uint16_t* input, uint pitch, uint width, uint height
  ) -> void;
}
