#include "mednafen/src/types.h"
#include "nyma.h"
#include <emulibc.h>
#include "mednafen/src/ngp/neopop.h"

using namespace MDFN_IEN_NGP;

extern Mednafen::MDFNGI EmulatedNGP;

void SetupMDFNGameInfo()
{
	Mednafen::MDFNGameInfo = &EmulatedNGP;
}
