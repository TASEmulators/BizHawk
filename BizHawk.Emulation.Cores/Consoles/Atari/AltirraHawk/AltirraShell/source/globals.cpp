#include <stdafx.h>
#include <at/atcore/devicemanager.h>
#include <at/atdevices/devices.h>
#include <at/atnativeui/syncwait.h>
#include "serialengine.h"
#include "serialemulator.h"
#include "serialconfig.h"

ATDeviceManager g_ATSDeviceManager;
ATSSerialEngine g_ATSSerialEngine;
ATSSerialEmulator g_ATSSerialEmulator;

void ATSInitSerialEngine() {
	g_ATSSerialEmulator.Init(g_ATSDeviceManager);
	g_ATSSerialEngine.Init(&g_ATSSerialEmulator);
}

void ATSShutdownSerialEngine() {
	g_ATSSerialEngine.Shutdown();
}

void ATSPostEngineRequest(vdfunction<void()> fn) {
	g_ATSSerialEngine.PostRequest(std::move(fn));
}

void ATSSendEngineRequest(vdfunction<void()> fn) {
	ATNativeUISyncWait waiter;

	g_ATSSerialEngine.PostRequest(
		[&]() {
			fn();
			waiter.Signal();
		}
	);

	waiter.Wait();
}

void ATSGetSerialConfig(ATSSerialConfig& cfg) {
	cfg = std::move(g_ATSSerialEngine.GetConfig());
}

void ATSSetSerialConfig(const ATSSerialConfig& cfg) {
	g_ATSSerialEngine.SetConfig(cfg);
}

void ATSInitDeviceManager() {
	g_ATSDeviceManager.Init();

	ATRegisterDeviceLibrary(g_ATSDeviceManager);
}

void ATSShutdownDeviceManager() {
	g_ATSDeviceManager.RemoveAllDevices(true);
}

ATDeviceManager *ATSGetDeviceManager() {
	return &g_ATSDeviceManager;
}
