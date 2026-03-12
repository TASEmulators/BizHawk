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

CXXFLAGS = -Wall -DLSB_FIRST -I.. -Wno-multichar -O3 -Wzero-as-null-pointer-constant -std=gnu++11 -fomit-frame-pointer -fno-exceptions -flto
TARGET = bizswan.dll

ifeq (,$(findstring MINGW,$(shell uname)))
	CXXFLAGS += -fPIC
endif

LDFLAGS_32 = -static -static-libgcc -static-libstdc++
LDFLAGS_64 =
LDFLAGS = -shared $(LDFLAGS_$(ARCH)) $(CXXFLAGS) -s

DEST_64 = ../../Assets/dll
DESTCOPY_64 = ../../output/dll

SRCS = \
	../eeprom.cpp \
	../gfx.cpp \
	../interrupt.cpp \
	../memory.cpp \
	../newstate.cpp \
	../rtc.cpp \
	../sound.cpp \
	../system.cpp \
	../tcache.cpp \
	../v30mz.cpp \
	../blip/Blip_Buffer.cpp

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
ifneq ("$(wildcard $(DESTCOPY_$(ARCH)))", "")
	$(CP) $(TARGET) $(DESTCOPY_$(ARCH))
endif
