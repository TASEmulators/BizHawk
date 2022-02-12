#pragma once

namespace nall {

template<s32...> struct BitField;

/* static BitField */

template<s32 Precision, s32 Index> struct BitField<Precision, Index> {
  static_assert(Precision >= 1 && Precision <= 64);
  using type =
    conditional_t<Precision <=  8, u8,
    conditional_t<Precision <= 16, u16,
    conditional_t<Precision <= 32, u32,
    conditional_t<Precision <= 64, u64,
    void>>>>;
  enum : u32 { shift = Index < 0 ? Precision + Index : Index };
  enum : type { mask = 1ull << shift };

  BitField(const BitField&) = delete;

  auto& operator=(const BitField& source) {
    target = target & ~mask | (bool)source << shift;
    return *this;
  }

  template<typename T> BitField(T* source) : target((type&)*source) {
    static_assert(sizeof(T) == sizeof(type));
  }

  auto bit() const {
    return shift;
  }

  operator bool() const {
    return target & mask;
  }

  auto& operator=(bool source) {
    target = target & ~mask | source << shift;
    return *this;
  }

  auto& operator&=(bool source) {
    target = target & (~mask | source << shift);
    return *this;
  }

  auto& operator^=(bool source) {
    target = target ^ source << shift;
    return *this;
  }

  auto& operator|=(bool source) {
    target = target | source << shift;
    return *this;
  }

private:
  type& target;
};

/* dynamic BitField */

template<s32 Precision> struct BitField<Precision> {
  static_assert(Precision >= 1 && Precision <= 64);
  using type =
    conditional_t<Precision <=  8, u8,
    conditional_t<Precision <= 16, u16,
    conditional_t<Precision <= 32, u32,
    conditional_t<Precision <= 64, u64,
    void>>>>;

  BitField(const BitField&) = delete;

  auto& operator=(const BitField& source) {
    target = target & ~mask | (bool)source << shift;
    return *this;
  }

  template<typename T> BitField(T* source, s32 index) : target((type&)*source) {
    static_assert(sizeof(T) == sizeof(type));
    if(index < 0) index = Precision + index;
    mask = 1ull << index;
    shift = index;
  }

  auto bit() const {
    return shift;
  }

  operator bool() const {
    return target & mask;
  }

  auto& operator=(bool source) {
    target = target & ~mask | source << shift;
    return *this;
  }

  auto& operator&=(bool source) {
    target = target & (~mask | source << shift);
    return *this;
  }

  auto& operator^=(bool source) {
    target = target ^ source << shift;
    return *this;
  }

  auto& operator|=(bool source) {
    target = target | source << shift;
    return *this;
  }

private:
  type& target;
  type mask;
  u32 shift;
};

}
