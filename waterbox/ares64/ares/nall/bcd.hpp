#pragma once

#include <typeinfo>

namespace nall {

struct BCD {
    static auto encode(u8 value) -> u8 { return value / 10 << 4 | value % 10; }
    static auto decode(u8 value) -> u8 { return (value >> 4) * 10 + (value & 15); }
};

}
