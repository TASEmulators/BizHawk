#pragma once

#include <emulibc.h>
#include <libco.h>
#include <sljit.h>

#include <nall/platform.hpp>
#include <nall/adaptive-array.hpp>
#include <nall/any.hpp>
#include <nall/array.hpp>
#include <nall/bump-allocator.hpp>
#include <nall/case-range.hpp>
#include <nall/chrono.hpp>
#include <nall/directory.hpp>
#include <nall/dl.hpp>
#include <nall/endian.hpp>
#include <nall/hashset.hpp>
#include <nall/image.hpp>
#include <nall/literals.hpp>
#include <nall/priority-queue.hpp>
#include <nall/queue.hpp>
#include <nall/random.hpp>
#include <nall/serializer.hpp>
#include <nall/set.hpp>
#include <nall/shared-pointer.hpp>
#include <nall/string.hpp>
#include <nall/terminal.hpp>
#include <nall/thread.hpp>
#include <nall/traits.hpp>
#include <nall/unique-pointer.hpp>
#include <nall/variant.hpp>
#include <nall/vector.hpp>
#include <nall/vfs.hpp>
#include <nall/cd.hpp>
#include <nall/dsp/iir/one-pole.hpp>
#include <nall/dsp/iir/biquad.hpp>
#include <nall/dsp/resampler/cubic.hpp>
#include <nall/hash/crc32.hpp>
#include <nall/hash/sha256.hpp>
using namespace nall;
using namespace nall::primitives;

namespace ares {
  extern const string Name;
  extern const string Version;
  extern const string Copyright;
  extern const string License;
  extern const string LicenseURI;
  extern const string Website;
  extern const string WebsiteURI;
  extern const u32 SerializerSignature;

  namespace VFS {
    using Pak = shared_pointer<vfs::directory>;
    using File = shared_pointer<vfs::file>;
  }

  namespace Video {
    static constexpr bool Threaded = false;
  }

  namespace Constants {
    namespace Colorburst {
      static constexpr f64 NTSC = 315.0 / 88.0 * 1'000'000.0;
      static constexpr f64 PAL  = 283.75 * 15'625.0 + 25.0;
    }
  }

  extern bool _runAhead;
  inline auto runAhead() -> bool { return _runAhead; }
  inline auto setRunAhead(bool runAhead) -> void { _runAhead = runAhead; }
}

#include <ares/types.hpp>
#include <ares/random.hpp>
#include <ares/debug/debug.hpp>
#include <ares/node/node.hpp>
#include <ares/platform.hpp>
#include <ares/memory/fixed-allocator.hpp>
#include <ares/memory/readable.hpp>
#include <ares/memory/writable.hpp>
#include <ares/resource/resource.hpp>
