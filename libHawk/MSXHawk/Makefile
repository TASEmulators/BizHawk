CXX = g++

CFLAGS = -Wall -Wextra -O3 -flto -fvisibility=internal
LFLAGS = -shared

SRCS = $(wildcard MSXHawk/*.cpp)

all:	libmsxhawk

libmsxhawk: $(SRCS)
	$(CXX) $(CFLAGS) $(SRCS) -o ../../Assets/dll/MSXHawk.so $(LFLAGS)
