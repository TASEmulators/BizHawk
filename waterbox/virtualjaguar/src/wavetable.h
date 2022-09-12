//
// Jaguar Wavetable ROM
//
// In a real Jaguar, these are 16-bit values that are sign-extended to 32 bits.
// Each entry has 128 values (e.g., SINE goes from F1D200-F1D3FF)
//

// NOTE: This can probably be converted to 32-bit table, since I don't think
//       that unaligned access is allowed...

#ifndef __WAVETABLE_H__
#define __WAVETABLE_H__

// How to preserve const-ness of this stuff without introducing tons of hairiness?
extern /*const*/ unsigned char waveTableROM[];

#endif	// __WAVETABLE_H__
