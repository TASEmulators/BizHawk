#ifndef CINTERFACE_H
#define CINTERFACE_H

// these are all documented on the C# side

#ifdef _WIN32
#define GBEXPORT extern "C" __declspec(dllexport)
#elif __linux__
#define GBEXPORT extern "C"
#endif

#endif
