#define ConcatenateType(Size) uint##Size##_t
#define DeclareType(Size) ConcatenateType(Size)

#define Pair DeclareType(PairBits)
#define Type DeclareType(TypeBits)
#define Half DeclareType(HalfBits)

//pick the larger of two types to prevent unnecessary data clamping
#define Cast (typename conditional<sizeof(Pair) >= sizeof(T), Pair, T>::type)

namespace nall {
//namespace Arithmetic {

struct Pair {
  Pair() = default;
  explicit constexpr Pair(const Pair& source) : hi(source.hi), lo(source.lo) {}
  template<typename Hi, typename Lo> constexpr Pair(const Hi& hi, const Lo& lo) : hi(hi), lo(lo) {}
  template<typename T> Pair(const T& source) { _set(*this, source); }

  explicit operator bool() const { return hi | lo; }
  template<typename T> operator T() const { T value; _get(*this, value); return value; }

  auto operator+() const -> Pair { return *this; }
  auto operator-() const -> Pair { return Pair(0) - *this; }
  auto operator~() const -> Pair { return {~hi, ~lo}; }
  auto operator!() const -> bool { return !(hi || lo); }

  auto operator++() -> Pair& { lo++; hi += lo == 0; return *this; }
  auto operator--() -> Pair& { hi -= lo == 0; lo--; return *this; }

  auto operator++(int) -> Pair { Pair r = *this; lo++; hi += lo == 0; return r; }
  auto operator--(int) -> Pair { Pair r = *this; hi -= lo == 0; lo--; return r; }

  auto operator* (const Pair& rhs) const -> Pair { return mul(*this, rhs); }
  auto operator/ (const Pair& rhs) const -> Pair { Pair q, r; div(*this, rhs, q, r); return q; }
  auto operator% (const Pair& rhs) const -> Pair { Pair q, r; div(*this, rhs, q, r); return r; }
  auto operator+ (const Pair& rhs) const -> Pair { return {hi + rhs.hi + (lo + rhs.lo < lo), lo + rhs.lo}; }
  auto operator- (const Pair& rhs) const -> Pair { return {hi - rhs.hi - (lo - rhs.lo > lo), lo - rhs.lo}; }
  auto operator<<(const Pair& rhs) const -> Pair { return shl(*this, rhs); }
  auto operator>>(const Pair& rhs) const -> Pair { return shr(*this, rhs); }
  auto operator& (const Pair& rhs) const -> Pair { return {hi & rhs.hi, lo & rhs.lo}; }
  auto operator| (const Pair& rhs) const -> Pair { return {hi | rhs.hi, lo | rhs.lo}; }
  auto operator^ (const Pair& rhs) const -> Pair { return {hi ^ rhs.hi, lo ^ rhs.lo}; }
  auto operator==(const Pair& rhs) const -> bool { return hi == rhs.hi && lo == rhs.lo; }
  auto operator!=(const Pair& rhs) const -> bool { return hi != rhs.hi || lo != rhs.lo; }
  auto operator>=(const Pair& rhs) const -> bool { return hi > rhs.hi || (hi == rhs.hi && lo >= rhs.lo); }
  auto operator<=(const Pair& rhs) const -> bool { return hi < rhs.hi || (hi == rhs.hi && lo <= rhs.lo); }
  auto operator> (const Pair& rhs) const -> bool { return hi > rhs.hi || (hi == rhs.hi && lo >  rhs.lo); }
  auto operator< (const Pair& rhs) const -> bool { return hi < rhs.hi || (hi == rhs.hi && lo <  rhs.lo); }

  template<typename T> auto& operator*= (const T& rhs) { return *this = *this *  Pair(rhs); }
  template<typename T> auto& operator/= (const T& rhs) { return *this = *this /  Pair(rhs); }
  template<typename T> auto& operator%= (const T& rhs) { return *this = *this %  Pair(rhs); }
  template<typename T> auto& operator+= (const T& rhs) { return *this = *this +  Pair(rhs); }
  template<typename T> auto& operator-= (const T& rhs) { return *this = *this -  Pair(rhs); }
  template<typename T> auto& operator<<=(const T& rhs) { return *this = *this << Pair(rhs); }
  template<typename T> auto& operator>>=(const T& rhs) { return *this = *this >> Pair(rhs); }
  template<typename T> auto& operator&= (const T& rhs) { return *this = *this &  Pair(rhs); }
  template<typename T> auto& operator|= (const T& rhs) { return *this = *this |  Pair(rhs); }
  template<typename T> auto& operator^= (const T& rhs) { return *this = *this ^  Pair(rhs); }

  template<typename T> auto operator* (const T& rhs) const { return Cast(*this) *  Cast(rhs); }
  template<typename T> auto operator/ (const T& rhs) const { return Cast(*this) /  Cast(rhs); }
  template<typename T> auto operator% (const T& rhs) const { return Cast(*this) %  Cast(rhs); }
  template<typename T> auto operator+ (const T& rhs) const { return Cast(*this) +  Cast(rhs); }
  template<typename T> auto operator- (const T& rhs) const { return Cast(*this) -  Cast(rhs); }
  template<typename T> auto operator<<(const T& rhs) const { return Cast(*this) << Cast(rhs); }
  template<typename T> auto operator>>(const T& rhs) const { return Cast(*this) >> Cast(rhs); }
  template<typename T> auto operator& (const T& rhs) const { return Cast(*this) &  Cast(rhs); }
  template<typename T> auto operator| (const T& rhs) const { return Cast(*this) |  Cast(rhs); }
  template<typename T> auto operator^ (const T& rhs) const { return Cast(*this) ^  Cast(rhs); }

  template<typename T> auto operator==(const T& rhs) const -> bool { return Cast(*this) == Cast(rhs); }
  template<typename T> auto operator!=(const T& rhs) const -> bool { return Cast(*this) != Cast(rhs); }
  template<typename T> auto operator>=(const T& rhs) const -> bool { return Cast(*this) >= Cast(rhs); }
  template<typename T> auto operator<=(const T& rhs) const -> bool { return Cast(*this) <= Cast(rhs); }
  template<typename T> auto operator> (const T& rhs) const -> bool { return Cast(*this) >  Cast(rhs); }
  template<typename T> auto operator< (const T& rhs) const -> bool { return Cast(*this) <  Cast(rhs); }

private:
  Type lo;
  Type hi;

  friend auto upper(const Pair&) -> Type;
  friend auto lower(const Pair&) -> Type;
  friend auto bits(Pair) -> uint;
  friend auto square(const Pair&) -> Pair;
  friend auto square(const Pair&, Pair&, Pair&) -> void;
  friend auto mul(const Pair&, const Pair&) -> Pair;
  friend auto mul(const Pair&, const Pair&, Pair&, Pair&) -> void;
  friend auto div(const Pair&, const Pair&, Pair&, Pair&) -> void;
  template<typename T> friend auto shl(const Pair&, const T&) -> Pair;
  template<typename T> friend auto shr(const Pair&, const T&) -> Pair;
};

template<> struct ArithmeticNatural<PairBits> {
  using type = Pair;
};

#define ConcatenateUDL(Size) _u##Size
#define DeclareUDL(Size) ConcatenateUDL(Size)

alwaysinline auto operator"" DeclareUDL(PairBits)(const char* s) -> Pair {
  Pair p = 0;
  if(s[0] == '0' && (s[1] == 'x' || s[1] == 'X')) {
    s += 2;
    while(*s) {
      auto c = *s++;
      if(c == '\'');
      else if(c >= '0' && c <= '9') p = (p << 4) + (c - '0');
      else if(c >= 'a' && c <= 'f') p = (p << 4) + (c - 'a' + 10);
      else if(c >= 'A' && c <= 'F') p = (p << 4) + (c - 'A' + 10);
      else break;
    }
  } else {
    while(*s) {
      auto c = *s++;
      if(c == '\'');
      else if(c >= '0' && c <= '9') p = (p << 3) + (p << 1) + (c - '0');
      else break;
    }
  }
  return p;
}

#undef ConcatenateUDL
#undef DeclareUDL

template<typename T> alwaysinline auto _set(Pair& lhs, const T& rhs) -> enable_if_t<(sizeof(Pair) == sizeof(T))> {
  lhs = rhs;
}

template<typename T> alwaysinline auto _set(Pair& lhs, const T& rhs) -> enable_if_t<(sizeof(Pair) > sizeof(T))> {
  lhs = {0, rhs};
}

template<typename T> alwaysinline auto _set(Pair& lhs, const T& rhs) -> enable_if_t<(sizeof(Pair) < sizeof(T))> {
  lhs = {lower(rhs) >> TypeBits, lower(rhs)};
}

template<typename T> alwaysinline auto _get(const Pair& lhs, T& rhs) -> enable_if_t<(sizeof(T) == sizeof(Pair))> {
  rhs = lhs;
}

template<typename T> alwaysinline auto _get(const Pair& lhs, T& rhs) -> enable_if_t<(sizeof(T) > sizeof(Pair))> {
  rhs = {0, lhs};
}

template<typename T> alwaysinline auto _get(const Pair& lhs, T& rhs) -> enable_if_t<(sizeof(T) < sizeof(Pair))> {
  rhs = lower(lhs);
}

alwaysinline auto upper(const Pair& value) -> Type { return value.hi; }
alwaysinline auto lower(const Pair& value) -> Type { return value.lo; }

alwaysinline auto bits(Pair value) -> uint {
  if(value.hi) {
    uint bits = TypeBits;
    while(value.hi) value.hi >>= 1, bits++;
    return bits;
  } else {
    uint bits = 0;
    while(value.lo) value.lo >>= 1, bits++;
    return bits;
  }
}

//Bits * Bits => Bits
inline auto square(const Pair& lhs) -> Pair {
  static const Type Mask = (Type(0) - 1) >> HalfBits;
  Type a = lhs.hi >> HalfBits, b = lhs.hi & Mask, c = lhs.lo >> HalfBits, d = lhs.lo & Mask;
  Type dd = square(d), dc = d * c, db = d * b, da = d * a;
  Type cc = square(c), cb = c * b;

  Pair r0 = Pair(dd);
  Pair r1 = Pair(dc) + Pair(dc) + Pair(r0 >> HalfBits);
  Pair r2 = Pair(db) + Pair(cc) + Pair(db) + Pair(r1 >> HalfBits);
  Pair r3 = Pair(da) + Pair(cb) + Pair(cb) + Pair(da) + Pair(r2 >> HalfBits);

  return {(r3.lo & Mask) << HalfBits | (r2.lo & Mask), (r1.lo & Mask) << HalfBits | (r0.lo & Mask)};
}

//Bits * Bits => 2 * Bits
inline auto square(const Pair& lhs, Pair& hi, Pair& lo) -> void {
  static const Type Mask = (Type(0) - 1) >> HalfBits;
  Type a = lhs.hi >> HalfBits, b = lhs.hi & Mask, c = lhs.lo >> HalfBits, d = lhs.lo & Mask;
  Type dd = square(d), dc = d * c, db = d * b, da = d * a;
  Type cc = square(c), cb = c * b, ca = c * a;
  Type bb = square(b), ba = b * a;
  Type aa = square(a);

  Pair r0 = Pair(dd);
  Pair r1 = Pair(dc) + Pair(dc) + Pair(r0 >> HalfBits);
  Pair r2 = Pair(db) + Pair(cc) + Pair(db) + Pair(r1 >> HalfBits);
  Pair r3 = Pair(da) + Pair(cb) + Pair(cb) + Pair(da) + Pair(r2 >> HalfBits);
  Pair r4 = Pair(ca) + Pair(bb) + Pair(ca) + Pair(r3 >> HalfBits);
  Pair r5 = Pair(ba) + Pair(ba) + Pair(r4 >> HalfBits);
  Pair r6 = Pair(aa) + Pair(r5 >> HalfBits);
  Pair r7 = Pair(r6 >> HalfBits);

  hi = {(r7.lo & Mask) << HalfBits | (r6.lo & Mask), (r5.lo & Mask) << HalfBits | (r4.lo & Mask)};
  lo = {(r3.lo & Mask) << HalfBits | (r2.lo & Mask), (r1.lo & Mask) << HalfBits | (r0.lo & Mask)};
}

//Bits * Bits => Bits
alwaysinline auto mul(const Pair& lhs, const Pair& rhs) -> Pair {
  static const Type Mask = (Type(0) - 1) >> HalfBits;
  Type a = lhs.hi >> HalfBits, b = lhs.hi & Mask, c = lhs.lo >> HalfBits, d = lhs.lo & Mask;
  Type e = rhs.hi >> HalfBits, f = rhs.hi & Mask, g = rhs.lo >> HalfBits, h = rhs.lo & Mask;

  Pair r0 = Pair(d * h);
  Pair r1 = Pair(c * h) + Pair(d * g) + Pair(r0 >> HalfBits);
  Pair r2 = Pair(b * h) + Pair(c * g) + Pair(d * f) + Pair(r1 >> HalfBits);
  Pair r3 = Pair(a * h) + Pair(b * g) + Pair(c * f) + Pair(d * e) + Pair(r2 >> HalfBits);

  return {(r3.lo & Mask) << HalfBits | (r2.lo & Mask), (r1.lo & Mask) << HalfBits | (r0.lo & Mask)};
}

//Bits * Bits => 2 * Bits
alwaysinline auto mul(const Pair& lhs, const Pair& rhs, Pair& hi, Pair& lo) -> void {
  static const Type Mask = (Type(0) - 1) >> HalfBits;
  Type a = lhs.hi >> HalfBits, b = lhs.hi & Mask, c = lhs.lo >> HalfBits, d = lhs.lo & Mask;
  Type e = rhs.hi >> HalfBits, f = rhs.hi & Mask, g = rhs.lo >> HalfBits, h = rhs.lo & Mask;

  Pair r0 = Pair(d * h);
  Pair r1 = Pair(c * h) + Pair(d * g) + Pair(r0 >> HalfBits);
  Pair r2 = Pair(b * h) + Pair(c * g) + Pair(d * f) + Pair(r1 >> HalfBits);
  Pair r3 = Pair(a * h) + Pair(b * g) + Pair(c * f) + Pair(d * e) + Pair(r2 >> HalfBits);
  Pair r4 = Pair(a * g) + Pair(b * f) + Pair(c * e) + Pair(r3 >> HalfBits);
  Pair r5 = Pair(a * f) + Pair(b * e) + Pair(r4 >> HalfBits);
  Pair r6 = Pair(a * e) + Pair(r5 >> HalfBits);
  Pair r7 = Pair(r6 >> HalfBits);

  hi = {(r7.lo & Mask) << HalfBits | (r6.lo & Mask), (r5.lo & Mask) << HalfBits | (r4.lo & Mask)};
  lo = {(r3.lo & Mask) << HalfBits | (r2.lo & Mask), (r1.lo & Mask) << HalfBits | (r0.lo & Mask)};
}

alwaysinline auto div(const Pair& lhs, const Pair& rhs, Pair& quotient, Pair& remainder) -> void {
  if(!rhs) throw std::runtime_error("division by zero");
  quotient = 0, remainder = lhs;
  if(!lhs || lhs < rhs) return;

  auto count = bits(lhs) - bits(rhs);
  Pair x = rhs << count;
  Pair y = Pair(1) << count;
  if(x > remainder) x >>= 1, y >>= 1;
  while(remainder >= rhs) {
    if(remainder >= x) remainder -= x, quotient |= y;
    x >>= 1, y >>= 1;
  }
}

template<typename T> alwaysinline auto shl(const Pair& lhs, const T& rhs) -> Pair {
  if(!rhs) return lhs;
  auto shift = (uint)rhs;
  if(shift < TypeBits) {
    return {lhs.hi << shift | lhs.lo >> (TypeBits - shift), lhs.lo << shift};
  } else {
    return {lhs.lo << (shift - TypeBits), 0};
  }
}

template<typename T> alwaysinline auto shr(const Pair& lhs, const T& rhs) -> Pair {
  if(!rhs) return lhs;
  auto shift = (uint)rhs;
  if(shift < TypeBits) {
    return {lhs.hi >> shift, lhs.hi << (TypeBits - shift) | lhs.lo >> shift};
  } else {
    return {0, lhs.hi >> (shift - TypeBits)};
  }
}

template<typename T> alwaysinline auto rol(const Pair& lhs, const T& rhs) -> Pair {
  return lhs << rhs | lhs >> (PairBits - rhs);
}

template<typename T> alwaysinline auto ror(const Pair& lhs, const T& rhs) -> Pair {
  return lhs >> rhs | lhs << (PairBits - rhs);
}

#define EI enable_if_t<is_integral<T>::value>

template<typename T, EI> auto& operator*= (T& lhs, const Pair& rhs) { return lhs = lhs *  T(rhs); }
template<typename T, EI> auto& operator/= (T& lhs, const Pair& rhs) { return lhs = lhs /  T(rhs); }
template<typename T, EI> auto& operator%= (T& lhs, const Pair& rhs) { return lhs = lhs %  T(rhs); }
template<typename T, EI> auto& operator+= (T& lhs, const Pair& rhs) { return lhs = lhs +  T(rhs); }
template<typename T, EI> auto& operator-= (T& lhs, const Pair& rhs) { return lhs = lhs -  T(rhs); }
template<typename T, EI> auto& operator<<=(T& lhs, const Pair& rhs) { return lhs = lhs << T(rhs); }
template<typename T, EI> auto& operator>>=(T& lhs, const Pair& rhs) { return lhs = lhs >> T(rhs); }
template<typename T, EI> auto& operator&= (T& lhs, const Pair& rhs) { return lhs = lhs &  T(rhs); }
template<typename T, EI> auto& operator|= (T& lhs, const Pair& rhs) { return lhs = lhs |  T(rhs); }
template<typename T, EI> auto& operator^= (T& lhs, const Pair& rhs) { return lhs = lhs ^  T(rhs); }

template<typename T, EI> auto operator* (const T& lhs, const Pair& rhs) { return Cast(lhs) *  Cast(rhs); }
template<typename T, EI> auto operator/ (const T& lhs, const Pair& rhs) { return Cast(lhs) /  Cast(rhs); }
template<typename T, EI> auto operator% (const T& lhs, const Pair& rhs) { return Cast(lhs) %  Cast(rhs); }
template<typename T, EI> auto operator+ (const T& lhs, const Pair& rhs) { return Cast(lhs) +  Cast(rhs); }
template<typename T, EI> auto operator- (const T& lhs, const Pair& rhs) { return Cast(lhs) -  Cast(rhs); }
template<typename T, EI> auto operator<<(const T& lhs, const Pair& rhs) { return Cast(lhs) << Cast(rhs); }
template<typename T, EI> auto operator>>(const T& lhs, const Pair& rhs) { return Cast(lhs) >> Cast(rhs); }
template<typename T, EI> auto operator& (const T& lhs, const Pair& rhs) { return Cast(lhs) &  Cast(rhs); }
template<typename T, EI> auto operator| (const T& lhs, const Pair& rhs) { return Cast(lhs) |  Cast(rhs); }
template<typename T, EI> auto operator^ (const T& lhs, const Pair& rhs) { return Cast(lhs) ^  Cast(rhs); }

template<typename T, EI> auto operator==(const T& lhs, const Pair& rhs) { return Cast(lhs) == Cast(rhs); }
template<typename T, EI> auto operator!=(const T& lhs, const Pair& rhs) { return Cast(lhs) != Cast(rhs); }
template<typename T, EI> auto operator>=(const T& lhs, const Pair& rhs) { return Cast(lhs) >= Cast(rhs); }
template<typename T, EI> auto operator<=(const T& lhs, const Pair& rhs) { return Cast(lhs) <= Cast(rhs); }
template<typename T, EI> auto operator> (const T& lhs, const Pair& rhs) { return Cast(lhs) >  Cast(rhs); }
template<typename T, EI> auto operator< (const T& lhs, const Pair& rhs) { return Cast(lhs) <  Cast(rhs); }

#undef EI

template<> struct stringify<Pair> {
  stringify(Pair source) {
    char _output[1 + sizeof(Pair) * 3];
    auto p = (char*)&_output;
    do {
      Pair quotient, remainder;
      div(source, 10, quotient, remainder);
      *p++ = remainder + '0';
      source = quotient;
    } while(source);
    _size = p - _output;
    *p = 0;
    for(int x = _size - 1, y = 0; x >= 0 && y < _size; x--, y++) _data[x] = _output[y];
  }

  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return _size; }
  char _data[1 + sizeof(Pair) * 3];
  uint _size;
};

}

#undef ConcatenateType
#undef DeclareType
#undef Pair
#undef Type
#undef Half
#undef Cast
