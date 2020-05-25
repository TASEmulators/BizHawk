#pragma once

#include "mednafen/src/types.h"
#include <mednafen/mednafen.h>
#include <mednafen/cdrom/CDInterface.h>

class CDInterfaceNyma : public Mednafen::CDInterface
{
  private:
	int disk;

  public:
	CDInterfaceNyma(int disk);
	virtual void HintReadSector(int32 lba) override;
	virtual bool ReadRawSector(uint8 *buf, int32 lba) override;
	virtual bool ReadRawSectorPWOnly(uint8 *pwbuf, int32 lba, bool hint_fullread) override;
};
