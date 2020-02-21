#ifndef f_ATS_SERIALHANDLER_H
#define f_ATS_SERIALHANDLER_H

class IATSSerialEngine;
struct ATSSerialConfig;

enum ATSerialCtlState : uint8 {
	kATSerialCtlState_Command = 1,
	kATSerialCtlState_Motor = 2
};

class IATSSerialHandler {
public:
	virtual void OnAttach(IATSSerialEngine& eng) = 0;
	virtual void OnConfigChanged(const ATSSerialConfig&) = 0;
	virtual void OnControlStateChanged(uint8 newState) = 0;
	virtual void OnReadDataAvailable(uint32 len) = 0;
	virtual void OnWriteSpaceAvailable(uint32 len) = 0;
	virtual void OnWriteBufferEmpty() = 0;
	virtual void OnReadFramingError() = 0;
};

#endif
