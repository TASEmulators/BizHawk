#pragma once

#include <nall/file.hpp>
#include <nall/maybe.hpp>
#include <nall/string.hpp>
#if false
#include <libchdr/chd.h>
#endif

namespace nall::Decode {

struct CHD {
  ~CHD();
  struct Index {
    auto sectorCount() const -> u32;

    u8 number = 0xff; //00-99
    s32 lba = -1;
    s32 end = -1;
    s32 chd_lba = -1;
  };

  struct Track {
    auto sectorCount() const -> u32;

    u8 number = 0xff; //01-99
    string type;
    vector<Index> indices;
    maybe<s32> pregap;
    maybe<s32> postgap;
  };

  auto load(const string& location) -> bool;
  auto read(u32 sector) const -> vector<u8>;
  auto sectorCount() const -> u32;

  vector<Track> tracks;
private:
  file_buffer fp;
#if false
  chd_file* chd = nullptr;
#endif
  static constexpr int chd_sector_size = 2352 + 96;
  size_t chd_hunk_size;
  mutable vector<u8> chd_hunk_buffer;
  mutable int chd_current_hunk = -1;
};

inline CHD::~CHD() {
#if false
  if (chd != nullptr) {
     chd_close(chd);
  }
#endif
}

inline auto CHD::load(const string& location) -> bool {
  fp = file::open(location, file::mode::read);
  if(!fp) {
    print("CHD: Failed to open ", location, "\n");
    return false;
  }

  return false;
#if false
  chd_error err = chd_open_file(fp.handle(), CHD_OPEN_READ, nullptr, &chd);
  if (err != CHDERR_NONE) {
    print("CHD: Failed to open ", location, ": ", chd_error_string(err), "\n");
    return false;
  }

  const chd_header* header = chd_get_header(chd);
  chd_hunk_size = header->hunkbytes;

  if ((chd_hunk_size % chd_sector_size) != 0) {
    print("CHD: hunk size (", chd_hunk_size, ") is not a multiple of ", chd_sector_size, "\n");
    return false;
  }

  chd_hunk_buffer.resize(chd_hunk_size);
  u32 disc_lba = 0;
  u32 chd_lba = 0;

  // Fetch track structure
  while(true) {
    char metadata[256];
    char type[256];
    char subtype[256];
    char pgtype[256];
    char pgsub[256];
    u32 metadata_size;

    int track_no;
    int frames;
    int pregap_frames;
    int postgap_frames;

    // First, attempt to fetch CDROMv2 metadata
    err = chd_get_metadata(chd, CDROM_TRACK_METADATA2_TAG, tracks.size(), metadata, sizeof(metadata), &metadata_size, nullptr, nullptr);
    if (err == CHDERR_NONE) {
      if (std::sscanf(metadata, CDROM_TRACK_METADATA2_FORMAT, &track_no, type, subtype, &frames, &pregap_frames, pgtype, pgsub, &postgap_frames) != 8) {
        print("CHD: Invalid track v2 metadata: ", metadata,  "\n");
        return false;
      }
    } else {
      // That failed, so try to fetch CDROM (old) metadata
      err = chd_get_metadata(chd, CDROM_TRACK_METADATA_TAG, tracks.size(), metadata, sizeof(metadata),  &metadata_size, nullptr, nullptr);
      if (err != CHDERR_NONE) {
        // Both meta-data types failed to fetch, so assume there are no further tracks
        break;
      }

      if (std::sscanf(metadata, CDROM_TRACK_METADATA_FORMAT, &track_no, type, subtype, &frames) != 4) {
        print("CHD: Invalid track metadata: ", metadata, "\n");
        return false;
      }
    }

    // We currently only support RAW and audio tracks; log an error and exit if we see anything different
    auto typeStr = string{type};
    if (!(typeStr.find("_RAW") || typeStr.find("AUDIO") || typeStr.find("MODE1"))) {
      print("CHD: Unsupported track type: ", type, "\n");
      return false;
    }

    const bool pregap_in_file = (pregap_frames > 0 && pgtype[0] == 'V');

    // First track should have 2 second pregap as standard
    if(track_no == 1 && !pregap_in_file) pregap_frames = 2 * 75;

    // Add the new track
    Track track;
    track.number = track_no;
    track.type = type;
    track.pregap = pregap_frames;
    track.postgap = postgap_frames;

    // index0 = Pregap
    if (pregap_frames > 0) {
      Index index;
      index.number = 0;
      index.lba = disc_lba;
      index.end = disc_lba + pregap_frames - 1;

      if (pregap_in_file) {
        if (pregap_frames > frames) {
          print("CHD: pregap length ", pregap_frames, " exceeds track length ", frames, "\n");
          return false;
        }

        index.chd_lba = chd_lba;
        chd_lba += pregap_frames;
        frames -= pregap_frames;
      }

      disc_lba += pregap_frames;
      track.indices.append(index);
    }

    // index1 = track data
    {
      Index index;
      index.number = 1;
      index.lba = disc_lba;
      index.end = disc_lba + frames - 1;
      index.chd_lba = chd_lba;
      track.indices.append(index);
      disc_lba += frames;
      chd_lba += frames;

      // chdman pads each track to a 4-frame boundary
      chd_lba = (chd_lba + 3) / 4 * 4;
    }

    // index2 = postgap
    if (postgap_frames > 0) {
      Index index;
      index.number = 2;
      index.lba = disc_lba;
      index.end = disc_lba + postgap_frames - 1;
      track.indices.append(index);
      disc_lba += postgap_frames;
    }

    tracks.append(track);
  }

  return true;
#endif
}

inline auto CHD::read(u32 sector) const -> vector<u8> {
  // Convert LBA in CD-ROM to LBA in CHD
#if false
  for(auto& track : tracks) {
    for(auto& index : track.indices) {
      if (sector >= index.lba && sector <= index.end) {
        auto chd_lba = (sector - index.lba) + index.chd_lba;

        vector<u8> output;
        output.resize(track.type == "MODE1" ? 2048 : 2352);

        int hunk = (chd_lba * chd_sector_size) / chd_hunk_size;
        int offset = (chd_lba * chd_sector_size) % chd_hunk_size;

        if (hunk != chd_current_hunk) {
          chd_read(chd, hunk, chd_hunk_buffer.data());
          chd_current_hunk = hunk;
        }

        // Audio data is in big-endian, so we need to byteswap
        if (track.type == "AUDIO") {
          u8* src_ptr = chd_hunk_buffer.data() + offset;
          u8* dst_ptr = output.data();
          const int value_count = 2352 / sizeof(uint16_t);
          for (int i = 0; i < value_count; i++) {
            u16 value;
            memcpy(&value, src_ptr, sizeof(value));
            value = (value << 8) | (value >> 8);
            memcpy(dst_ptr, &value, sizeof(value));
            src_ptr += sizeof(value);
            dst_ptr += sizeof(value);
          }
        } else {
          std::copy(chd_hunk_buffer.data() + offset, chd_hunk_buffer.data() + offset + output.size(), output.data());
        }

        return output;
      }
    }
  }

  print("CHD: Attempting to read from unmapped sector ", sector, "\n");
#endif
  return {};
}

inline auto CHD::sectorCount() const -> u32 {
  u32 count = 0;
  for(auto& track : tracks) count += track.sectorCount();
  return count;
}

inline auto CHD::Track::sectorCount() const -> u32 {
  u32 count = 0;
  for(auto& index : indices) count += index.sectorCount();
  return count;
}

inline auto CHD::Index::sectorCount() const -> u32 {
  if(end < 0) return 0;
  return end - lba + 1;
}

}
