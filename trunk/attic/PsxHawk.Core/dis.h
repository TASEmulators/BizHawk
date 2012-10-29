/*
this disassembler is courtesy of mednafen
*/

#pragma once

#include <string>
#include "types.h"

namespace MDFN_IEN_PSX
{

std::string DisassembleMIPS(u32 PC, u32 instr);

}
