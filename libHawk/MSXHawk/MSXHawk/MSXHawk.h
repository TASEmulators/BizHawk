#ifdef _WIN32 // msvc garbage needs this
#define MSXHawk_EXPORT extern "C" __declspec(dllexport)
#else
#define MSXHawk_EXPORT extern "C" __attribute__((visibility("default")))
#endif
