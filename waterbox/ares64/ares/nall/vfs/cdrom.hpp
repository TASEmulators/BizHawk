#pragma once

#include <nall/array-span.hpp>
#include <nall/cd.hpp>
#include <nall/file.hpp>
#include <nall/string.hpp>
#include <nall/decode/cue.hpp>
#include <nall/decode/chd.hpp>
#include <nall/decode/wav.hpp>

namespace nall::vfs {

struct cdrom : file {
  ~cdrom() {
    _thread.join();
  }

  static auto open(const string& location) -> shared_pointer<cdrom> {
    auto instance = shared_pointer<cdrom>{new cdrom};
    if(location.iendsWith(".cue") && instance->loadCue(location)) return instance;
    if(location.iendsWith(".chd") && instance->loadChd(location)) return instance;
    return {};
  }

  auto writable() const -> bool override { return false; }
  auto data() const -> const u8* override { wait(size()); return _image.data(); }
  auto data() -> u8* override { wait(size()); return _image.data(); }
  auto size() const -> u64 override { return _image.size(); }
  auto offset() const -> u64 override { return _offset; }

  auto resize(u64 size) -> bool override {
    //unsupported
    return false;
  }

  auto seek(s64 offset, index mode) -> void override {
    if(mode == index::absolute) _offset  = (u64)offset;
    if(mode == index::relative) _offset += (s64)offset;
  }

  auto read() -> u8 override {
    if(_offset >= _image.size()) return 0x00;
    wait(_offset);
    return _image[_offset++];
  }

  auto write(u8 data) -> void override {
    //CD-ROMs are read-only; but allow writing anyway if needed, since the image is in memory
    if(_offset >= _image.size()) return;
    wait(_offset);
    _image[_offset++] = data;
  }

  auto wait(u64 offset) const -> void {
    bool force = false;
    if(offset >= _image.size()) {
      offset = _image.size() - 1;
      force = true;
    }
    //subchannel data is always loaded
    if(offset % 2448 < 2352 || force) {
      while(offset + 1 > _loadOffset) usleep(1);
    }
  }

private:
  auto loadCue(const string& cueLocation) -> bool {
    auto cuesheet = shared_pointer<Decode::CUE>::create();
    if(!cuesheet->load(cueLocation)) return false;

    CD::Session session;
    session.leadIn.lba = -LeadInSectors;
    session.leadIn.end = -1;
    s32 lbaFileBase = 0;

    // add 2 sec pregap to 1st track
    if(!cuesheet->files[0].tracks[0].pregap)
      cuesheet->files[0].tracks[0].pregap = Track1Pregap;
    else
      cuesheet->files[0].tracks[0].pregap = Track1Pregap + cuesheet->files[0].tracks[0].pregap();

    if(cuesheet->files[0].tracks[0].indices[0].number == 1) {
      session.tracks[1].indices[0].lba = 0;
      session.tracks[1].indices[0].end =
          cuesheet->files[0].tracks[0].pregap() + cuesheet->files[0].tracks[0].indices[0].lba - 1;
    }

    s32 lbaIndex = 0;
    for(auto& file : cuesheet->files) {
      for(auto& track : file.tracks) {
        session.tracks[track.number].control = track.type == "audio" ? 0b0000 : 0b0100;
        if(track.pregap) lbaFileBase += track.pregap();
        for(auto& index : track.indices) {
          if(index.lba >= 0) {
            session.tracks[track.number].indices[index.number].lba = lbaFileBase + index.lba;
            session.tracks[track.number].indices[index.number].end = lbaFileBase + index.end;
            if(index.number == 0 && track.pregap) {
              session.tracks[track.number].indices[index.number].lba -= track.pregap();
              session.tracks[track.number].indices[index.number].end -= track.pregap();
            }
          } else {
            // insert gap
            session.tracks[track.number].indices[index.number].lba = lbaIndex;
            if(index.number == 0)
              session.tracks[track.number].indices[index.number].end = lbaIndex + track.pregap() - 1;
            else
              session.tracks[track.number].indices[index.number].end = lbaIndex + track.postgap() - 1;
          }
          lbaIndex = session.tracks[track.number].indices[index.number].end + 1;
        }
        if(track.postgap) lbaFileBase += track.postgap();
      }
      lbaFileBase = lbaIndex;
    }
    session.leadOut.lba = lbaFileBase;
    session.leadOut.end = lbaFileBase + LeadOutSectors - 1;

    // determine track and index ranges
    session.firstTrack = 0xff;
    for(u32 track : range(100)) {
      if(!session.tracks[track]) continue;
      if(session.firstTrack > 99) session.firstTrack = track;
      // find first index
      for(u32 indexID : range(100)) {
        auto& index = session.tracks[track].indices[indexID];
        if(index) { session.tracks[track].firstIndex = indexID; break; }
      }
      // find last index
      for(u32 indexID : reverse(range(100))) {
        auto& index = session.tracks[track].indices[indexID];
        if(index) { session.tracks[track].lastIndex = indexID; break; }
      }
      session.lastTrack = track;
    }

    _image.resize(2448 * (LeadInSectors + lbaFileBase + LeadOutSectors));

    //preload subchannel data
    loadSub({Location::notsuffix(cueLocation), ".sub"}, session);

    //load user data on separate thread
    _thread = thread::create(
    [this, cueLocation, cuesheet = std::move(cuesheet)](uintptr) -> void {

    s32 lbaFileBase = 0;
    for(auto& file : cuesheet->files) {
      auto location = string{Location::path(cueLocation), file.name};
      auto filedata = nall::file::open(location, nall::file::mode::read);
      if(file.type == "wave") filedata.seek(44);  //skip RIFF header
      for(auto& track : file.tracks) {
        if(track.pregap) lbaFileBase += track.pregap();
        for(auto& index : track.indices) {
          if(index.lba < 0) continue; // ignore gaps (not in file)
          for(s32 sector : range(index.sectorCount())) {
            auto offset = 2448ull * (LeadInSectors + lbaFileBase + index.lba + sector);
            auto target = _image.data() + offset;
            auto length = track.sectorSize();
            if(length == 2048) {
              //ISO: generate header + parity data
              memory::assign(target + 0, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff);  //sync
              memory::assign(target + 6, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00);  //sync
              auto [minute, second, frame] = CD::MSF(lbaFileBase + index.lba + sector);
              target[12] = BCD::encode(minute);
              target[13] = BCD::encode(second);
              target[14] = BCD::encode(frame);
              target[15] = 0x01;  //mode
              filedata.read({target + 16, length});
              CD::RSPC::encodeMode1({target, 2352});
            }
            if(length == 2352) {
              //BIN + WAV: direct copy
              filedata.read({target, length});
            }
            _loadOffset = offset + 2448;
          }
        }
        if(track.postgap) lbaFileBase += track.postgap();
      }
      lbaFileBase += file.tracks.last().indices.last().end + 1;
    }
    _loadOffset = _image.size();

    });

    return true;
  }

  auto loadChd(const string& location) -> bool {
    auto chd = shared_pointer<Decode::CHD>::create();
    if(!chd->load(location)) return false;

    CD::Session session;
    session.leadIn.lba = -LeadInSectors;
    session.leadIn.end = -1;

    s32 lbaIndex = 0;
    for(auto& track : chd->tracks) {
      session.tracks[track.number].control = track.type == "AUDIO" ? 0b0000 : 0b0100;
      for(auto& index : track.indices) {
        session.tracks[track.number].indices[index.number].lba = index.lba;
        session.tracks[track.number].indices[index.number].end = index.end;
        lbaIndex = session.tracks[track.number].indices[index.number].end + 1;
      }
    }

    session.leadOut.lba = lbaIndex;
    session.leadOut.end = lbaIndex + LeadOutSectors - 1;

    // determine track and index ranges
    session.firstTrack = 0xff;
    for(u32 track : range(100)) {
      if(!session.tracks[track]) continue;
      if(session.firstTrack > 99) session.firstTrack = track;
      // find first index
      for(u32 indexID : range(100)) {
        auto& index = session.tracks[track].indices[indexID];
        if(index) { session.tracks[track].firstIndex = indexID; break; }
      }
      // find last index
      for(u32 indexID : reverse(range(100))) {
        auto& index = session.tracks[track].indices[indexID];
        if(index) { session.tracks[track].lastIndex = indexID; break; }
      }
      session.lastTrack = track;
    }

    _image.resize(2448 * (LeadInSectors + lbaIndex + LeadOutSectors));

    //preload subchannel data
    loadSub({Location::notsuffix(location), ".sub"}, session);

    //load user data on separate thread
    _thread = thread::create(
    [this, chd = std::move(chd)](uintptr) -> void {

    s32 lba = 0;
    for(auto& track : chd->tracks) {
      for(auto& index : track.indices) {
        for(s32 sector : range(index.sectorCount())) {
          auto offset = 2448ull * (LeadInSectors + index.lba + sector);
          auto target = _image.data() + offset;
          auto sectorData = chd->read(lba);
          if(sectorData.size() == 2048) {
            //ISO: generate header + parity data
            memory::assign(target + 0, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff);  //sync
            memory::assign(target + 6, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00);  //sync
            auto [minute, second, frame] = CD::MSF(index.lba + sector);
            target[12] = BCD::encode(minute);
            target[13] = BCD::encode(second);
            target[14] = BCD::encode(frame);
            target[15] = 0x01;  //mode
            memory::copy(target + 16, 2048, sectorData.data(), sectorData.size());
            CD::RSPC::encodeMode1({target, 2352});
          } else {
            memory::copy(target, 2352, sectorData.data(), sectorData.size());
          }
          lba++;
          _loadOffset = offset + 2448;
        }
      }
    }
    _loadOffset = _image.size();

    });

    return true;
  }

private:
  void loadSub(const string& location, const CD::Session& session) {
    auto subchannel = session.encode(LeadInSectors + session.leadOut.end + 1);

    if(auto overlay = nall::file::read(location)) {
      auto target = subchannel.data() + 96 * (LeadInSectors + Track1Pregap);
      auto length = (s64)subchannel.size() - 96 * (LeadInSectors + Track1Pregap);
      memory::copy(target, length, overlay.data(), overlay.size());
    }

    for(u64 sector : range(size() / 2448)) {
      auto source = subchannel.data() + sector * 96;
      auto target = _image.data() + sector * 2448 + 2352;
      memory::copy(target, source, 96);
    }
  }

  vector<u8> _image;
  u64 _offset = 0;
  atomic<u64> _loadOffset = 0;
  thread _thread;

  static constexpr s32 LeadInSectors  = 7500;
  static constexpr s32 Track1Pregap   =  150;
  static constexpr s32 LeadOutSectors = 6750;
};

}
