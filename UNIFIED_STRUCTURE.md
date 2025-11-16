# Code Headers and Unified Structure Documentation

## Overview

This document describes the unified header structure and dependency management approach for BizHawkRafaelia.

## Header Templates

Two header templates are provided for new files:

### C/C++ Headers
**Template File**: `HEADER_TEMPLATE_C_CPP.txt`

Use this template for:
- C source files (.c)
- C++ source files (.cpp)
- Header files (.h, .hpp)

### C# Headers
**Template File**: `HEADER_TEMPLATE_C_SHARP.txt`

Use this template for:
- C# source files (.cs)

## Unified Include Structure

### Philosophy

BizHawkRafaelia adopts a unified approach to includes and dependencies:

1. **Centralized Common Headers**: Create common header files that consolidate frequently used includes
2. **Clear Dependency Chains**: Document and organize dependencies systematically
3. **Consistent Attribution**: All files maintain proper license headers
4. **Modular Organization**: Group related functionality together

### Implementation Guidelines

#### For C/C++ Projects

```c
// Example: Common header approach

// common.h - Centralized includes
#ifndef BIZHAWKRAFAELIA_COMMON_H
#define BIZHAWKRAFAELIA_COMMON_H

#include <stdint.h>
#include <stdbool.h>
#include <stdlib.h>
#include <string.h>

// Project-specific common definitions
typedef uint8_t  u8;
typedef uint16_t u16;
typedef uint32_t u32;
typedef uint64_t u64;

// Common macros
#define ARRAY_SIZE(x) (sizeof(x) / sizeof((x)[0]))

#endif // BIZHAWKRAFAELIA_COMMON_H
```

Individual source files then include the common header:

```c
#include "common.h"
// Additional specific includes as needed
```

#### For C# Projects

```csharp
// Example: Using directives organized by category

// System namespaces
using System;
using System.Collections.Generic;
using System.Linq;

// External dependencies
using Newtonsoft.Json;

// Project namespaces
using BizHawk.Common;
using BizHawk.Emulation.Common;
```

### Dependency Documentation

Each major component should document its dependencies:

#### Core Dependencies
```
BizHawk.Common
├── System (BCL)
├── System.Collections.Generic
├── System.Drawing
└── [Minimal external dependencies]

BizHawk.Emulation.Common
├── BizHawk.Common
├── System
└── [Core interfaces only]

BizHawk.Emulation.Cores
├── BizHawk.Common
├── BizHawk.Emulation.Common
└── [Core-specific native libraries]
```

## Attribution Requirements in Headers

All new or modified files MUST include:

1. **Copyright Notice**: Both BizHawk team and BizHawkRafaelia
2. **License Information**: Specify the applicable license
3. **Attribution**: Original authors and modifiers
4. **Brief Description**: What the file does
5. **References**: Links to CONTRIBUTORS.md, ATTRIBUTIONS.md, REFERENCES.md

## Best Practices

### When Creating New Files

1. Copy the appropriate header template
2. Fill in all required fields:
   - License (usually MIT for new BizHawkRafaelia code)
   - Original authors (if derived from existing code)
   - Your name and description of the file
   - Brief documentation
3. Use consistent formatting and style
4. Document dependencies

### When Modifying Existing Files

1. If the file lacks a proper header, add one
2. Add your name to the "Modified by" section
3. Preserve original copyright and attribution
4. Document significant changes
5. Ensure license compliance (don't change GPL to MIT!)

### When Integrating Third-Party Code

1. **ALWAYS** preserve original headers and copyright notices
2. Add BizHawkRafaelia integration notice AFTER original header
3. Document the source:
   - Original project name
   - Original author
   - Original license
   - URL to original source
   - Description of modifications
4. Add entry to ATTRIBUTIONS.md
5. Ensure license compatibility

Example:
```c
/*
 * Original Code from [PROJECT NAME]
 * Copyright (c) [YEAR] [ORIGINAL AUTHOR]
 * License: [ORIGINAL LICENSE]
 * Source: [URL]
 */

/*
 * Integrated into BizHawkRafaelia by [YOUR NAME]
 * Date: [DATE]
 * Modifications:
 * - [Description of changes]
 */
```

## Unified Core Structure

### Directory Organization

```
BizHawkRafaelia/
├── src/                          # C# source code
│   ├── BizHawk.Common/           # Common utilities
│   ├── BizHawk.Emulation.Common/ # Emulation interfaces
│   └── BizHawk.Emulation.Cores/  # Core implementations
├── libHawk/                      # C++ native cores
│   └── MSXHawk/
├── waterbox/                     # Sandboxed native cores
├── ExternalProjects/             # Third-party libraries
├── CONTRIBUTORS.md               # All contributors
├── ATTRIBUTIONS.md               # Third-party licenses
├── REFERENCES.md                 # Bibliographic references
├── LICENSE                       # Primary license file
└── HEADER_TEMPLATE_*.txt         # Header templates
```

### Include Paths and Dependencies

#### C/C++ Projects

Standard include structure:
```c
#include "common.h"           // Project common definitions
#include "core_specific.h"    // Core-specific headers
#include <standard_lib.h>     // Standard library
```

#### C# Projects

Namespace organization:
```
BizHawk.Common              - Fundamental utilities
BizHawk.Bizware.*          - GUI and platform abstraction
BizHawk.Client.Common       - Client-side common code
BizHawk.Emulation.Common    - Emulation interfaces
BizHawk.Emulation.Cores     - Emulation implementations
```

## License Compliance Checklist

Before committing code:

- [ ] Proper header with copyright and license
- [ ] Attribution to original authors (if applicable)
- [ ] License is compatible with existing code
- [ ] ATTRIBUTIONS.md updated (if third-party code)
- [ ] CONTRIBUTORS.md updated (if new contributor)
- [ ] No GPL code mixed with incompatible licenses
- [ ] All dependencies documented

## Optimization Guidelines

### Performance Considerations

When refining engines for better execution time:

1. **Profile First**: Identify actual bottlenecks
2. **Document Changes**: Explain optimizations in comments
3. **Preserve Accuracy**: Don't sacrifice correctness for speed
4. **Test Thoroughly**: Ensure optimizations don't break functionality
5. **Platform-Specific**: Consider cross-platform compatibility

### Code Quality

1. **Readability**: Code should be self-documenting
2. **Maintainability**: Future developers should understand your intent
3. **Consistency**: Follow existing code style
4. **Documentation**: Complex algorithms need explanation
5. **Attribution**: Credit optimization techniques from papers/references

## Resources

- **CONTRIBUTORS.md** - List all project contributors
- **ATTRIBUTIONS.md** - Detailed third-party attributions and licenses
- **REFERENCES.md** - Bibliographic references and inspirations
- **LICENSE** - Primary license file
- **contributing.md** - Contribution guidelines (from upstream BizHawk)

## Questions and Support

For questions about:
- **Headers**: See examples in existing code
- **Licenses**: Review ATTRIBUTIONS.md and LICENSE
- **Attribution**: Check CONTRIBUTORS.md
- **Structure**: Review this document

Open an issue on GitHub for clarification: https://github.com/rafaelmeloreisnovo/BizHawkRafaelia/issues

## Version History

- **2025-11-16**: Initial unified structure documentation created
- Future updates will be documented here

---

**Maintained by**: Rafael Melo Reis  
**Project**: BizHawkRafaelia  
**Last Updated**: 2025-11-16
