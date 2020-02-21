#include <stdafx.h>
#include <vd2/system/strutil.h>
#include "console.h"
#include "debuggerlog.h"
#include "simulator.h"
#include "cassette.h"

extern ATSimulator g_sim;

VDStringA g_ATDebuggerLogBuffer;

void ATConsoleLogWriteTag(ATLogChannel *channel);

void ATConsoleLogWrite(ATLogChannel *channel, const char *message) {
	ATConsoleLogWriteTag(channel);

	g_ATDebuggerLogBuffer.append(message);
	ATConsoleWrite(g_ATDebuggerLogBuffer.c_str());
}

void ATConsoleLogWriteV(ATLogChannel *channel, const char *format, va_list val) {
	ATConsoleLogWriteTag(channel);

	g_ATDebuggerLogBuffer.append_vsprintf(format, val);

	ATConsoleWrite(g_ATDebuggerLogBuffer.c_str());
}

void ATConsoleLogWriteTag(ATLogChannel *channel) {
	g_ATDebuggerLogBuffer.clear();

	const auto flags = channel->GetTagFlags();

	if (flags & kATLogFlags_Timestamp) {
		ATAnticEmulator& antic = g_sim.GetAntic();
		g_ATDebuggerLogBuffer.append_sprintf("(%3d:%3d,%3d) ", antic.GetFrameCounter(), antic.GetBeamY(), antic.GetBeamX());
	}

	if (flags & kATLogFlags_CassettePos) {
		ATCassetteEmulator& cas = g_sim.GetCassette();

		if (!cas.IsLoaded())
			g_ATDebuggerLogBuffer += "(---:--.---) ";
		else {
			float t = cas.GetPosition();
			int mins = (int)(t / 60.0f);
			g_ATDebuggerLogBuffer.append_sprintf("(%3d:%06.3f) ", mins, t - (float)mins * 60.0f);
		}
	}

	g_ATDebuggerLogBuffer += channel->GetName();
	g_ATDebuggerLogBuffer += ": ";
}

void ATInitDebuggerLogging() {
	ATLogSetWriteCallbacks(ATConsoleLogWrite, ATConsoleLogWriteV);
}

