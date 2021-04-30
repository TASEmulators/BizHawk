#pragma once

namespace nall {

template<int...> struct BitRange;

/* static BitRange */

template<int Precision, int Lo, int Hi> struct BitRange<Precision, Lo, Hi> {
  static_assert(Precision >= 1 && Precision <= 64);
  enum : uint { bits = Precision };
  using type =
    conditional_t<bits <=  8,  uint8_t,
    conditional_t<bits <= 16, uint16_t,
    conditional_t<bits <= 32, uint32_t,
    conditional_t<bits <= 64, uint64_t,
    void>>>>;
  enum : uint { lo = Lo < 0 ? Precision + Lo : Lo };
  enum : uint { hi = Hi < 0 ? Precision + Hi : Hi };
  enum : type { mask = ~0ull >> 64 - (hi - lo + 1) << lo };
  enum : uint { shift = lo };

  BitRange(const BitRange& source) = delete;

  inline auto& operator=(const BitRange& source) {
    target = target & ~mask | ((source.target & source.mask) >> source.shift) << shift & mask;
    return *this;
  }

  template<typename T> inline BitRange(T* source) : target((type&)*source) {
    static_assert(sizeof(T) == sizeof(type));
  }

  inline operator type() const {
    return (target & mask) >> shift;
  }

  inline auto operator++(int) {
    auto value = (target & mask) >> shift;
    target = target & ~mask | target + (1 << shift) & mask;
    return value;
  }

  inline auto operator--(int) {
    auto value = (target & mask) >> shift;
    target = target & ~mask | target - (1 << shift) & mask;
    return value;
  }

  inline auto& operator++() {
    target = target & ~mask | target + (1 << shift) & mask;
    return *this;
  }

  inline auto& operator--() {
    target = target & ~mask | target - (1 << shift) & mask;
    return *this;
  }

  template<typename T> inline auto& operator=(const T& source) {
    target = target & ~mask | source << shift & mask;
    return *this;
  }

  template<typename T> inline auto& operator*=(const T& source) {
    auto value = ((target & mask) >> shift) * source;
    target = target & ~mask | value << shift & mask;
    return *this;
  }

  template<typename T> inline auto& operator/=(const T& source) {
    auto value = ((target & mask) >> shift) / source;
    target = target & ~mask | value << shift & mask;
    return *this;
  }

  template<typename T> inline auto& operator%=(const T& source) {
    auto value = ((target & mask) >> shift) % source;
    target = target & ~mask | value << shift & mask;
    return *this;
  }

  template<typename T> inline auto& operator+=(const T& source) {
    auto value = ((target & mask) >> shift) + source;
    target = target & ~mask | value << shift & mask;
    return *this;
  }

  template<typename T> inline auto& operator-=(const T& source) {
    auto value = ((target & mask) >> shift) - source;
    target = target & ~mask | value << shift & mask;
    return *this;
  }

  template<typename T> inline auto& operator<<=(const T& source) {
    auto value = ((target & mask) >> shift) << source;
    target = target & ~mask | value << shift & mask;
    return *this;
  }

  template<typename T> inline auto& operator>>=(const T& source) {
    auto value = ((target & mask) >> shift) >> source;
    target = target & ~mask | value << shift & mask;
    return *this;
  }

  template<typename T> inline auto& operator&=(const T& source) {
    target = target & (~mask | source << shift & mask);
    return *this;
  }

  template<typename T> inline auto& operator^=(const T& source) {
    target = target ^ source << shift & mask;
    return *this;
  }

  template<typename T> inline auto& operator|=(const T& source) {
    target = target | source << shift & mask;
    return *this;
  }

private:
  type& target;
};

/* dynamic BitRange */

template<int Precision> struct BitRange<Precision> {
  static_assert(Precision >= 1 && Precision <= 64);
  enum : uint { bits = Precision };
  using type =
    conditional_t<bits <=  8,  uint8_t,
    conditional_t<bits <= 16, uint16_t,
    conditional_t<bits <= 32, uint32_t,
    conditional_t<bits <= 64, uint64_t,
    void>>>>;

  BitRange(const BitRange& source) = delete;

  inline auto& operator=(const BitRange& source) {
    target = target & ~mask | ((source.target & source.mask) >> source.shift) << shift & mask;
    return *this;
  }

  template<typename T> inline BitRange(T* source, int index) : target((type&)*source) {
    static_assert(sizeof(T) == sizeof(type));
    if(index < 0) index = Precision + index;
    mask = 1ull << index;
    shift = index;
  }

  template<typename T> inline BitRange(T* source, int lo, int hi) : target((type&)*source) {
    static_assert(sizeof(T) == sizeof(type));
    if(lo < 0) lo = Precision + lo;
    if(hi < 0) hi = Precision + hi;
    if(lo > hi) swap(lo, hi);
    mask = ~0ull >> 64 - (hi - lo + 1) << lo;
    shift = lo;
  }

  inline operator type() const {
    return (target & mask) >> shift;
  }

  inline auto operator++(int) {
    auto value = (target & mask) >> shift;
    target = target & ~mask | target + (1 << shift) & mask;
    return value;
  }

  inline auto operator--(int) {
    auto value = (target & mask) >> shift;
    target = target & ~mask | target - (1 << shift) & mask;
    return value;
  }

  inline auto& operator++() {
    target = target & ~mask | target + (1 << shift) & mask;
    return *this;
  }

  inline auto& operator--() {
    target = target & ~mask | target - (1 << shift) & mask;
    return *this;
  }

  template<typename T> inline auto& operator=(const T& source) {
    target = target & ~mask | source << shift & mask;
    return *this;
  }

  template<typename T> inline auto& operator*=(const T& source) {
    auto value = ((target & mask) >> shift) * source;
    target = target & ~mask | value << shift & mask;
    return *this;
  }

  template<typename T> inline auto& operator/=(const T& source) {
    auto value = ((target & mask) >> shift) / source;
    target = target & ~mask | value << shift & mask;
    return *this;
  }

  template<typename T> inline auto& operator%=(const T& source) {
    auto value = ((target & mask) >> shift) % source;
    target = target & ~mask | value << shift & mask;
    return *this;
  }

  template<typename T> inline auto& operator+=(const T& source) {
    auto value = ((target & mask) >> shift) + source;
    target = target & ~mask | value << shift & mask;
    return *this;
  }

  template<typename T> inline auto& operator-=(const T& source) {
    auto value = ((target & mask) >> shift) - source;
    target = target & ~mask | value << shift & mask;
    return *this;
  }

  template<typename T> inline auto& operator<<=(const T& source) {
    auto value = ((target & mask) >> shift) << source;
    target = target & ~mask | value << shift & mask;
    return *this;
  }

  template<typename T> inline auto& operator>>=(const T& source) {
    auto value = ((target & mask) >> shift) >> source;
    target = target & ~mask | value << shift & mask;
    return *this;
  }

  template<typename T> inline auto& operator&=(const T& source) {
    target = target & (~mask | source << shift & mask);
    return *this;
  }

  template<typename T> inline auto& operator^=(const T& source) {
    target = target ^ source << shift & mask;
    return *this;
  }

  template<typename T> inline auto& operator|=(const T& source) {
    target = target | source << shift & mask;
    return *this;
  }

private:
  type& target;
  type mask;
  uint shift;
};

}
