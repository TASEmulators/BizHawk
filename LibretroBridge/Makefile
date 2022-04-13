CXX = g++

CFLAGS = -Wall -Wextra -O3 -fPIC -fvisibility=hidden
LFLAGS = -shared

SRCS = LibretroBridge.cpp

ifeq ($(OS),Windows_NT)
	TARGET = ../Assets/dll/libLibretroBridge.dll
else
	TARGET = ../Assets/dll/libLibretroBridge.so
endif

all: libLibretroBridge

libLibretroBridge: $(SRCS)
	$(CXX) $(CFLAGS) $(SRCS) -o $(TARGET) $(LFLAGS)
