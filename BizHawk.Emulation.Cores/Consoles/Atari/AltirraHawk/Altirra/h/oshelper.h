#ifndef AT_OSHELPER_H
#define AT_OSHELPER_H

#include <vd2/system/vdtypes.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/vectors.h>

struct VDPixmap;
class VDPixmapBuffer;
class VDStringW;

const void *ATLockResource(uint32 id, size_t& size);
bool ATLoadKernelResource(int id, void *dst, uint32 offset, uint32 size, bool allowPartial);
bool ATLoadKernelResource(int id, vdfastvector<uint8>& data);
bool ATLoadKernelResourceLZPacked(int id, vdfastvector<uint8>& data);
bool ATLoadMiscResource(int id, vdfastvector<uint8>& data);
bool ATLoadImageResource(uint32 id, VDPixmapBuffer& buf);

void ATFileSetReadOnlyAttribute(const wchar_t *path, bool readOnly);

void ATCopyFrameToClipboard(const VDPixmap& px);
void ATSaveFrame(const VDPixmap& px, const wchar_t *filename);

void ATCopyTextToClipboard(void *hwnd, const char *s);
void ATCopyTextToClipboard(void *hwnd, const wchar_t *s);

void ATUISaveWindowPlacement(void *hwnd, const char *name);
void ATUISaveWindowPlacement(const char *name, const vdrect32& r, bool isMaximized, uint32 dpi);
void ATUIRestoreWindowPlacement(void *hwnd, const char *name, int nCmdShow = -1, bool sizeOnly = false);

void ATUIEnableEditControlAutoComplete(void *hwnd);

VDStringW ATGetHelpPath();
void ATShowHelp(void *hwnd, const wchar_t *filename);

bool ATIsUserAdministrator();

void ATGenerateGuid(uint8 guid[16]);

#endif
