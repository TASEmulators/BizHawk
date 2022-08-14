/* x86_features.c - x86 feature check
 *
 * Copyright (C) 2013 Intel Corporation. All rights reserved.
 * Author:
 *  Jim Kukunas
 *
 * For conditions of distribution and use, see copyright notice in README.md
 */

#include <cpuid.h>
#include <string.h>

int x86_cpu_has_avx2;
int x86_cpu_has_avx512;
int x86_cpu_has_avx512vnni;
int x86_cpu_has_sse2;
int x86_cpu_has_ssse3;
int x86_cpu_has_sse41;
int x86_cpu_has_sse42;
int x86_cpu_has_pclmulqdq;
int x86_cpu_has_vpclmulqdq;
int x86_cpu_has_tzcnt;
int x86_cpu_has_sha;

void x86_check_features(void) {
	static int features_checked = 0;
	if (features_checked)
		return;

	unsigned eax, ebx, ecx, edx;
	unsigned maxbasic;

	__cpuid(0, maxbasic, ebx, ecx, edx);
	__cpuid(1 /*CPU_PROCINFO_AND_FEATUREBITS*/, eax, ebx, ecx, edx);

	x86_cpu_has_sse2 = edx & 0x4000000;
	x86_cpu_has_ssse3 = ecx & 0x200;
	x86_cpu_has_sse41 = ecx & 0x80000;
	x86_cpu_has_sse42 = ecx & 0x100000;
	x86_cpu_has_pclmulqdq = ecx & 0x2;

	if (maxbasic >= 7) {
		__cpuid_count(7, 0, eax, ebx, ecx, edx);

		// check BMI1 bit
		// Reference: https://software.intel.com/sites/default/files/article/405250/how-to-detect-new-instruction-support-in-the-4th-generation-intel-core-processor-family.pdf
		x86_cpu_has_tzcnt = ebx & 0x8;
		// check AVX2 bit
		x86_cpu_has_avx2 = ebx & 0x20;
		x86_cpu_has_avx512 = ebx & 0x00010000;
		x86_cpu_has_avx512vnni = ecx & 0x800;
		x86_cpu_has_vpclmulqdq = ecx & 0x400;
		// check SHA bit
		x86_cpu_has_sha = ebx & 0x20000000;
	} else {
		x86_cpu_has_tzcnt = 0;
		x86_cpu_has_avx2 = 0;
		x86_cpu_has_avx512 = 0;
		x86_cpu_has_avx512vnni = 0;
		x86_cpu_has_vpclmulqdq = 0;
		x86_cpu_has_sha = 0;
	}
}
