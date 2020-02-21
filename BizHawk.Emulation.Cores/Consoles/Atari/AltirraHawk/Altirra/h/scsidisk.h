#ifndef f_AT_SCSIDISK_H
#define f_AT_SCSIDISK_H

#include <at/atemulation/scsi.h>

class IATDeviceIndicatorManager;
class IATBlockDevice;

class IATSCSIDiskDevice : public IATSCSIDevice {
public:
	virtual void SetUIRenderer(IATDeviceIndicatorManager *r) = 0;
};

void ATCreateSCSIDiskDevice(IATBlockDevice *disk, IATSCSIDiskDevice **dev);

#endif
