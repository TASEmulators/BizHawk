#pragma once

namespace LZMA {

auto extract(string_view filename) -> vector<uint8_t>;

}
