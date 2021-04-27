#pragma once

#include <nall/arithmetic.hpp>
#include <nall/chrono.hpp>
#include <nall/range.hpp>
#include <nall/serializer.hpp>
#include <nall/stdint.hpp>
#if !defined(PLATFORM_ANDROID)
#include <nall/cipher/chacha20.hpp>
#endif

#if defined(PLATFORM_LINUX) && __has_include(<sys/random.h>)
  #include <sys/random.h>
#elif defined(PLATFORM_ANDROID) && __has_include(<sys/syscall.h>)
  #include <sys/syscall.h>
#elif defined(PLATFORM_WINDOWS) && __has_include(<wincrypt.h>)
  #include <wincrypt.h>
#else
  #include <stdio.h>
#endif

namespace nall {

template<typename Base> struct RNG {
  template<typename T = uint64_t> auto random() -> T {
    T value = 0;
    for(uint n : range((sizeof(T) + 3) / 4)) {
      value = value << 32 | (uint32_t)static_cast<Base*>(this)->read();
    }
    return value;
  }

  template<typename T = uint64_t> auto bound(T range) -> T {
    T threshold = -range % range;
    while(true) {
      T value = random<T>();
      if(value >= threshold) return value % range;
    }
  }

protected:
  auto randomSeed() -> uint256_t {
    uint256_t seed = 0;
    #if defined(PLATFORM_BSD) || defined(PLATFORM_MACOS)
    for(uint n : range(8)) seed = seed << 32 | (uint32_t)arc4random();
    #elif defined(PLATFORM_LINUX) && __has_include(<sys/random.h>)
    getrandom(&seed, 32, GRND_NONBLOCK);
    #elif defined(PLATFORM_ANDROID) && __has_include(<sys/syscall.h>)
    syscall(__NR_getrandom, &seed, 32, 0x0001);  //GRND_NONBLOCK
    #elif defined(PLATFORM_WINDOWS) && __has_include(<wincrypt.h>)
    HCRYPTPROV provider;
    if(CryptAcquireContext(&provider, nullptr, MS_STRONG_PROV, PROV_RSA_FULL, CRYPT_VERIFYCONTEXT)) {
      CryptGenRandom(provider, 32, (BYTE*)&seed);
      CryptReleaseContext(provider, 0);
    }
    #else
    srand(time(nullptr));
    for(uint n : range(32)) seed = seed << 8 | (uint8_t)rand();
    if(auto fp = fopen("/dev/urandom", "rb")) {
      fread(&seed, 32, 1, fp);
      fclose(fp);
    }
    #endif
    return seed;
  }
};

namespace PRNG {

//Galois linear feedback shift register using CRC64 polynomials
struct LFSR : RNG<LFSR> {
  LFSR() { seed(); }

  auto seed(maybe<uint64_t> seed = {}) -> void {
    lfsr = seed ? seed() : (uint64_t)randomSeed();
    for(uint n : range(8)) read();  //hide the CRC64 polynomial from initial output
  }

  auto serialize(serializer& s) -> void {
    s.integer(lfsr);
  }

private:
  auto read() -> uint64_t {
    return lfsr = (lfsr >> 1) ^ (-(lfsr & 1) & crc64);
  }

  static const uint64_t crc64 = 0xc96c'5795'd787'0f42;
  uint64_t lfsr = crc64;

  friend class RNG<LFSR>;
};

struct PCG : RNG<PCG> {
  PCG() { seed(); }

  auto seed(maybe<uint32_t> seed = {}, maybe<uint32_t> sequence = {}) -> void {
    if(!seed) seed = (uint32_t)randomSeed();
    if(!sequence) sequence = 0;

    state = 0;
    increment = sequence() << 1 | 1;
    read();
    state += seed();
    read();
  }

  auto serialize(serializer& s) -> void {
    s.integer(state);
    s.integer(increment);
  }

private:
  auto read() -> uint32_t {
    uint64_t state = this->state;
    this->state = state * 6'364'136'223'846'793'005ull + increment;
    uint32_t xorshift = (state >> 18 ^ state) >> 27;
    uint32_t rotate = state >> 59;
    return xorshift >> rotate | xorshift << (-rotate & 31);
  }

  uint64_t state = 0;
  uint64_t increment = 0;

  friend class RNG<PCG>;
};

}

#if !defined(PLATFORM_ANDROID)
namespace CSPRNG {

//XChaCha20 cryptographically secure pseudo-random number generator
struct XChaCha20 : RNG<XChaCha20> {
  XChaCha20() { seed(); }

  auto seed(maybe<uint256_t> key = {}, maybe<uint192_t> nonce = {}) -> void {
    //the randomness comes from the key; the nonce just adds a bit of added entropy
    if(!key) key = randomSeed();
    if(!nonce) nonce = (uint192_t)clock() << 64 | chrono::nanosecond();
    context = {key(), nonce()};
  }

private:
  auto read() -> uint32_t {
    if(!counter) { context.cipher(); context.increment(); }
    uint32_t value = context.block[counter++];
    if(counter == 16) counter = 0;  //64-bytes per block; 4 bytes per read
    return value;
  }

  Cipher::XChaCha20 context{0, 0};
  uint counter = 0;

  friend class RNG<XChaCha20>;
};

}
#endif

//

template<typename T = uint64_t> inline auto random() -> T {
  static PRNG::PCG pcg;  //note: unseeded
  return pcg.random<T>();
}

}
