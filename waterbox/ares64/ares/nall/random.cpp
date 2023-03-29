#include <nall/random.hpp>

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

NALL_HEADER_INLINE auto RNGBase::randomSeed() -> u256 {
  u256 seed = 0;
  #if defined(PLATFORM_BSD) || defined(PLATFORM_MACOS)
  for(u32 n : range(8)) seed = seed << 32 | (u32)arc4random();
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
  for(u32 n : range(32)) seed = seed << 8 | (u8)rand();
  if(auto fp = fopen("/dev/urandom", "rb")) {
    fread(&seed, 32, 1, fp);
    fclose(fp);
  }
  #endif
  return seed;
}

}
