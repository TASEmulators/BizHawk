#ifdef _WIN32
#define MSXHawk_EXPORT extern "C" __declspec(dllexport)
#elif __linux__
#define MSXHawk_EXPORT extern "C"
#endif
