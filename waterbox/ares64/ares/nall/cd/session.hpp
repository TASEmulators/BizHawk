#pragma once

//subchannel processor
//note: this code is not tolerant to subchannel data that violates the Redbook standard

namespace nall::CD {

enum : s32 { InvalidLBA = 100 * 60 * 75 };

struct BCD {
  static auto encode(u8 value) -> u8 { return value / 10 << 4 | value % 10; }
  static auto decode(u8 value) -> u8 { return (value >> 4) * 10 + (value & 15); }
};

struct MSF {
  u8 minute;        //00-99
  u8 second;        //00-59
  u8 frame = 0xff;  //00-74

  MSF() = default;
  MSF(u8 m, u8 s, u8 f) : minute(m), second(s), frame(f) {}
  MSF(s32 lba) { *this = fromLBA(lba); }

  explicit operator bool() const {
    return minute <= 99 && second <= 59 && frame <= 74;
  }

  static auto fromBCD(u8 minute, u8 second, u8 frame) -> MSF {
    return {BCD::decode(minute), BCD::decode(second), BCD::decode(frame)};
  }

  static auto fromLBA(s32 lba) -> MSF {
    if(lba < 0) lba = 100 * 60 * 75 + lba;
    if(lba >= 100 * 60 * 75) return {};
    u8 minute = lba / 75 / 60 % 100;
    u8 second = lba / 75 % 60;
    u8 frame  = lba % 75;
    return {minute, second, frame};
  }

  auto toLBA() const -> s32 {
    s32 lba = minute * 60 * 75 + second * 75 + frame;
    if(minute < 90) return lba;
    return -(100 * 60 * 75 - lba);
  }

  //for debugging purposes
  auto toString() const -> string {
    if(!operator bool()) return "??:??:??";
    return {pad(minute, 2, '0'), ":", pad(second, 2, '0'), ":", pad(frame, 2, '0')};
  }
};

struct Index {
  s32 lba = InvalidLBA;
  s32 end = InvalidLBA;  //inclusive range

  explicit operator bool() const {
    return lba != InvalidLBA;
  }

  auto inRange(s32 sector) const -> bool {
    if(lba == InvalidLBA || end == InvalidLBA) return false;
    return sector >= lba && sector <= end;
  }
};

struct Track {
  u8 control = 0b1111;  //4-bit
  Index indices[100];
  u8 firstIndex = 0xff;
  u8 lastIndex  = 0xff;

  explicit operator bool() const {
    return (bool)indices[1];
  }

  auto emphasis() const -> bool {
    return control & 1;
  }

  auto copyable() const -> bool {
    return control & 2;
  }

  auto channels() const -> u32 {
    if((control & 0b1100) == 0b0000) return 2;
    if((control & 0b1100) == 0b1000) return 4;
    return 0;  //data track or reserved
  }

  auto pregap() const -> s32 {
    if(!indices[0] || !indices[1]) return InvalidLBA;
    return indices[1].lba - indices[0].lba;
  }

  auto isAudio() const -> bool {
    return channels() != 0;
  }

  auto isData() const -> bool {
    return (control & 0b1100) == 0b0100;
  }

  auto inIndex(s32 lba) const -> maybe<u8> {
    for(u8 index : range(100)) {
      if(indices[index].inRange(lba)) return index;
    }
    return {};
  }

  auto inRange(s32 lba) const -> bool {
    if(firstIndex > 99 || lastIndex > 99) return false;
    return lba >= indices[firstIndex].lba && lba <= indices[lastIndex].end;
  }

  auto index(u8 indexID) -> maybe<Index&> {
    if(indexID < 100 && indices[indexID]) return indices[indexID];
    return {};
  }
};

struct Session {
  Index leadIn;       //00
  Track tracks[100];  //01-99
  Index leadOut;      //aa
  u8 firstTrack = 0xff;
  u8 lastTrack  = 0xff;

  auto inLeadIn(s32 lba) const -> bool {
    return leadIn && lba <= leadIn.end;
  }

  auto inTrack(s32 lba) const -> maybe<u8> {
    for(u8 trackID : range(99)) {
      auto& track = tracks[trackID+1];
      if(track && track.inRange(lba)) return trackID+1;
    }
    return {};
  }

  auto inLeadOut(s32 lba) const -> bool {
    return leadOut && lba >= leadOut.lba;
  }

  auto track(u8 trackID) -> maybe<Track&> {
    if(trackID >= 1 && trackID < 100 && tracks[trackID]) return tracks[trackID];
    return {};
  }

  auto encode(u32 sectors) const -> vector<u8> {
    if(sectors < abs(leadIn.lba) + leadOut.lba) return {};  //not enough sectors

    vector<u8> data;
    data.resize(sectors * 96 + 96);  //add one sector for P shift

    auto toP = [&](s32 lba) -> array_span<u8> {
      //P is encoded one sector later than Q
      return {&data[(lba + abs(leadIn.lba) + 1) * 96], 12};
    };

    auto toQ = [&](s32 lba) -> array_span<u8> {
      return {&data[(lba + abs(leadIn.lba)) * 96 + 12], 12};
    };

    //lead-in
    s32 lba = leadIn.lba;
    while(lba < 0) {
      //tracks
      for(u32 trackID : range(100)) {
      for(u32 repeat : range(3)) {
        auto& track = tracks[trackID];
        if(!track) continue;
        auto q = toQ(lba);
        q[0] = track.control << 4 | 1;
        q[1] = 0x00;
        q[2] = BCD::encode(trackID);
        auto msf = MSF(lba);
        q[3] = BCD::encode(msf.minute);
        q[4] = BCD::encode(msf.second);
        q[5] = BCD::encode(msf.frame);
        q[6] = 0x00;
        msf = MSF(track.indices[1].lba);
        q[7] = BCD::encode(msf.minute);
        q[8] = BCD::encode(msf.second);
        q[9] = BCD::encode(msf.frame);
        auto crc16 = CRC16({q.data(), 10});
        q[10] = crc16 >> 8;
        q[11] = crc16 >> 0;
        if(++lba >= 0) break;
      } if(  lba >= 0) break;
      } if(  lba >= 0) break;

      //first track
      for(u32 repeat : range(3)) {
        auto q = toQ(lba);
        q[0] = 0x01;  //control value unverified; address = 1
        q[1] = 0x00;  //track# = 00 (TOC)
        q[2] = 0xa0;  //first track
        auto msf = MSF(lba);
        q[3] = BCD::encode(msf.minute);
        q[4] = BCD::encode(msf.second);
        q[5] = BCD::encode(msf.frame);
        q[6] = 0x00;
        q[7] = BCD::encode(firstTrack);
        q[8] = 0x00;
        q[9] = 0x00;
        auto crc16 = CRC16({q.data(), 10});
        q[10] = crc16 >> 8;
        q[11] = crc16 >> 0;
        if(++lba >= 0) break;
      } if(  lba >= 0) break;

      //last track
      for(u32 repeat : range(3)) {
        auto q = toQ(lba);
        q[0] = 0x01;
        q[1] = 0x00;
        q[2] = 0xa1;  //last track
        auto msf = MSF(lba);
        q[3] = BCD::encode(msf.minute);
        q[4] = BCD::encode(msf.second);
        q[5] = BCD::encode(msf.frame);
        q[6] = 0x00;
        q[7] = BCD::encode(lastTrack);
        q[8] = 0x00;
        q[9] = 0x00;
        auto crc16 = CRC16({q.data(), 10});
        q[10] = crc16 >> 8;
        q[11] = crc16 >> 0;
        if(++lba >= 0) break;
      } if(  lba >= 0) break;

      //lead-out point
      for(u32 repeat : range(3)) {
        auto q = toQ(lba);
        q[0] = 0x01;
        q[1] = 0x00;
        q[2] = 0xa2;  //lead-out point
        auto msf = MSF(lba);
        q[3] = BCD::encode(msf.minute);
        q[4] = BCD::encode(msf.second);
        q[5] = BCD::encode(msf.frame);
        q[6] = 0x00;
        msf = MSF(leadOut.lba);
        q[7] = BCD::encode(msf.minute);
        q[8] = BCD::encode(msf.second);
        q[9] = BCD::encode(msf.frame);
        auto crc16 = CRC16({q.data(), 10});
        q[10] = crc16 >> 8;
        q[11] = crc16 >> 0;
        if(++lba >= 0) break;
      } if(  lba >= 0) break;
    }

    //tracks
    s32 end = leadOut.lba;
    for(u8 trackID : reverse(range(100))) {
      auto& track = tracks[trackID];
      if(!track) continue;

      //indices
      for(u8 indexID : reverse(range(100))) {
        auto& index = track.indices[indexID];
        if(!index) continue;

        for(s32 lba = index.lba; lba < end; lba++) {
          auto p = toP(lba);
          u8 byte = indexID == 0 ? 0xff : 0x00;
          for(u32 index : range(12)) p[index] = byte;

          auto q = toQ(lba);
          q[0] = track.control << 4 | 1;
          q[1] = BCD::encode(trackID);
          q[2] = BCD::encode(indexID);
          auto msf = MSF(lba - track.indices[1].lba);
          q[3] = BCD::encode(msf.minute);
          q[4] = BCD::encode(msf.second);
          q[5] = BCD::encode(msf.frame);
          q[6] = 0x00;
          msf = MSF(lba);
          q[7] = BCD::encode(msf.minute);
          q[8] = BCD::encode(msf.second);
          q[9] = BCD::encode(msf.frame);
          auto crc16 = CRC16({q.data(), 10});
          q[10] = crc16 >> 8;
          q[11] = crc16 >> 0;
        }

        end = index.lba;
      }
    }

    //pre-lead-out (2-3s at the end of last track)
    for(auto i : range(150)) {
      auto p = toP(leadOut.lba - 150 + i);
      for(auto sig : range(12)) {
        p[sig]= 0xff;
    }}

    //lead-out
    for(s32 lba : range(sectors - abs(leadIn.lba) - leadOut.lba)) {
      auto p = toP(leadOut.lba + lba);
      u8 byte;
      if(lba < 150) {
        //2s start (standard specifies 2-3s start)
        byte = 0x00;
      } else {
        //2hz duty cycle; rounded downward (standard specifies 2% tolerance)
        byte = (lba - 150) / (75 >> 1) & 1 ? 0x00 : 0xff;
      }
      for(u32 index : range(12)) p[index] = byte;

      auto q = toQ(leadOut.lba + lba);
      q[0] = 0x01;
      q[1] = 0xaa;  //lead-out track#
      q[2] = 0x01;  //lead-out index#
      auto msf = MSF(lba);
      q[3] = BCD::encode(msf.minute);
      q[4] = BCD::encode(msf.second);
      q[5] = BCD::encode(msf.frame);
      q[6] = 0x00;
      msf = MSF(leadOut.lba + lba);
      q[7] = BCD::encode(msf.minute);
      q[8] = BCD::encode(msf.second);
      q[9] = BCD::encode(msf.frame);
      auto crc16 = CRC16({q.data(), 10});
      q[10] = crc16 >> 8;
      q[11] = crc16 >> 0;
    }

    data.resize(data.size() - 96);  //remove padding for P shift
    return data;
  }

  auto decode(array_view<u8> data, u32 size, u32 leadOutSectors = 0) -> bool {
    *this = {};  //reset session
    //three data[] types supported: subcode Q only, subcode P-W only, data+subcode complete image
    if(size != 12 && size != 96 && size != 2448) return false;

    //determine lead-in sector count
    leadIn.lba = InvalidLBA;
    for(s32 lba : range(7500)) {  //7500 max sectors scanned
      u32 offset = lba * size;
      if(size ==   96) offset += 12;
      if(size == 2448) offset += 12 + 2352;
      if(offset + 12 > data.size()) break;
      auto q = array_view<u8>{&data[offset], 12};
      auto crc16 = CRC16({q.data(), 10});
      if(q[10] != u8(crc16 >> 8)) continue;
      if(q[11] != u8(crc16 >> 0)) continue;

      u8 control = q[0] >> 4;
      u8 address = q[0] & 15;
      u8 trackID = q[1];
      if(address != 1) continue;
      if(trackID != 0) continue;

      leadIn.lba = lba - 7500;
      break;
    }
    if(leadIn.lba == InvalidLBA || leadIn.lba >= 0) return false;

    auto toQ = [&](s32 lba) -> array_view<u8> {
      u32 offset = (lba + abs(leadIn.lba)) * size;
      if(size ==   96) offset += 12;
      if(size == 2448) offset += 12 + 2352;
      if(offset + 12 > data.size()) return {};
      return {&data[offset], 12};
    };

    //lead-in
    leadOut.lba = InvalidLBA;
    for(s32 lba = leadIn.lba; lba < 0; lba++) {
      auto q = toQ(lba);
      if(!q) break;
      auto crc16 = CRC16({q.data(), 10});
      if(q[10] != u8(crc16 >> 8)) continue;
      if(q[11] != u8(crc16 >> 0)) continue;

      u8 control = q[0] >> 4;
      u8 address = q[0] & 15;
      u8 trackID = q[1];
      if(address != 1) continue;
      if(trackID != 0) continue;

      trackID = BCD::decode(q[2]);

      if(trackID <=  99) {  //00-99
        auto& track = tracks[trackID];
        track.control = control;
        track.indices[1].lba = MSF::fromBCD(q[7], q[8], q[9]).toLBA();
      }

      if(trackID == 100) {  //a0
        firstTrack = BCD::decode(q[7]);
      }

      if(trackID == 101) {  //a1
        lastTrack = BCD::decode(q[7]);
      }

      if(trackID == 102) {  //a2
        leadOut.lba = MSF::fromBCD(q[7], q[8], q[9]).toLBA();
      }
    }
    if(leadOut.lba == InvalidLBA) return false;

    //tracks
    for(s32 lba = 0; lba < leadOut.lba; lba++) {
      auto q = toQ(lba);
      if(!q) break;
      auto crc16 = CRC16({q.data(), 10});
      if(q[10] != u8(crc16 >> 8)) continue;
      if(q[11] != u8(crc16 >> 0)) continue;

      u8 control = q[0] >> 4;
      u8 address = q[0] & 15;
      u8 trackID = BCD::decode(q[1]);
      u8 indexID = BCD::decode(q[2]);
      if(address != 1) continue;
      if(trackID == 0 || trackID > 99) continue;
      if(indexID > 99) continue;

      auto& track = tracks[trackID];
      if(!track) continue;  //track not found?
      auto& index = track.indices[indexID];
      if(index) continue;   //index already decoded?

      index.lba = MSF::fromBCD(q[7], q[8], q[9]).toLBA();
    }

    synchronize(leadOutSectors);
    return true;
  }

  //calculates Index::end variables:
  //needed for Session::isTrack() and Track::isIndex() to function.
  auto synchronize(u32 leadOutSectors = 0) -> void {
    leadIn.end = -1;
    s32 end = leadOut.lba - 1;
    for(u32 trackID : reverse(range(100))) {
      auto& track = tracks[trackID];
      if(!track) continue;

      for(u32 indexID : reverse(range(100))) {
        auto& index = track.indices[indexID];
        if(!index) continue;

        index.end = end;
        end = index.lba - 1;
      }

      for(u32 indexID : range(100)) {
        auto& index = track.indices[indexID];
        if(index) { track.firstIndex = indexID; break; }
      }

      for(u32 indexID : reverse(range(100))) {
        auto& index = track.indices[indexID];
        if(index) { track.lastIndex = indexID; break; }
      }
    }
    leadOut.end = leadOut.lba + leadOutSectors - 1;
  }

  //for diagnostic use only
  auto serialize() const -> string {
    string s;
    s.append("session\n");
    s.append("  leadIn: ");
    s.append(MSF(leadIn.lba).toString(), " - ", MSF(leadIn.end).toString(), "\n");
    for(u32 trackID : range(100)) {
      auto& track = tracks[trackID];
      if(!track) continue;
      s.append("  track", pad(trackID, 2, '0'));
      if(trackID == firstTrack) s.append(" first");
      if(trackID ==  lastTrack) s.append( " last");
      s.append("\n");
      s.append("    control: ", binary(track.control, 4, '0'), "\n");
      for(u32 indexID : range(100)) {
        auto& index = track.indices[indexID];
        if(!index) continue;
        s.append("    index", pad(indexID, 2, '0'), ": ");
        s.append(MSF(index.lba).toString(), " - ", MSF(index.end).toString(), "\n");
      }
    }
    s.append("  leadOut: ");
    s.append(MSF(leadOut.lba).toString(), " - ", MSF(leadOut.end).toString(), "\n");
    return s;
  }
};

}
