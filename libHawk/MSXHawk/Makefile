CXX = g++

CFLAGS = -Wall -Wextra -Wno-unknown-pragmas -Wno-unused-parameter -Wno-unused-value -Wno-type-limits -O3 -flto -fvisibility=internal
LFLAGS = -shared

SRCS = $(wildcard MSXHawk/*.cpp)

all:	libmsxhawk

libmsxhawk: $(SRCS)
	$(CXX) $(CFLAGS) $(SRCS) -o ../../Assets/dll/libMSXHawk.so $(LFLAGS)
