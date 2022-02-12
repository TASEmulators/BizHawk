#pragma once

namespace nall {

//RS(n,k) = ReedSolomon<Length, Inputs>
template<u32 Length, u32 Inputs>
struct ReedSolomon {
  enum : u32 { Parity = Length - Inputs };
  static_assert(Length <= 255 && Length > 0);
  static_assert(Parity <=  32 && Parity > 0);

  using Field = GaloisField<u8, 255, 0x11d>;
  template<u32 Rows, u32 Cols = 1> using Polynomial = Matrix<Field, Rows, Cols>;

  template<u32 Size>
  static auto shift(Polynomial<Size> polynomial) -> Polynomial<Size> {
    for(s32 n = Size - 1; n > 0; n--) polynomial[n] = polynomial[n - 1];
    polynomial[0] = 0;
    return polynomial;
  }

  template<u32 Size>
  static auto degree(const Polynomial<Size>& polynomial) -> u32 {
    for(s32 n = Size; n > 0; n--) {
      if(polynomial[n - 1] != 0) return n - 1;
    }
    return 0;
  }

  template<u32 Size>
  static auto evaluate(const Polynomial<Size>& polynomial, Field field) -> Field {
    Field sum = 0;
    for(u32 n : range(Size)) sum += polynomial[n] * field.pow(n);
    return sum;
  }

  Polynomial<Length> message;
  Polynomial<Parity> syndromes;
  Polynomial<Parity + 1> locators;

  ReedSolomon() = default;
  ReedSolomon(const ReedSolomon&) = default;

  ReedSolomon(const initializer_list<u8>& source) {
    u32 index = 0;
    for(auto& value : source) {
      if(index >= Length) break;
      message[index++] = value;
    }
  }

  auto operator[](u32 index) -> Field& { return message[index]; }
  auto operator[](u32 index) const -> Field { return message[index]; }

  auto calculateSyndromes() -> void {
    static const Polynomial<Parity> bases = [] {
      Polynomial<Parity> bases;
      for(u32 n : range(Parity)) {
        bases[n] = Field::exp(n);
      }
      return bases;
    }();

    syndromes = {};
    for(u32 m : range(Length)) {
      for(u32 p : range(Parity)) {
        syndromes[p] *= bases[p];
        syndromes[p] += message[m];
      }
    }
  }

  auto generateParity() -> void {
    static const Polynomial<Parity, Parity> matrix = [] {
      Polynomial<Parity, Parity> matrix{};
      for(u32 row : range(Parity)) {
        for(u32 col : range(Parity)) {
          matrix(row, col) = Field::exp(row * col);
        }
      }
      if(auto result = matrix.invert()) return *result;
      throw;  //should never occur
    }();

    for(u32 p : range(Parity)) message[Inputs + p] = 0;
    calculateSyndromes();
    auto parity = matrix * syndromes;
    for(u32 p : range(Parity)) message[Inputs + p] = parity[Parity - (p + 1)];
  }

  auto syndromesAreZero() -> bool {
    for(u32 p : range(Parity)) {
      if(syndromes[p]) return false;
    }
    return true;
  }

  //algorithm: Berlekamp-Massey
  auto calculateLocators() -> void {
    Polynomial<Parity + 1> history{1};
    locators = history;
    u32 errors = 0;

    for(u32 n : range(Parity)) {
      Field discrepancy = 0;
      for(u32 l : range(errors + 1)) {
        discrepancy += locators[l] * syndromes[n - l];
      }

      history = shift(history);
      if(discrepancy) {
        auto located = locators - history * discrepancy;
        if(errors * 2 <= n) {
          errors = (n + 1) - errors;
          history = locators * discrepancy.inv();
        }
        locators = located;
      }
    }
  }

  //algorithm: brute force
  //todo: implement Chien search here
  auto calculateErrors() -> vector<u8> {
    calculateSyndromes();
    if(syndromesAreZero()) return {};  //no errors detected
    calculateLocators();
    vector<u8> errors;
    for(u32 n : range(Length)) {
      if(evaluate(locators, Field{2}.pow(255 - n))) continue;
      errors.append(Length - (n + 1));
    }
    return errors;
  }

  template<u32 Size>
  static auto calculateErasures(array_view<u8> errors) -> maybe<Polynomial<Size, Size>> {
    Polynomial<Size, Size> matrix{};
    for(u32 row : range(Size)) {
      for(u32 col : range(Size)) {
        u32 index = Length - (errors[col] + 1);
        matrix(row, col) = Field::exp(row * index);
      }
    }
    return matrix.invert();
  }

  template<u32 Size>
  auto correctErasures(array_view<u8> errors) -> s32 {
    calculateSyndromes();
    if(syndromesAreZero()) return 0;  //no errors detected
    if(auto matrix = calculateErasures<Size>(errors)) {
      Polynomial<Size> factors;
      for(u32 n : range(Size)) factors[n] = syndromes[n];
      auto errata = matrix() * factors;
      for(u32 m : range(Size)) {
        message[errors[m]] += errata[m];
      }
      calculateSyndromes();
      if(syndromesAreZero()) return Size;  //corrected Size errors
      return -Size;  //failed to correct Size errors
    }
    return -Size;  //should never occur, but might ...
  }

  //note: the erasure matrix is generated as a Polynomial<NxN>, where N is the number of errors to correct.
  //because this is a template parameter, and the actual number of errors may very, this function is needed.
  //the alternative would be to convert Matrix<Rows, Cols> to a dynamically sized Matrix(Rows, Cols) type,
  //but this would require heap memory allocations and would be a massive performance penalty.
  auto correctErrata(array_view<u8> errors) -> s32 {
    if(errors.size() >= Parity) return -errors.size();  //too many errors to be correctable

    switch(errors.size()) {
    case  0: return 0;
    case  1: return correctErasures< 1>(errors);
    case  2: return correctErasures< 2>(errors);
    case  3: return correctErasures< 3>(errors);
    case  4: return correctErasures< 4>(errors);
    case  5: return correctErasures< 5>(errors);
    case  6: return correctErasures< 6>(errors);
    case  7: return correctErasures< 7>(errors);
    case  8: return correctErasures< 8>(errors);
    case  9: return correctErasures< 9>(errors);
    case 10: return correctErasures<10>(errors);
    case 11: return correctErasures<11>(errors);
    case 12: return correctErasures<12>(errors);
    case 13: return correctErasures<13>(errors);
    case 14: return correctErasures<14>(errors);
    case 15: return correctErasures<15>(errors);
    case 16: return correctErasures<16>(errors);
    case 17: return correctErasures<17>(errors);
    case 18: return correctErasures<18>(errors);
    case 19: return correctErasures<19>(errors);
    case 20: return correctErasures<20>(errors);
    case 21: return correctErasures<21>(errors);
    case 22: return correctErasures<22>(errors);
    case 23: return correctErasures<23>(errors);
    case 24: return correctErasures<24>(errors);
    case 25: return correctErasures<25>(errors);
    case 26: return correctErasures<26>(errors);
    case 27: return correctErasures<27>(errors);
    case 28: return correctErasures<28>(errors);
    case 29: return correctErasures<29>(errors);
    case 30: return correctErasures<30>(errors);
    case 31: return correctErasures<31>(errors);
    case 32: return correctErasures<32>(errors);
    }
    return -errors.size();  //it's possible to correct more errors if the above switch were extended ...
  }

  //convenience function for when erasures aren't needed
  auto correctErrors() -> s32 {
    auto errors = calculateErrors();
    return correctErrata(errors);
  }
};

}
