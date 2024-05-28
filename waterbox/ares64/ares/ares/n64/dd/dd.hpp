//Disk Drive

#include <nall/bcd.hpp>

struct DD : Memory::PI<DD> {
  Node::Object obj;
  Node::Port port;
  Node::Peripheral node;
  VFS::Pak pak;
  Memory::Readable iplrom;
  Memory::Writable c2s;
  Memory::Writable ds;
  Memory::Writable ms;
  Memory::Writable disk;
  Memory::Writable error;

  struct Debugger {
    //debugger.cpp
    auto load(Node::Object) -> void;
    auto interrupt(u8 source) -> void;
    auto io(bool mode, u32 address, u32 data) -> void;

    struct Tracer {
      Node::Debugger::Tracer::Notification interrupt;
      Node::Debugger::Tracer::Notification io;
    } tracer;
  } debugger;

  struct RTC {
    Memory::Writable ram;

    //rtc.cpp
    auto load() -> void;
    auto reset() -> void;
    auto save() -> void;
    auto serialize(serializer& s) -> void;
    auto tick(u32 offset) -> void;
    auto tickClock() -> void;
    auto tickSecond() -> void;
    auto valid() -> bool;
    auto daysInMonth(u8 month, u8 year) -> u8;
  } rtc;

  auto title() const -> string { return information.title; }
  auto cic() const -> string { return information.cic; }

  //dd.cpp
  auto load(Node::Object) -> void;
  auto unload() -> void;

  auto allocate(Node::Port) -> Node::Peripheral;
  auto connect() -> void;
  auto disconnect() -> void;

  auto save() -> void;
  auto power(bool reset) -> void;

  enum class IRQ : u32 { MECHA, BM };
  auto raise(IRQ) -> void;
  auto lower(IRQ) -> void;
  auto poll() -> void;

  //controller.cpp
  auto command(n16 command) -> void;
  auto mechaResponse() -> void;

  //drive.cpp
  auto seekTrack() -> n1;
  auto seekSector(n8 sector) -> u32;
  auto bmRequest() -> void;
  auto motorActive() -> void;
  auto motorStandby() -> void;
  auto motorStop() -> void;
  auto motorChange() -> void;

  //io.cpp
  auto readHalf(u32 address) -> u16;
  auto writeHalf(u32 address, u16 data) -> void;
  auto readWord(u32 address) -> u32;
  auto writeWord(u32 address, u32 data) -> void;

  //serialization.cpp
  auto serialize(serializer&) -> void;

  struct Information {
    string title;
    string cic;
  } information;

private:
  struct Interrupt {
    b1 line = 0;
    b1 mask = 1;
  };

  struct IRQs {
    Interrupt mecha;
    Interrupt bm;
  } irq;

  struct Controller {
    n8 diskType;
    struct {
      n1 selfDiagnostic;
      n1 servoNG;
      n1 indexGapNG;
      n1 timeout;
      n1 undefinedCommand;
      n1 invalidParam;
      n1 unknown;
    } error;
    n1 standbyDelayDisable = 0;
    n16 standbyDelay = 3 * 0x17;
    n1 sleepDelayDisable = 0;
    n16 sleepDelay = 1 * 0x17;
    n8 ledOnTime = 0x4;
    n8 ledOffTime = 0x4;
  } ctl;

  struct IO {
    n16 data;

    struct {
      n1 requestUserSector;
      n1 requestC2Sector;
      n1 busyState;
      n1 resetState;

      n1 spindleMotorStopped;
      n1 headRetracted;

      n1 writeProtect;
      n1 mechaError;
      n1 diskChanged;
      n1 diskPresent;
    } status;

    n16 currentTrack;
    n8 currentSector;

    n8 sectorSizeBuf;
    n8 sectorSize;
    n8 sectorBlock;
    n16 id;

    struct {
      //stat
      n1 start;
      n1 reset;
      n1 error;
      n1 blockTransfer;

      n1 c1Correct;
      n1 c1Double;
      n1 c1Single;
      n1 c1Error;

      //ctl
      n1 readMode;

      n1 disableORcheck;
      n1 disableC1Correction;
    } bm;

    struct {
      n1 am;

      n1 spindle;
      n1 overrun;
      n1 offTrack;
      n1 clockUnlock;
      n1 selfStop;

      n8 sector;
    } error;

    struct {
      n1 enable;
      n1 error;
    } micro;
  } io;

  struct State {
    n1 seek;
  } state;

  struct Command { enum : u16 {
      Nop                = 0x0,  //no operation
      ReadSeek           = 0x1,  //seek (read)
      WriteSeek          = 0x2,  //seek (write)
      Recalibration      = 0x3,  //recalibration
      Sleep              = 0x4,  //drive sleep (stop motor and retract heads)
      Start              = 0x5,  //start the drive
      SetStandby         = 0x6,  //set auto standby time
      SetSleep           = 0x7,  //set auto sleep time
      ClearChangeFlag    = 0x8,  //clear disk changed flag
      ClearResetFlag     = 0x9,  //clear reset and disk changed flag
      ReadVersion        = 0xa,  //read asic version
      SetDiskType        = 0xb,  //set disk type (defines write protection)
      RequestStatus      = 0xc,  //request internal drive error status
      Standby            = 0xd,  //drive standby (set heads)
      IndexLockRetry     = 0xe,  //index lock retry (track index)
      SetRTCYearMonth    = 0xf,  //set year and month
      SetRTCDayHour      = 0x10, //set day and hour
      SetRTCMinuteSecond = 0x11, //set minute and second
      GetRTCYearMonth    = 0x12, //get year and month
      GetRTCDayHour      = 0x13, //get day and hour
      GetRTCMinuteSecond = 0x14, //get minute and second (must be done first)
      SetLEDBlinkRate    = 0x15, //set led blink rate
      ReadProgramVersion = 0x1b, //read program version
    };};
};

extern DD dd;
