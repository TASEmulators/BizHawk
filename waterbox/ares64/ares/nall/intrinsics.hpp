#pragma once

#if defined(__APPLE__)
  #include <machine/endian.h>
#elif defined(linux) || defined(__linux__)
  #include <endian.h>
#elif defined(__FreeBSD__) || defined(__FreeBSD_kernel__) || defined(__NetBSD__) || defined(__OpenBSD__)
  #include <sys/endian.h>
#endif

namespace nall {

/* Compiler detection */

#if defined(__clang__)
  #define COMPILER_CLANG
  struct Compiler {
    static constexpr bool Clang     = 1;
    static constexpr bool GCC       = 0;
    static constexpr bool Microsoft = 0;
  };
  #pragma clang diagnostic warning "-Wreturn-type"
  #pragma clang diagnostic ignored "-Wunused-result"
  #pragma clang diagnostic ignored "-Wunknown-pragmas"
  #pragma clang diagnostic ignored "-Wempty-body"
  #pragma clang diagnostic ignored "-Wparentheses"
  #pragma clang diagnostic ignored "-Wswitch"
  #pragma clang diagnostic ignored "-Wswitch-bool"
  #pragma clang diagnostic ignored "-Wabsolute-value"
  #pragma clang diagnostic ignored "-Wtrigraphs"
  #pragma clang diagnostic ignored "-Wnarrowing"
  #pragma clang diagnostic ignored "-Wattributes"
#elif defined(__GNUC__)
  #define COMPILER_GCC
  struct Compiler {
    static constexpr bool Clang     = 0;
    static constexpr bool GCC       = 1;
    static constexpr bool Microsoft = 0;
  };
  #pragma GCC diagnostic warning "-Wreturn-type"
  #pragma GCC diagnostic ignored "-Wunused-result"
  #pragma GCC diagnostic ignored "-Wunknown-pragmas"
  #pragma GCC diagnostic ignored "-Wpragmas"
  #pragma GCC diagnostic ignored "-Wswitch-bool"
  #pragma GCC diagnostic ignored "-Wtrigraphs"
  #pragma GCC diagnostic ignored "-Wnarrowing"
  #pragma GCC diagnostic ignored "-Wattributes"
  #pragma GCC diagnostic ignored "-Wstringop-overflow"  //GCC 10.2 warning heuristic is buggy
#elif defined(_MSC_VER)
  #define COMPILER_MICROSOFT
  struct Compiler {
    static constexpr bool Clang     = 0;
    static constexpr bool GCC       = 0;
    static constexpr bool Microsoft = 1;
  };
  #pragma warning(disable:4996)  //libc "deprecation" warnings
#else
  #error "unable to detect compiler"
#endif

/* Platform detection */

#if defined(_WIN32)
  #define PLATFORM_WINDOWS
  struct Platform {
    static constexpr bool Windows = 1;
    static constexpr bool MacOS   = 0;
    static constexpr bool Android = 0;
    static constexpr bool Linux   = 0;
    static constexpr bool BSD     = 0;
  };
#elif defined(__APPLE__)
  #define PLATFORM_MACOS
  struct Platform {
    static constexpr bool Windows = 0;
    static constexpr bool MacOS   = 1;
    static constexpr bool Android = 0;
    static constexpr bool Linux   = 0;
    static constexpr bool BSD     = 0;
  };
#elif defined(__ANDROID__)
  #define PLATFORM_ANDROID
  struct Platform {
    static constexpr bool Windows = 0;
    static constexpr bool MacOS   = 0;
    static constexpr bool Android = 1;
    static constexpr bool Linux   = 0;
    static constexpr bool BSD     = 0;
  };
#elif defined(linux) || defined(__linux__)
  #define PLATFORM_LINUX
  struct Platform {
    static constexpr bool Windows = 0;
    static constexpr bool MacOS   = 0;
    static constexpr bool Android = 0;
    static constexpr bool Linux   = 1;
    static constexpr bool BSD     = 0;
  };
#elif defined(__FreeBSD__) || defined(__FreeBSD_kernel__) || defined(__NetBSD__) || defined(__OpenBSD__)
  #define PLATFORM_BSD
  struct Platform {
    static constexpr bool Windows = 0;
    static constexpr bool MacOS   = 0;
    static constexpr bool Android = 0;
    static constexpr bool Linux   = 0;
    static constexpr bool BSD     = 1;
  };
#else
  #error "unable to detect platform"
#endif

/* ABI detection */

#if defined(_WIN32)
  #define ABI_WINDOWS
  struct ABI {
    static constexpr bool Windows = 1;
    static constexpr bool SystemV = 0;
  };
#else
  #define ABI_SYSTEMV
  struct ABI {
    static constexpr bool Windows = 0;
    static constexpr bool SystemV = 1;
  };
#endif

/* API detection */

#if defined(_WIN32)
  #define API_WINDOWS
  struct API {
    static constexpr bool Windows = 1;
    static constexpr bool Posix   = 0;
  };
#else
  #define API_POSIX
  struct API {
    static constexpr bool Windows = 0;
    static constexpr bool Posix   = 1;
  };
#endif

/* Display server detection */

#if defined(_WIN32)
  #define DISPLAY_WINDOWS
  struct DisplayServer {
    static constexpr bool Windows = 1;
    static constexpr bool Quartz  = 0;
    static constexpr bool Xorg    = 0;
  };
#elif defined(__APPLE__)
  #define DISPLAY_QUARTZ
  struct DisplayServer {
    static constexpr bool Windows = 0;
    static constexpr bool Quartz  = 1;
    static constexpr bool Xorg    = 0;
  };
#else
  #define DISPLAY_XORG
  struct DisplayServer {
    static constexpr bool Windows = 0;
    static constexpr bool Quartz  = 0;
    static constexpr bool Xorg    = 1;
  };
#endif

/* Architecture detection */

#if defined(__i386__) || defined(_M_IX86)
  #define ARCHITECTURE_X86
  struct Architecture {
    static constexpr bool x86   = 1;
    static constexpr bool amd64 = 0;
    static constexpr bool arm64 = 0;
    static constexpr bool arm32 = 0;
    static constexpr bool ppc64 = 0;
    static constexpr bool ppc32 = 0;
  };
#elif defined(__amd64__) || defined(_M_AMD64)
  #define ARCHITECTURE_AMD64
  struct Architecture {
    static constexpr bool x86   = 0;
    static constexpr bool amd64 = 1;
    static constexpr bool arm64 = 0;
    static constexpr bool arm32 = 0;
    static constexpr bool ppc64 = 0;
    static constexpr bool ppc32 = 0;
  };
#elif defined(__aarch64__)
  #define ARCHITECTURE_ARM64
  struct Architecture {
    static constexpr bool x86   = 0;
    static constexpr bool amd64 = 0;
    static constexpr bool arm64 = 1;
    static constexpr bool arm32 = 0;
    static constexpr bool ppc64 = 0;
    static constexpr bool ppc32 = 0;
  };
#elif defined(__arm__)
  #define ARCHITECTURE_ARM32
  struct Architecture {
    static constexpr bool x86   = 0;
    static constexpr bool amd64 = 0;
    static constexpr bool arm64 = 0;
    static constexpr bool arm32 = 1;
    static constexpr bool ppc64 = 0;
    static constexpr bool ppc32 = 0;
  };
#elif defined(__ppc64__) || defined(_ARCH_PPC64)
  #define ARCHITECTURE_PPC64
  struct Architecture {
    static constexpr bool x86   = 0;
    static constexpr bool amd64 = 0;
    static constexpr bool arm64 = 0;
    static constexpr bool arm32 = 0;
    static constexpr bool ppc64 = 1;
    static constexpr bool ppc32 = 0;
  };
#elif defined(__ppc__) || defined(_ARCH_PPC) || defined(_M_PPC)
  #define ARCHITECTURE_PPC32
  struct Architecture {
    static constexpr bool x86   = 0;
    static constexpr bool amd64 = 0;
    static constexpr bool arm64 = 0;
    static constexpr bool arm32 = 0;
    static constexpr bool ppc64 = 0;
    static constexpr bool ppc32 = 1;
  };
#else
  #error "unable to detect architecture"
#endif

/* Endian detection */

#if (defined(__BYTE_ORDER) && defined(__LITTLE_ENDIAN) && __BYTE_ORDER == __LITTLE_ENDIAN) || defined(__LITTLE_ENDIAN__) || defined(__i386__) || defined(__amd64__) || defined(_M_IX86) || defined(_M_AMD64)
  #define ENDIAN_LITTLE
  struct Endian {
    static constexpr bool Little = 1;
    static constexpr bool Big    = 0;
  };
#elif (defined(__BYTE_ORDER) && defined(__BIG_ENDIAN) && __BYTE_ORDER == __BIG_ENDIAN) || defined(__BIG_ENDIAN__) || defined(__powerpc__) || defined(_M_PPC)
  #define ENDIAN_BIG
  struct Endian {
    static constexpr bool Little = 0;
    static constexpr bool Big    = 1;
  };
#else
  #error "unable to detect endian"
#endif

/* Build optimization level detection */

#undef DEBUG
#undef NDEBUG

#if defined(BUILD_DEBUG)
  #define DEBUG
  struct Build {
    static constexpr bool Debug     = 1;
    static constexpr bool Stable    = 0;
    static constexpr bool Minified  = 0;
    static constexpr bool Release   = 0;
    static constexpr bool Optimized = 0;
  };
#elif defined(BUILD_STABLE)
  #define DEBUG
  struct Build {
    static constexpr bool Debug     = 0;
    static constexpr bool Stable    = 1;
    static constexpr bool Minified  = 0;
    static constexpr bool Release   = 0;
    static constexpr bool Optimized = 0;
  };
#elif defined(BUILD_MINIFIED)
  #define NDEBUG
  struct Build {
    static constexpr bool Debug     = 0;
    static constexpr bool Stable    = 0;
    static constexpr bool Minified  = 1;
    static constexpr bool Release   = 0;
    static constexpr bool Optimized = 0;
  };
#elif defined(BUILD_RELEASE)
  #define NDEBUG
  struct Build {
    static constexpr bool Debug     = 0;
    static constexpr bool Stable    = 0;
    static constexpr bool Minified  = 0;
    static constexpr bool Release   = 1;
    static constexpr bool Optimized = 0;
  };
#elif defined(BUILD_OPTIMIZED)
  #define NDEBUG
  struct Build {
    static constexpr bool Debug     = 0;
    static constexpr bool Stable    = 0;
    static constexpr bool Minified  = 0;
    static constexpr bool Release   = 0;
    static constexpr bool Optimized = 1;
  };
#else
  //default to debug mode
  #define BUILD_DEBUG
  #define DEBUG
  struct Build {
    static constexpr bool Debug     = 1;
    static constexpr bool Stable    = 0;
    static constexpr bool Minified  = 0;
    static constexpr bool Release   = 0;
    static constexpr bool Optimized = 0;
  };
#endif

}
