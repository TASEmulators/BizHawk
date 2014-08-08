CXX = g++
CXXFLAGS = -Wall -O3 -fpermissive -Wno-unused-but-set-variable -Wno-strict-aliasing -Wzero-as-null-pointer-constant -Wno-unused-variable -Wno-parentheses -Wno-sign-compare -std=gnu++11 -fomit-frame-pointer -fno-exceptions
TARGET = libvbanext.dll
LDFLAGS = -shared -static-libgcc -static-libstdc++ $(CXXFLAGS)
RM = rm
CP = cp

SRCS = \
	../instance.cpp \
	../newstate.cpp

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
