# Optimization Guidelines and Engine Refinement

## Overview

This document outlines guidelines and best practices for optimizing and refining the BizHawkRafaelia emulation engines for improved execution time and stability.

## Core Optimization Principles

### 1. Accuracy First, Then Performance

**Philosophy**: Never sacrifice emulation accuracy for performance unless explicitly configurable by the user.

**Rationale**: BizHawkRafaelia is used for Tool-Assisted Speedrunning (TAS), which requires deterministic, cycle-accurate emulation. Performance optimizations must not compromise this.

### 2. Profile Before Optimizing

**Always measure before optimizing:**

```bash
# Example: Profiling with dotnet-trace
dotnet trace collect --process-id <PID> --profile cpu-sampling
```

**Don't optimize based on assumptions**:
- Identify actual bottlenecks through profiling
- Focus on hot paths (frequently executed code)
- Measure the impact of each optimization

### 3. Maintain Cross-Platform Compatibility

Optimizations should work on:
- Windows (x64)
- Linux (x64, potentially ARM64)
- Both .NET Framework (Windows) and Mono (Linux)

## Optimization Strategies by Component

### Frontend (C# Code)

#### Memory Management

**Good Practices:**
```csharp
// Reuse buffers instead of allocating new ones
private byte[] _frameBuffer = new byte[320 * 240];

public void ProcessFrame()
{
    // Reuse existing buffer
    Array.Clear(_frameBuffer, 0, _frameBuffer.Length);
    // ... process frame
}
```

**Avoid:**
```csharp
// Don't create new arrays every frame
public void ProcessFrame()
{
    byte[] buffer = new byte[320 * 240]; // Allocation overhead!
    // ... process frame
}
```

#### Collection Usage

**Good Practices:**
```csharp
// Pre-allocate collections with known size
var list = new List<int>(capacity: 1000);

// Use for loops instead of foreach for arrays
for (int i = 0; i < array.Length; i++)
{
    // Process array[i]
}
```

#### String Operations

**Good Practices:**
```csharp
// Use StringBuilder for string concatenation in loops
var sb = new StringBuilder(capacity: 100);
for (int i = 0; i < items.Count; i++)
{
    sb.Append(items[i]);
}
string result = sb.ToString();
```

### Emulation Cores (Native Code)

#### CPU Emulation Optimization

**Techniques:**

1. **Instruction Lookup Tables**
```c
// Fast dispatch using function pointer tables
typedef void (*CPUInstruction)(CPU* cpu);
CPUInstruction opcode_table[256];

void execute_instruction(CPU* cpu, uint8_t opcode)
{
    opcode_table[opcode](cpu);
}
```

2. **Avoid Unnecessary Branches**
```c
// Use branchless code where possible
// Bad:
if (flag) value = a; else value = b;

// Better:
value = flag ? a : b;

// Even better (branchless):
value = (flag * a) + (!flag * b);
```

3. **Cache-Friendly Data Structures**
```c
// Structure of Arrays (SoA) for better cache locality
struct Sprites
{
    uint8_t x[64];
    uint8_t y[64];
    uint8_t tile[64];
} sprites;

// Instead of Array of Structures (AoS)
struct Sprite
{
    uint8_t x, y, tile;
};
struct Sprite sprites[64]; // Worse cache locality
```

#### Graphics Processing Optimization

1. **Batch Operations**
```c
// Process multiple pixels in one operation
void render_scanline_optimized(uint32_t* output, uint8_t* input, int width)
{
    // Process 4 pixels at a time using SIMD or loop unrolling
    for (int i = 0; i < width; i += 4)
    {
        output[i+0] = palette[input[i+0]];
        output[i+1] = palette[input[i+1]];
        output[i+2] = palette[input[i+2]];
        output[i+3] = palette[input[i+3]];
    }
}
```

2. **Dirty Rectangle Tracking**
```c
// Only redraw changed portions of the screen
typedef struct
{
    int x, y, width, height;
    bool dirty;
} RenderRegion;

void mark_dirty(RenderRegion* region, int x, int y, int w, int h)
{
    region->dirty = true;
    region->x = x;
    region->y = y;
    region->width = w;
    region->height = h;
}
```

#### Sound Processing Optimization

1. **Buffer Management**
```c
// Use ring buffers for audio streaming
typedef struct
{
    int16_t* buffer;
    size_t capacity;
    size_t read_pos;
    size_t write_pos;
} AudioRingBuffer;
```

2. **Resampling Efficiency**
```c
// Use blip_buf or similar optimized resampling
// Avoid per-sample processing in hot paths
```

## Stability Improvements

### 1. Error Handling

**C# Code:**
```csharp
public void LoadROM(string path)
{
    try
    {
        // Validate inputs
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));
        
        if (!File.Exists(path))
            throw new FileNotFoundException($"ROM not found: {path}");
        
        // Load ROM with proper error handling
        byte[] data = File.ReadAllBytes(path);
        ValidateROMData(data);
        
        // ... rest of loading
    }
    catch (Exception ex)
    {
        // Log and handle gracefully
        Console.WriteLine($"Error loading ROM: {ex.Message}");
        throw;
    }
}
```

**Native Code:**
```c
// Always check for NULL pointers
bool load_rom(Core* core, const char* path)
{
    if (core == NULL || path == NULL)
        return false;
    
    FILE* f = fopen(path, "rb");
    if (f == NULL)
    {
        fprintf(stderr, "Failed to open: %s\n", path);
        return false;
    }
    
    // ... rest of loading
    fclose(f);
    return true;
}
```

### 2. Memory Safety

**Bounds Checking:**
```c
// Always validate array indices
void write_memory(Memory* mem, uint32_t address, uint8_t value)
{
    if (address >= mem->size)
    {
        // Handle error - don't crash
        fprintf(stderr, "Memory write out of bounds: 0x%08X\n", address);
        return;
    }
    mem->data[address] = value;
}
```

**Resource Cleanup:**
```csharp
// Use 'using' statements for proper disposal
using (var stream = File.OpenRead(path))
{
    // Process file
} // Automatically closed even if exception occurs
```

### 3. Thread Safety

**C# Synchronization:**
```csharp
private readonly object _lock = new object();

public void ThreadSafeOperation()
{
    lock (_lock)
    {
        // Critical section
    }
}
```

**Atomic Operations:**
```c
#include <stdatomic.h>

typedef struct
{
    atomic_int frame_count;
} Core;

void increment_frame(Core* core)
{
    atomic_fetch_add(&core->frame_count, 1);
}
```

## Unified Dependency Management

### C/C++ Projects

**Create common headers per core:**

```c
// msxhawk_common.h
#ifndef MSXHAWK_COMMON_H
#define MSXHAWK_COMMON_H

#include <stdint.h>
#include <stdbool.h>
#include <string.h>

// Common type definitions
typedef uint8_t  u8;
typedef uint16_t u16;
typedef uint32_t u32;

// Common macros
#define MIN(a, b) ((a) < (b) ? (a) : (b))
#define MAX(a, b) ((a) > (b) ? (a) : (b))

// Core state structure
typedef struct MSXCore MSXCore;

#endif
```

**Then in each source file:**
```c
#include "msxhawk_common.h"
// Core-specific includes follow
```

### C# Projects

**Organized using directives:**
```csharp
// Group 1: System namespaces
using System;
using System.Collections.Generic;
using System.IO;

// Group 2: BizHawk common
using BizHawk.Common;
using BizHawk.Common.PathExtensions;

// Group 3: Emulation interfaces
using BizHawk.Emulation.Common;

// Group 4: Core-specific
using BizHawk.Emulation.Cores.Components;
```

## Performance Testing

### Automated Benchmarks

```csharp
// Example benchmark structure
[Benchmark]
public void BenchmarkFrameProcessing()
{
    for (int i = 0; i < 1000; i++)
    {
        core.FrameAdvance(controller, true);
    }
}
```

### Regression Testing

**Ensure optimizations don't break accuracy:**

1. Run test ROM suites before and after optimization
2. Compare output with reference emulators
3. Verify TAS movies still sync
4. Check performance metrics

## Documentation Requirements

When implementing optimizations:

1. **Comment the optimization:**
```c
// Optimization: Use lookup table instead of switch for 20% speedup
// Profiling showed this was a hot path (15% of CPU time)
```

2. **Document assumptions:**
```c
// Assumes width is always multiple of 4
// Caller must ensure this or use slow path
```

3. **Cite references:**
```c
// Based on technique from:
// "Fast NES Emulation Using Dynamic Recompilation"
// See REFERENCES.md for full citation
```

4. **Measure and record:**
```c
// Performance: ~60fps -> ~120fps on i5-8600K
// Tested with Super Mario Bros. 3
```

## Optimization Checklist

Before committing optimizations:

- [ ] Profiled to identify actual bottleneck
- [ ] Measured performance improvement
- [ ] Tested on multiple systems (if applicable)
- [ ] Verified accuracy not compromised
- [ ] Ran test ROM suites
- [ ] Tested TAS movie sync
- [ ] Documented the optimization
- [ ] Added comments explaining technique
- [ ] Updated any relevant documentation
- [ ] Checked cross-platform compatibility

## Anti-Patterns to Avoid

### Don't:

1. **Premature Optimization**
   - Optimize only after profiling identifies bottlenecks

2. **Micro-Optimizations That Hurt Readability**
   - Don't sacrifice code clarity for negligible gains

3. **Platform-Specific Code Without Fallbacks**
   - Always provide portable alternatives

4. **Breaking Determinism**
   - Never introduce non-deterministic behavior for speed

5. **Ignoring Memory Leaks**
   - Profile memory usage, not just CPU time

6. **Removing Safety Checks in Release Builds**
   - Keep critical validation even in optimized builds

## Resources

### Profiling Tools

- **Windows**: Visual Studio Profiler, dotTrace, PerfView
- **Linux**: perf, Valgrind, gperftools
- **.NET**: dotnet-trace, dotnet-counters, BenchmarkDotNet

### References

See REFERENCES.md for:
- Emulation optimization papers
- CPU architecture documentation
- Performance engineering resources

### Community Resources

- TASVideos forums - Testing and feedback
- GitHub issues - Performance reports
- Discord - Real-time optimization discussions

## Version History

- **2025-11-16**: Initial optimization guidelines created

---

**Note**: All optimizations must maintain the project's core principles:
1. Accuracy over speed (for TASing)
2. Cross-platform compatibility
3. Code maintainability
4. Proper attribution

For questions about optimizations, open an issue on GitHub or discuss on Discord.

**Maintained by**: Rafael Melo Reis  
**Project**: BizHawkRafaelia  
**Last Updated**: 2025-11-16
