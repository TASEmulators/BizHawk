CXX = g++

CFLAGS = -Wall -Wextra -Wpedantic -std=c++11 -O3 -fPIC -fvisibility=hidden -fomit-frame-pointer -fno-exceptions
LFLAGS = -shared -s

SRCS = LibretroBridge.cpp

ifeq ($(OS),Windows_NT)
	TARGET = ../Assets/dll/libLibretroBridge.dll
else
	TARGET = ../Assets/dll/libLibretroBridge.so
endif

all: libLibretroBridge

libLibretroBridge: $(SRCS)
	$(CXX) $(CFLAGS) $(SRCS) -o $(TARGET) $(LFLAGS)
