#include <sfc/sfc.hpp>

namespace SuperFamicom {

MSU1 msu1;

#include "serialization.cpp"

auto MSU1::synchronizeCPU() -> void {
  if(clock >= 0) scheduler.resume(cpu.thread);
}


auto MSU1::Enter() -> void {
  while(true) {
    scheduler.synchronize();
    msu1.main();
  }
}

auto MSU1::main() -> void {
  double left  = 0.0;
  double right = 0.0;

  if(io.audioPlay) {
    if(audioFile) {
      if(audioFile->end()) {
        if(!io.audioRepeat) {
          io.audioPlay = false;
          audioFile->seek(io.audioPlayOffset = 8);
        } else {
          audioFile->seek(io.audioPlayOffset = io.audioLoopOffset);
        }
      } else {
        io.audioPlayOffset += 4;
        left  = (double)(int16)audioFile->readl(2) / 32768.0 * (double)io.audioVolume / 255.0;
        right = (double)(int16)audioFile->readl(2) / 32768.0 * (double)io.audioVolume / 255.0;
        if(dsp.mute()) left = 0, right = 0;
      }
    } else {
      io.audioPlay = false;
    }
  }

  if(!system.runAhead) stream->sample(float(left), float(right));
  step(1);
  synchronizeCPU();
}

auto MSU1::step(uint clocks) -> void {
  clock += clocks * (uint64_t)cpu.frequency;
}

auto MSU1::unload() -> void {
  dataFile.reset();
  audioFile.reset();
}

auto MSU1::power() -> void {
  create(MSU1::Enter, 44100);
  stream = Emulator::audio.createStream(2, frequency);

  io.dataSeekOffset = 0;
  io.dataReadOffset = 0;

  io.audioPlayOffset = 0;
  io.audioLoopOffset = 0;

  io.audioTrack = 0;
  io.audioVolume = 0;

  io.audioResumeTrack = ~0;  //no resume
  io.audioResumeOffset = 0;

  io.audioError = false;
  io.audioPlay = false;
  io.audioRepeat = false;
  io.audioBusy = false;
  io.dataBusy = false;

  dataOpen();
  audioOpen();
}

auto MSU1::dataOpen() -> void {
  dataFile.reset();
  string name = {"msu1/data.rom"};
  if(dataFile = platform->open(ID::SuperFamicom, name, File::Read)) {
    dataFile->seek(io.dataReadOffset);
  }
}

auto MSU1::audioOpen() -> void {
  audioFile.reset();
  string name = {"msu1/track-", io.audioTrack, ".pcm"};
  if(audioFile = platform->open(ID::SuperFamicom, name, File::Read)) {
    if(audioFile->size() >= 8) {
      uint32 header = audioFile->readm(4);
      if(header == 0x4d535531) {  //"MSU1"
        io.audioLoopOffset = 8 + audioFile->readl(4) * 4;
        if(io.audioLoopOffset > audioFile->size()) io.audioLoopOffset = 8;
        io.audioError = false;
        audioFile->seek(io.audioPlayOffset);
        return;
      }
    }
    audioFile.reset();
  }
  io.audioError = true;
}

auto MSU1::readIO(uint addr, uint8) -> uint8 {
  cpu.synchronizeCoprocessors();

  switch(0x2000 | addr & 7) {
  case 0x2000:
    return (
      Revision       << 0
    | io.audioError  << 3
    | io.audioPlay   << 4
    | io.audioRepeat << 5
    | io.audioBusy   << 6
    | io.dataBusy    << 7
    );
  case 0x2001:
    if(io.dataBusy) return 0x00;
    if(!dataFile) return 0x00;
    if(dataFile->end()) return 0x00;
    io.dataReadOffset++;
    return dataFile->read();
  case 0x2002: return 'S';
  case 0x2003: return '-';
  case 0x2004: return 'M';
  case 0x2005: return 'S';
  case 0x2006: return 'U';
  case 0x2007: return '1';
  }

  unreachable;
}

auto MSU1::writeIO(uint addr, uint8 data) -> void {
  cpu.synchronizeCoprocessors();

  switch(0x2000 | addr & 7) {
  case 0x2000: io.dataSeekOffset = io.dataSeekOffset & 0xffffff00 | data <<  0; break;
  case 0x2001: io.dataSeekOffset = io.dataSeekOffset & 0xffff00ff | data <<  8; break;
  case 0x2002: io.dataSeekOffset = io.dataSeekOffset & 0xff00ffff | data << 16; break;
  case 0x2003: io.dataSeekOffset = io.dataSeekOffset & 0x00ffffff | data << 24;
    io.dataReadOffset = io.dataSeekOffset;
    if(dataFile) dataFile->seek(io.dataReadOffset);
    break;
  case 0x2004: io.audioTrack = io.audioTrack & 0xff00 | data << 0; break;
  case 0x2005: io.audioTrack = io.audioTrack & 0x00ff | data << 8;
    io.audioPlay = false;
    io.audioRepeat = false;
    io.audioPlayOffset = 8;
    if(io.audioTrack == io.audioResumeTrack) {
      io.audioPlayOffset = io.audioResumeOffset;
      io.audioResumeTrack = ~0;  //erase resume track
      io.audioResumeOffset = 0;
    }
    audioOpen();
    break;
  case 0x2006:
    io.audioVolume = data;
    break;
  case 0x2007:
    if(io.audioBusy) break;
    if(io.audioError) break;
    io.audioPlay = bool(data & 1);
    io.audioRepeat = bool(data & 2);
    boolean audioResume = bool(data & 4);
    if(!io.audioPlay && audioResume) {
      io.audioResumeTrack = io.audioTrack;
      io.audioResumeOffset = io.audioPlayOffset;
    }
    break;
  }
}

}
