CXX = g++
CXXFLAGS = -Wall -O3 -fpermissive -Wno-unused-but-set-variable \
	-Wno-strict-aliasing -Wzero-as-null-pointer-constant -Wno-unused-variable \
	-Wno-parentheses -Wno-sign-compare -std=gnu++11 -fomit-frame-pointer -fno-exceptions -fPIC

MACHINE = $(shell $(CXX) -dumpmachine)
ifneq (,$(findstring i686,$(MACHINE)))
    $(error 32 bit build no longer supported)
else ifneq (,$(findstring x86_64,$(MACHINE)))
	ARCH = 64
else
	$(error Unknown arch)
endif

LDFLAGS_32 = -static-libgcc -static-libstdc++
LDFLAGS_64 =
LDFLAGS = -shared $(LDFLAGS_$(ARCH)) $(CXXFLAGS)

TARGET = libvbanext.dll
RM = rm
CP = cp

SRCS = \
	../instance.cpp \
	../newstate.cpp

OBJS = $(SRCS:.cpp=.o)
DEST_32 = ../../output/dll
DEST_64 = ../../output/dll

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
