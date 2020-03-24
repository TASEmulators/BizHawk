#ifdef _WIN32
#define GBHawk_EXPORT extern "C" __declspec(dllexport)
#elif __linux__
#define GBHawk_EXPORT extern "C"
#endif
