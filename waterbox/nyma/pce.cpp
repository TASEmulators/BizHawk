#include "mednafen/src/types.h"
#include "nyma.h"
#include <emulibc.h>
#include "mednafen/src/pce/pce.h"

using namespace MDFN_IEN_PCE;

extern Mednafen::MDFNGI EmulatedPCE;

void SetupMDFNGameInfo()
{
	Mednafen::MDFNGameInfo = &EmulatedPCE;
}
