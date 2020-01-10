#ifdef _WIN32
#define MSXHAWK_EXPORT extern "C" __declspec(dllexport)
#elif __linux__
#define MSXHAWK_EXPORT extern "C"
#endif
