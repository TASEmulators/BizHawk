#ifndef BIZGLPRESENTER_H
#define BIZGLPRESENTER_H

#include "GPU.h"
#include "BizTypes.h"

namespace GLPresenter
{

void Init(u32 scale);
std::pair<u32, u32> Present(melonDS::GPU& gpu);

}

#endif
