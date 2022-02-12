#pragma once

namespace nall::CD::Sync {

inline auto create(array_span<u8> sector) -> bool {
  if(sector.size() != 12 && sector.size() != 2352) return false;

  for(u32 n : range(12)) {
    sector[n] = ((n == 0 || n == 11) ? 0x00 : 0xff);
  }

  return true;
}

//

inline auto verify(array_view<u8> sector) -> bool {
  if(sector.size() != 12 && sector.size() != 2352) return false;

  for(u32 n : range(12)) {
    if(sector[n] != ((n == 0 || n == 11) ? 0x00 : 0xff)) return false;
  }

  return true;
}

}
