#include <stdlib.h>
#include <emulibc.h>

// for systems that do not include cds, we need just a bit of stub code

void StartGameWithCds(int numdisks)
{
	abort();
}
ECL_EXPORT void SetCDCallbacks()
{
	abort();
}
void SwitchCds(bool prev, bool next)
{
	abort();
}
