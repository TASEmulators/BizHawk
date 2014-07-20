CXX = g++
CXXFLAGS = -Wall -DLSB_FIRST -I.. -Wno-multichar -O3 -Wzero-as-null-pointer-constant -std=gnu++11 -fomit-frame-pointer -fno-exceptions
TARGET = bizswan.dll
LDFLAGS = -shared -static-libgcc -static-libstdc++ $(CXXFLAGS)
RM = rm
CP = cp

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
	../Blip/Blip_Buffer.cpp

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
	$(CP) $(TARGET) ../../output/dll
