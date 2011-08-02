#include "DiscInterface.h"

static FunctionRecord records[] = {
	REG("DiscInterface.Construct", &DiscInterface::Construct),
	REG("DiscInterface.Set_fp", &DiscInterface::Set_fp),
};

void* DiscInterface::Construct(void* ManagedOpaque)
{
	return new DiscInterface(ManagedOpaque);
}