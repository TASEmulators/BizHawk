#include <stdafx.h>
#include <windows.h>

bool ShouldBreak() {
	return !!IsDebuggerPresent();
}
