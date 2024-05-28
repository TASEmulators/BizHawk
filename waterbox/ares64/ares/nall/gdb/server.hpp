#pragma once

#include <nall/tcptext/tcptext-server.hpp>
#include <nall/gdb/watchpoint.hpp>

namespace nall::GDB {

enum class Signal : u8 {
  HANGUP  = 1,
  INT     = 2,
  QUIT    = 3,
  ILLEGAL = 4,
  TRAP    = 5,
  ABORT   = 6,
  SEGV    = 11,
};

/**
 * This implements a GDB server to handle remote debugging via a GDB client.
 * It is both independent of ares itself and any specific system.
 * Functionality is added by providing system-specific callbacks, as well as using the API inside a system.
 * (See the Readme.md file for more information.)
 * 
 * NOTE:
 * Command handling and the overall logic was carefully designed to support as many IDEs and GDB versions as possible.
 * Things can break very easily (and the official documentation may lie), so be very sure of any changes made here.
 * If changes are necessary, please verify that the following gdb-versions / IDEs still work properly:
 * 
 * GDB:
 * - gdb-multiarch        (the plain vanilla version exists in most package managers, supports a lot of arches)
 * - mips64-ultra-elf-gdb (special MIPS build of gdb-multiarch, i do NOT recommend it, behaves strangely)
 * - mingw-w64-x86_64-gdb (vanilla build for Windows/MSYS)
 * 
 * IDEs/Tools:
 * - GDB's CLI
 * - VSCode
 * - CLion (with bundled gdb-multiarch)
 * 
 * For testing, please also check both linux and windows (WSL2).
 * With WSL2, windows-ares is started from within WSL, while the debugger runs in linux.
 * This can be easily tested with VSCode and it's debugger.
 */
class Server : public nall::TCPText::Server {
  public:

    auto reset() -> void;

    struct {
      // Memory
      function<string(u64 address, u32 byteCount)> read{};
      function<void(u64 address, u32 unitSize, u64 value)> write{};
      function<u64(u64 address)> normalizeAddress{};

      // Registers
      function<string()> regReadGeneral{};
      function<void(const string &regData)> regWriteGeneral{};
      function<string(u32 regIdx)> regRead{};
      function<bool(u32 regIdx, u64 regValue)> regWrite{};

      // Emulator
      function<void(u64 address)> emuCacheInvalidate{};
      function<string()> targetXML{};


    } hooks{};

    // Exception
    auto reportSignal(Signal sig, u64 originPC) -> bool;

    // PC / Memory State Updates
    auto reportPC(u64 pc) -> bool;
    auto reportMemRead(u64 address, u32 size) -> void;
    auto reportMemWrite(u64 address, u32 size) -> void;

    // Breakpoints / Watchpoints
    auto isHalted() const { return forceHalt && haltSignalSent; }
    auto hasBreakpoints() const { 
      return breakpoints || singleStepActive || watchpointRead || watchpointWrite;
    }
    
    auto getPcOverride() const { return pcOverride; };

    auto updateLoop() -> void;
    auto getStatusText(u32 port, bool useIPv4) -> string;

  protected:
    auto onText(string_view text) -> void override;
    auto onConnect() -> void override;
    auto onDisconnect() -> void override;

  private:
    bool insideCommand{false};
    string cmdBuffer{""};

    bool haltSignalSent{false}; // marks if a signal as been sent for new halts (force-halt and breakpoints)
    bool forceHalt{false}; // forces a halt despite no breakpoints being hit
    bool singleStepActive{false};

    bool noAckMode{false}; // gets set if lldb prefers no acknowledgements
    bool nonStopMode{false}; // (NOTE: Not working for now), gets set if gdb wants to switch over to async-messaging
    bool handshakeDone{false}; // set to true after a few handshake commands, used to prevent exception-reporting until client is ready
    bool requestDisconnect{false}; // set to true if the client decides it wants to disconnect

    bool hasActiveClient{false};
    u32 messageCount{0}; // message count per update loop
    s32 currentThreadC{-1}; // selected thread for the next 'c' command

    u64 currentPC{0};
    maybe<u64> pcOverride{0}; // temporary override to handle edge-cases for exceptions/watchpoints

    // client-state:
    vector<u64> breakpoints{};
    vector<Watchpoint> watchpointRead{};
    vector<Watchpoint> watchpointWrite{};

    auto processCommand(const string& cmd, bool &shouldReply) -> string;
    auto resetClientData() -> void;

    auto reportWatchpoint(const Watchpoint &wp, u64 address) -> void;

    auto sendPayload(const string& payload) -> void;
    auto sendSignal(Signal code) -> void;
    auto sendSignal(Signal code, const string& reason) -> void;

    auto haltProgram() -> void;
    auto resumeProgram() -> void;
};

extern Server server;

}
