#pragma once

/* nall
 * author: byuu
 * license: ISC
 *
 * nall is a header library that provides both fundamental and useful classes
 * its goals are portability, consistency, minimalism and reusability
 */

//include the most common nall headers with one statement
//does not include the most obscure components with high cost and low usage

#include <nall/platform.hpp>

#include <nall/algorithm.hpp>
#include <nall/any.hpp>
//#include <nall/arguments.hpp>
#include <nall/arithmetic.hpp>
#include <nall/array.hpp>
#include <nall/array-span.hpp>
#include <nall/array-view.hpp>
#include <nall/atoi.hpp>
#include <nall/bit.hpp>
#include <nall/chrono.hpp>
#include <nall/directory.hpp>
#include <nall/dl.hpp>
#include <nall/endian.hpp>
#include <nall/file.hpp>
#include <nall/file-buffer.hpp>
#include <nall/file-map.hpp>
#include <nall/function.hpp>
#include <nall/galois-field.hpp>
#include <nall/hashset.hpp>
#include <nall/hid.hpp>
#include <nall/image.hpp>
#include <nall/inode.hpp>
#include <nall/instance.hpp>
#include <nall/interpolation.hpp>
#include <nall/intrinsics.hpp>
#include <nall/iterator.hpp>
#include <nall/literals.hpp>
#include <nall/locale.hpp>
#include <nall/location.hpp>
#include <nall/map.hpp>
#include <nall/matrix.hpp>
#include <nall/matrix-multiply.hpp>
#include <nall/maybe.hpp>
#include <nall/memory.hpp>
#include <nall/merge-sort.hpp>
#include <nall/path.hpp>
#include <nall/pointer.hpp>
#include <nall/primitives.hpp>
#include <nall/queue.hpp>
#include <nall/random.hpp>
#include <nall/range.hpp>
#include <nall/reed-solomon.hpp>
#include <nall/run.hpp>
#include <nall/serializer.hpp>
#include <nall/set.hpp>
#include <nall/shared-pointer.hpp>
#include <nall/simd.hpp>
#include <nall/stdint.hpp>
#include <nall/string.hpp>
#include <nall/terminal.hpp>
#include <nall/thread.hpp>
#include <nall/traits.hpp>
#include <nall/unique-pointer.hpp>
#include <nall/utility.hpp>
#include <nall/variant.hpp>
#include <nall/varint.hpp>
#include <nall/vector.hpp>
#include <nall/view.hpp>
#include <nall/arguments.hpp>  //todo: compilation errors when included earlier
#include <nall/decode/base.hpp>
#include <nall/decode/base64.hpp>
#include <nall/decode/bmp.hpp>
#include <nall/decode/gzip.hpp>
#include <nall/decode/html.hpp>
#include <nall/decode/inflate.hpp>
#include <nall/decode/png.hpp>
#include <nall/decode/url.hpp>
#include <nall/decode/zip.hpp>
#include <nall/encode/base.hpp>
#include <nall/encode/base64.hpp>
#include <nall/encode/html.hpp>
#include <nall/encode/url.hpp>
#include <nall/encode/zip.hpp>
#include <nall/hash/crc16.hpp>
#include <nall/hash/crc32.hpp>
#include <nall/hash/crc64.hpp>
#include <nall/hash/sha256.hpp>

#if defined(PLATFORM_WINDOWS)
  #include <nall/windows/registry.hpp>
  #include <nall/windows/utf8.hpp>
#endif

#if defined(API_POSIX)
  #include <nall/serial.hpp>
#endif
