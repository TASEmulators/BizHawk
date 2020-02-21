#ifndef f_AT_SCSI_H
#define f_AT_SCSI_H

#include <vd2/system/unknown.h>
#include <at/atcore/scheduler.h>

class ATSCSIBusEmulator;

enum ATSCSICtrlState {
	kATSCSICtrlState_RST = 0x0100,
	kATSCSICtrlState_BSY = 0x0200,
	kATSCSICtrlState_SEL = 0x0400,
	kATSCSICtrlState_IO = 0x0800,
	kATSCSICtrlState_CD = 0x1000,
	kATSCSICtrlState_MSG = 0x2000,
	kATSCSICtrlState_ACK = 0x4000,
	kATSCSICtrlState_REQ = 0x8000,
	kATSCSICtrlState_All = 0xFF00
};

class IATSCSIBusMonitor {
public:
	virtual void OnSCSIControlStateChanged(uint32 state) = 0;
};

class IATSCSIDevice : public IVDRefUnknown {
public:
	enum { kTypeID = 'scdv' };

	virtual void Attach(ATSCSIBusEmulator *bus) = 0;
	virtual void Detach() = 0;
	virtual void BeginCommand(const uint8 *command, uint32 length) = 0;
	virtual void AdvanceCommand() = 0;
	virtual void AbortCommand() = 0;

	virtual void SetBlockSize(uint32 blockSize) = 0;
};

class ATSCSIBusEmulator final : public IATSchedulerCallback {
	ATSCSIBusEmulator(const ATSCSIBusEmulator&) = delete;
	ATSCSIBusEmulator& operator=(const ATSCSIBusEmulator&) = delete;
public:
	ATSCSIBusEmulator();
	~ATSCSIBusEmulator();

	void Init(ATScheduler *scheduler);
	void Shutdown();

	void SetBusMonitor(IATSCSIBusMonitor *monitor) {
		mpBusMonitor = monitor;
	}

	uint32 GetBusState() const { return mBusState; }
	void SetControl(uint32 idx, uint32 state, uint32 mask = kATSCSICtrlState_All);

	void AttachDevice(uint32 id, IATSCSIDevice *dev);
	void DetachDevice(IATSCSIDevice *dev);
	void SwapDevices(uint32 id1, uint32 id2);

	void CommandAbort();

	/// Release BSY to end the information transfer phases.
	void CommandEnd();
	void CommandDelay(float microseconds);

	enum SendMode {
		kSendMode_DataIn,
		kSendMode_Status,
		kSendMode_MessageIn
	};

	void CommandSendData(SendMode mode, const void *data, uint32 length);

	enum ReceiveMode {
		kReceiveMode_DataOut,
		kReceiveMode_MessageOut
	};

	void CommandReceiveData(ReceiveMode mode, void *buf, uint32 length);

public:
	void OnScheduledEvent(uint32 id) override;

protected:
	enum BusPhase {
		kBusPhase_BusFree,
		kBusPhase_Selection,
		kBusPhase_Command,
		kBusPhase_DataIn,
		kBusPhase_DataOut,
		kBusPhase_Status,
		kBusPhase_MessageIn,
		kBusPhase_MessageOut
	};

	void UpdateBusState();
	void SetBusPhase(BusPhase phase);
	void AdvanceCommand();

	IATSCSIBusMonitor *mpBusMonitor = nullptr;
	ATScheduler *mpScheduler = nullptr;

	uint32 mEndpointState[2] = {};
	uint32 mBusState = 0;
	BusPhase mBusPhase = kBusPhase_BusFree;

	ATEvent *mpEventCommandDelay = nullptr;
	bool mbCommandActive = false;
	IATSCSIDevice *mpTargetDevice = nullptr;

	const uint8 *mpTransferBuffer = nullptr;
	bool mbTransferInActive = false;
	bool mbTransferOutActive = false;
	uint32 mTransferIndex = 0;
	uint32 mTransferLength = 0;

	uint8 mCommandBuffer[16] = {};

	IATSCSIDevice *mpDevices[8] = {};
};

#endif
