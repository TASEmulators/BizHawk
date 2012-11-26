#include "mednafen.h"
#include "headless.h"

int MDFNnetplay = 0;
void MDFND_NetplayText(const uint8* buf, bool something) {}
void MDFND_NetworkClose(void) {}
bool MDFNI_DumpSettingsDef(char const *) { return false; }

uint32 MDFND_GetTime(void) { return (uint32)0xFFFFFFFF; }
void MDFND_DispMessage(UTF8 *text)
{
	printf("%s",text);
}

void LockGameMutex(bool lock)
{
}