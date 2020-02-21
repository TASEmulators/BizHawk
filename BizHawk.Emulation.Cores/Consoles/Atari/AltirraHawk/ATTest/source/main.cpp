#include <stdafx.h>
#include <stdio.h>
#include <vd2/system/vdtypes.h>
#include <vd2/system/cpuaccel.h>
#include <vd2/system/VDString.h>
#include <vd2/system/text.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/bitmath.h>

#include "test.h"

#include <vector>
#include <utility>

#if defined(_M_IX86)
	#define BUILD L"80x86"
#elif defined(_M_AMD64)
	#define BUILD L"AMD64"
#elif defined(_M_ARM64)
	#define BUILD L"ARM64"
#endif

namespace {
	struct TestInfo {
		TestFn		mpTestFn;
		const char	*mpName;
		bool		mbAutoRun;
	};

	typedef vdfastvector<TestInfo> Tests;
	Tests g_tests;
}

void AddTest(TestFn f, const char *name, bool autoRun) {
	TestInfo ti;
	ti.mpTestFn = f;
	ti.mpName = name;
	ti.mbAutoRun = autoRun;
	g_tests.push_back(ti);
}

void help() {
	wprintf(L"\n");
	wprintf(L"Available tests:\n");

	for(Tests::const_iterator it(g_tests.begin()), itEnd(g_tests.end()); it!=itEnd; ++it) {
		const TestInfo& ent = *it;

		wprintf(L"\t%hs%s\n", ent.mpName, ent.mbAutoRun ? L"" : L"*");
	}
	wprintf(L"\tAll\n");
}

int VDCDECL wmain(int argc, wchar_t **argv) {
	_CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_CHECK_ALWAYS_DF | _CRTDBG_LEAK_CHECK_DF);

	wprintf(L"Altirra test harness utility for " BUILD L"\n");
	wprintf(L"Copyright (C) 2016 Avery Lee. Licensed under GNU General Public License\n\n");

	Tests selectedTests;

	if (argc <= 1) {
		help();
		exit(0);
	} else {
		for(int i=1; i<argc; ++i) {
			const wchar_t *test = argv[i];

			if (!_wcsicmp(test, L"all")) {
				for(Tests::const_iterator it(g_tests.begin()), itEnd(g_tests.end()); it!=itEnd; ++it) {
					const TestInfo& ent = *it;

					if (ent.mbAutoRun)
						selectedTests.push_back(ent);
				}
				break;
			}

			for(Tests::const_iterator it(g_tests.begin()), itEnd(g_tests.end()); it!=itEnd; ++it) {
				const TestInfo& ent = *it;

				if (!_wcsicmp(VDTextAToW(ent.mpName).c_str(), test)) {
					selectedTests.push_back(ent);
					goto next;
				}
			}

			wprintf(L"\nUnknown test: %ls\n", test);
			help();
			exit(5);
next:
			;
		}
	}

	long exts = CPUCheckForExtensions();
	int failedTests = 0;

	CPUEnableExtensions(exts);

	for(Tests::const_iterator it(selectedTests.begin()), itEnd(selectedTests.end()); it!=itEnd; ++it) {
		const Tests::value_type& ent = *it;

		wprintf(L"Running test: %hs\n", ent.mpName);

		try {
			ent.mpTestFn();
		} catch(const AssertionException& e) {
			wprintf(L"    TEST FAILED: %hs\n", e.gets());
			++failedTests;
		}
	}

	printf("Tests complete. Failures: %u\n", failedTests);

	return failedTests;
}
