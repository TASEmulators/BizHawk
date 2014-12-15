#pragma once

#include "emuware/emuware.h"
#include <string>

namespace MDFN_IEN_PSX
{

std::string DisassembleMIPS(uint32 PC, uint32 instr);

}
