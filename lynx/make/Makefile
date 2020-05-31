CXX = g++
RM = rm
CP = cp

MACHINE = $(shell $(CXX) -dumpmachine)
ifneq (,$(findstring i686,$(MACHINE)))
	$(error 32 bit build no longer supported)
else ifneq (,$(findstring x86_64,$(MACHINE)))
	ARCH = 64
else
	$(error Unknown arch)
endif

CXXFLAGS = -Wall -Wno-parentheses -I.. -O3 -std=gnu++11 -fomit-frame-pointer -fno-exceptions -flto -fPIC
TARGET = bizlynx.dll

LDFLAGS_32 = -static -static-libgcc -static-libstdc++
LDFLAGS_64 =
LDFLAGS = -shared $(LDFLAGS_$(ARCH)) $(CXXFLAGS)

DEST_32 = ../../output/dll
DEST_64 = ../../output/dll

SRCS = \
	../c65c02.cpp \
	../cart.cpp \
	../cinterface.cpp \
	../memmap.cpp \
	../mikie.cpp \
	../newstate.cpp \
	../ram.cpp \
	../rom.cpp \
	../sound/Blip_Buffer.cpp \
	../sound/Stereo_Buffer.cpp \
	../susie.cpp \
	../system.cpp

OBJS = $(SRCS:.cpp=.o)

all: $(TARGET)

%.o: %.cpp
	$(CXX) -c -o $@ $< $(CXXFLAGS)

$(TARGET) : $(OBJS)
	$(CXX) -o $@ $(LDFLAGS) $(OBJS)

clean:
	$(RM) $(OBJS)
	$(RM) $(TARGET)
	
install:
	$(CP) $(TARGET) $(DEST_$(ARCH))
