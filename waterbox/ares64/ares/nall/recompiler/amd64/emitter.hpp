#pragma once

struct emitter {
  auto byte() {
  }

  template<typename... P>
  alwaysinline auto byte(u8 data, P&&... p) {
    span.write(data);
    byte(forward<P>(p)...);
  }

  alwaysinline auto word(u16 data) {
    span.write(data >> 0);
    span.write(data >> 8);
  }

  alwaysinline auto dword(u32 data) {
    span.write(data >>  0);
    span.write(data >>  8);
    span.write(data >> 16);
    span.write(data >> 24);
  }

  alwaysinline auto qword(u64 data) {
    span.write(data >>  0);
    span.write(data >>  8);
    span.write(data >> 16);
    span.write(data >> 24);
    span.write(data >> 32);
    span.write(data >> 40);
    span.write(data >> 48);
    span.write(data >> 56);
  }

  alwaysinline auto rex(bool w, bool r, bool x, bool b) {
    u8 data = 0x40 | w << 3 | r << 2 | x << 1 | b << 0;
    if(data == 0x40) return;  //rex prefix not needed
    byte(data);
  }

  //mod: {[r/m], [r/m+dis8], [r/m+dis32], r/m}
  alwaysinline auto modrm(u8 mod, u8 reg, u8 rm) {
    byte(mod << 6 | reg << 3 | rm << 0);
  }

  //scale: {index*1, index*2, index*4, index*8}
  //index: {eax, ecx, edx, ebx, invalid, ebp, esi, edi}
  //base:  {eax, ecx, edx, ebx, esp, displacement, esi, edi}
  alwaysinline auto sib(u8 scale, u8 index, u8 base) {
    byte(scale << 6 | index << 3 | base << 0);
  }

  array_span<u8> span, origin;
} emit;

struct label {
  explicit label(u32 index) : index(index) {}
  u32 index;
};

struct fixup {
  u32 index;
  u32 offset;
  u32 size;
};

vector<u32> labelOffsets;
vector<fixup> fixups;

alwaysinline auto bind(array_span<u8> span) {
  emit.span = span;
  emit.origin = span;
  labelOffsets.reset();
  assert(fixups.size() == 0);
  fixups.reset();
}

alwaysinline auto declareLabel() -> label {
  labelOffsets.append(~0);
  return label{labelOffsets.size() - 1};
}

alwaysinline auto defineLabel(label label) -> amd64::label {
  u32 labelOffset = size();
  labelOffsets[label.index] = labelOffset;
  for(u32 n = 0; n < fixups.size(); ) {
    auto fixup = fixups[n];
    if(fixup.index == label.index) {
      u32 value = labelOffset - (fixup.offset + fixup.size);
      emit.origin.span(fixup.offset, fixup.size).writel(value, fixup.size);
      fixups.removeByIndex(n);
      continue;
    }
    n++;
  }
  return label;
}

alwaysinline auto defineLabel() -> label {
  return defineLabel(declareLabel());
}

alwaysinline auto resolve(label label, u32 offset, u32 size) -> u32 {
  u32 labelOffset = labelOffsets[label.index];
  if(labelOffset == ~0) {
    fixups.append(fixup{label.index, this->size() + offset, size});
    return ~0;
  }
  return labelOffset - (this->size() + offset + size);
}

alwaysinline auto distance(u64 target) const -> s64 {
  return target - (u64)emit.span.data();
}

alwaysinline auto size() const -> u32 {
  return emit.span.data() - emit.origin.data();
}
