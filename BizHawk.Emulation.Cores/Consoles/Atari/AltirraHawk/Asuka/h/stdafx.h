// Needed to work around d3d11.h issue that breaks under Clang/C2:
// 2>c:\dx9sdk6\include\d3d11.h(930,48): error : default initialization of an object of const type 'const CD3D11_DEFAULT' without a user-provided default constructor
#define D3D11_NO_HELPERS

struct IUnknown;

#include <stdio.h>
#include <stdlib.h>
#include <stddef.h>
#include <stdarg.h>
#include <algorithm>
#include <string>
#include <vector>
#include <vd2/system/vdtypes.h>
