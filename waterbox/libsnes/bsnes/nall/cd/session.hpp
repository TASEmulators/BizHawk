#pragma once

//subchannel processor
//note: this code is not tolerant to subchannel data that violates the Redbook standard

namespace nall::CD {

enum : int { InvalidLBA = 100 * 60 * 75 };

struct BCD {
  static auto encode(uint8_t value) -> uint8_t { return value / 10 << 4 | value % 10; }
  static auto decode(uint8_t value) -> uint8_t { return (value >> 4) * 10 + (value & 15); }
};

struct MSF {
  uint8_t minute;      //00-99
  uint8_t second;      //00-59
  uint8_t frame = -1;  //00-74

  MSF() = default;
  MSF(uint8_t m, uint8_t s, uint8_t f) : minute(m), second(s), frame(f) {}
  MSF(int lba) { *this = fromLBA(lba); }

  explicit operator bool() const {
    return minute <= 99 && second <= 59 && frame <= 74;
  }

  static auto fromBCD(uint8_t minute, uint8_t second, uint8_t frame) -> MSF {
    return {BCD::decode(minute), BCD::decode(second), BCD::decode(frame)};
  }

  static auto fromLBA(int lba) -> MSF {
    if(lba < 0) lba = 100 * 60 * 75 + lba;
    if(lba >= 100 * 60 * 75) return {};
    uint8_t minute = lba / 75 / 60 % 100;
    uint8_t second = lba / 75 % 60;
    uint8_t frame  = lba % 75;
    return {minute, second, frame};
  }

  auto toLBA() const -> int {
    int lba = minute * 60 * 75 + second * 75 + frame;
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
  int lba = InvalidLBA;
  int end = InvalidLBA;  //inclusive range

  explicit operator bool() const {
    return lba != InvalidLBA;
  }

  auto inRange(int sector) const -> bool {
    if(lba == InvalidLBA || end == InvalidLBA) return false;
    return sector >= lba && sector <= end;
  }
};

struct Track {
  uint8_t control = 0b1111;  //4-bit
  uint8_t address = 0b1111;  //4-bit
  Index indices[100];
  uint8_t firstIndex = -1;
  uint8_t lastIndex  = -1;

  explicit operator bool() const {
    return (bool)indices[1];
  }

  auto emphasis() const -> bool {
    return control & 1;
  }

  auto copyable() const -> bool {
    return control & 2;
  }

  auto channels() const -> uint {
    if((control & 0b1100) == 0b0000) return 2;
    if((control & 0b1100) == 0b1000) return 4;
    return 0;  //data track or reserved
  }

  auto pregap() const -> int {
    if(!indices[0] || !indices[1]) return InvalidLBA;
    return indices[1].lba - indices[0].lba;
  }

  auto isAudio() const -> bool {
    return channels() != 0;
  }

  auto isData() const -> bool {
    return (control & 0b1100) == 0b0100;
  }

  auto inIndex(int lba) const -> maybe<uint8_t> {
    for(uint8_t index : range(100)) {
      if(indices[index].inRange(lba)) return index;
    }
    return {};
  }

  auto inRange(int lba) const -> bool {
    if(firstIndex > 99 || lastIndex > 99) return false;
    return lba >= indices[firstIndex].lba && lba <= indices[lastIndex].end;
  }
};

struct Session {
  Index leadIn;       //00
  Track tracks[100];  //01-99
  Index leadOut;      //aa
  uint8_t firstTrack = -1;
  uint8_t lastTrack  = -1;

  auto inLeadIn(int lba) const -> bool {
    return lba < 0;
  }

  auto inTrack(int lba) const -> maybe<uint8_t> {
    for(uint8_t trackID : range(100)) {
      auto& track = tracks[trackID];
      if(track && track.inRange(lba)) return trackID;
    }
    return {};
  }

  auto inLeadOut(int lba) const -> bool {
    return lba >= leadOut.lba;
  }

  auto encode(uint sectors) const -> vector<uint8_t> {
    if(sectors < abs(leadIn.lba) + leadOut.lba) return {};  //not enough sectors

    vector<uint8_t> data;
    data.resize(sectors * 96 + 96);  //add one sector for P shift

    auto toP = [&](int lba) -> array_span<uint8_t> {
      //P is encoded one sector later than Q
      return {&data[(lba + abs(leadIn.lba) + 1) * 96], 12};
    };

    auto toQ = [&](int lba) -> array_span<uint8_t> {
      return {&data[(lba + abs(leadIn.lba)) * 96 + 12], 12};
    };

    //lead-in
    int lba = leadIn.lba;
    while(lba < 0) {
      //tracks
      for(uint trackID : range(100)) {
      for(uint repeat : range(3)) {
        auto& track = tracks[trackID];
        if(!track) continue;
        auto q = toQ(lba);
        q[0] = track.control << 4 | track.address << 0;
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
        auto crc16 = CRC16({q, 10});
        q[10] = crc16 >> 8;
        q[11] = crc16 >> 0;
        if(++lba >= 0) break;
      }}if(  lba >= 0) break;

      //first track
      for(uint repeat : range(3)) {
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
        auto crc16 = CRC16({q, 10});
        q[10] = crc16 >> 8;
        q[11] = crc16 >> 0;
        if(++lba >= 0) break;
      } if(  lba >= 0) break;

      //last track
      for(uint repeat : range(3)) {
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
        auto crc16 = CRC16({q, 10});
        q[10] = crc16 >> 8;
        q[11] = crc16 >> 0;
        if(++lba >= 0) break;
      } if(  lba >= 0) break;

      //lead-out point
      for(uint repeat : range(3)) {
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
        auto crc16 = CRC16({q, 10});
        q[10] = crc16 >> 8;
        q[11] = crc16 >> 0;
        if(++lba >= 0) break;
      } if(  lba >= 0) break;
    }

    //tracks
    int end = leadOut.lba;
    for(uint8_t trackID : reverse(range(100))) {
      auto& track = tracks[trackID];
      if(!track) continue;

      //indices
      for(uint8_t indexID : reverse(range(100))) {
        auto& index = track.indices[indexID];
        if(!index) continue;

        for(int lba = index.lba; lba < end; lba++) {
          auto p = toP(lba);
          uint8_t byte = indexID == 0 ? 0xff : 0x00;
          for(uint index : range(12)) p[index] = byte;

          auto q = toQ(lba);
          q[0] = track.control << 4 | track.address << 0;
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
          auto crc16 = CRC16({q, 10});
          q[10] = crc16 >> 8;
          q[11] = crc16 >> 0;
        }

        end = index.lba;
      }
    }

    //lead-out
    for(int lba : range(sectors - abs(leadIn.lba) - leadOut.lba)) {
      auto p = toP(leadOut.lba + lba);
      uint8_t byte;
      if(lba < 150) {
        //2s start (standard specifies 2-3s start)
        byte = 0x00;
      } else {
        //2hz duty cycle; rounded downward (standard specifies 2% tolerance)
        byte = (lba - 150) / (75 >> 1) & 1 ? 0xff : 0x00;
      }
      for(uint index : range(12)) p[index] = byte;

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
      auto crc16 = CRC16({q, 10});
      q[10] = crc16 >> 8;
      q[11] = crc16 >> 0;
    }

    data.resize(data.size() - 96);  //remove padding for P shift
    return data;
  }

  auto decode(array_view<uint8_t> data, uint size, uint leadOutSectors = 0) -> bool {
    *this = {};  //reset session
    //three data[] types supported: subcode Q only, subcode P-W only, data+subcode complete image
    if(size != 12 && size != 96 && size != 2448) return false;

    //determine lead-in sector count
    for(int lba : range(7500)) {  //7500 max sectors scanned
      uint offset = lba * size;
      if(size ==   96) offset += 12;
      if(size == 2448) offset += 12 + 2352;
      if(offset + 12 > data.size()) break;
      auto q = array_view<uint8_t>{&data[offset], 12};
      auto crc16 = CRC16({q, 10});
      if(q[10] != uint8_t(crc16 >> 8)) continue;
      if(q[11] != uint8_t(crc16 >> 0)) continue;

      uint8_t control = q[0] >> 4;
      uint8_t address = q[0] & 15;
      uint8_t trackID = q[1];
      if(address != 1) continue;
      if(trackID != 0) continue;

      auto msf = MSF::fromBCD(q[3], q[4], q[5]);
      leadIn.lba = msf.toLBA() - lba;
      break;
    }
    if(leadIn.lba == InvalidLBA || leadIn.lba >= 0) return false;

    auto toQ = [&](int lba) -> array_view<uint8_t> {
      uint offset = (lba + abs(leadIn.lba)) * size;
      if(size ==   96) offset += 12;
      if(size == 2448) offset += 12 + 2352;
      if(offset + 12 > data.size()) return {};
      return {&data[offset], 12};
    };

    //lead-in
    for(int lba = leadIn.lba; lba < 0; lba++) {
      auto q = toQ(lba);
      if(!q) break;
      auto crc16 = CRC16({q, 10});
      if(q[10] != uint8_t(crc16 >> 8)) continue;
      if(q[11] != uint8_t(crc16 >> 0)) continue;

      uint8_t control = q[0] >> 4;
      uint8_t address = q[0] & 15;
      uint8_t trackID = q[1];
      if(address != 1) continue;
      if(trackID != 0) continue;

      trackID = BCD::decode(q[2]);

      if(trackID <=  99) {  //00-99
        auto& track = tracks[trackID];
        track.control = control;
        track.address = address;
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
    for(int lba = 0; lba < leadOut.lba; lba++) {
      auto q = toQ(lba);
      if(!q) break;
      auto crc16 = CRC16({q, 10});
      if(q[10] != uint8_t(crc16 >> 8)) continue;
      if(q[11] != uint8_t(crc16 >> 0)) continue;

      uint8_t control = q[0] >> 4;
      uint8_t address = q[0] & 15;
      uint8_t trackID = BCD::decode(q[1]);
      uint8_t indexID = BCD::decode(q[2]);
      if(address != 1) continue;
      if(trackID > 99) continue;
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
  auto synchronize(uint leadOutSectors = 0) -> void {
    leadIn.end = -1;
    int end = leadOut.lba - 1;
    for(uint trackID : reverse(range(100))) {
      auto& track = tracks[trackID];
      if(!track) continue;

      for(uint indexID : reverse(range(100))) {
        auto& index = track.indices[indexID];
        if(!index) continue;

        index.end = end;
        end = index.lba - 1;
      }

      for(uint indexID : range(100)) {
        auto& index = track.indices[indexID];
        if(index) { track.firstIndex = indexID; break; }
      }

      for(uint indexID : reverse(range(100))) {
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
    for(uint trackID : range(100)) {
      auto& track = tracks[trackID];
      if(!track) continue;
      s.append("  track", pad(trackID, 2, '0'));
      if(trackID == firstTrack) s.append(" first");
      if(trackID ==  lastTrack) s.append( " last");
      s.append("\n");
      s.append("    control: ", binary(track.control, 4, '0'), "\n");
      s.append("    address: ", binary(track.address, 4, '0'), "\n");
      for(uint indexID : range(100)) {
        auto& index = track.indices[indexID];
        if(!index) continue;
        s.append("    index", pad(indexID, 2, '0'), ": ");
        s.append(MSF(index.lba).toString(), " - ", MSF(index.end).toString(), "\n");
      }
    }
    s.append("  leadout: ");
    s.append(MSF(leadOut.lba).toString(), " - ", MSF(leadOut.end).toString(), "\n");
    return s;
  }
};

}
