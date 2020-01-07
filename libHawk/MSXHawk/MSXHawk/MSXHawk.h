// The following ifdef block is the standard way of creating macros which make exporting
// from a DLL simpler. All files within this DLL are compiled with the MSXHAWK_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see
// MSXHAWK_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef MSXHAWK_EXPORTS
#define MSXHAWK_API __declspec(dllexport)
#else
#define MSXHAWK_API __declspec(dllimport)
#endif

// This class is exported from the dll
class MSXHAWK_API CMSXHawk {
public:
	CMSXHawk(void);
	// TODO: add your methods here.
};

extern MSXHAWK_API int nMSXHawk;

MSXHAWK_API int fnMSXHawk(void);
