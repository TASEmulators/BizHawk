#include <ares/ares.hpp>
#include <ares/debug/debug.cpp>
#include <nall/gdb/server.cpp>
#include <ares/node/node.cpp>
#include <ares/resource/resource.cpp>

namespace ares {

Platform* platform = nullptr;
bool _runAhead = false;

const string Name       = "ares";
const string Version    = "138";
const string Copyright  = "ares team, Near";
const string License    = "ISC";
const string LicenseURI = "https://opensource.org/licenses/ISC";
const string Website    = "ares-emu.net";
const string WebsiteURI = "https://ares-emu.net/";
const u32    SerializerSignature = 0x31545342;  //"BST1" (little-endian)

}
