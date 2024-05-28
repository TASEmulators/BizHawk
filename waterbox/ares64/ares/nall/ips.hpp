#pragma once

namespace nall::IPS {

inline auto apply(array_view<u8> source, array_view<u8> patch, maybe<string&> result = {}) -> maybe<vector<u8>> {
    #define error(text) { if(result) *result = {"error: ", text}; return {}; }
    #define success() { if(result) *result = ""; return target; }

    vector<u8> target;
    for (u32 i : range(source.size())) {
        target.append(source[i]);
    }

    u32 patchOffset = 0;
    auto read = [&]() -> u8 {
        return patch[patchOffset++];
    };
    auto readOffset = [&]() -> u32 {
        u32 result = read() << 16;
        result |= read() << 8;
        result |= read();
        return result;
    };
    auto readLength = [&]() -> u32 {
        u32 result = read() << 8;
        result |= read();
        return result;
    };
    auto write = [&](u32 index, u8 data) {
        target[index] = data;
    };

    if(read() != 'P') error("IPS header invalid");
    if(read() != 'A') error("IPS header invalid");
    if(read() != 'T') error("IPS header invalid");
    if(read() != 'C') error("IPS header invalid");
    if(read() != 'H') error("IPS header invalid");

    u32 patchSize = patch.size();
    while (patchOffset < patchSize - 3) {
        u32 offset = readOffset();
        u32 length = readLength();

        if(target.size() < offset + length) error("Invalid IPS patch file");

        if (length == 0) {
            length = readLength();
            u8 data = read();
            for(u32 i : range(length)) write(offset + i, data);
        } else {
            for (u32 i : range(length)) write(offset + i, read());
        }
    }

    if(read() != 'E') error("IPS footer invalid");
    if(read() != 'O') error("IPS footer invalid");
    if(read() != 'F') error("IPS footer invalid");

    success();
    #undef error
    #undef success
}
}
