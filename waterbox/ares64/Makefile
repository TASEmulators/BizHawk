NEED_LIBCO := 1

ARES_PATH = $(ROOT_DIR)/ares/ares
ANGRYLION_PATH = $(ROOT_DIR)/ares/thirdparty/angrylion-rdp/mylittle-nocomment

CXXFLAGS := -std=c++17 -msse4.2 \
	-I../libco -I.$(ROOT_DIR)/ares/ -I.$(ROOT_DIR)/ares/thirdparty/ -I.$(ARES_PATH) \
	-Werror=int-to-pointer-cast -Wno-unused-but-set-variable -Wno-format-security \
	-Wno-parentheses -Wno-reorder -Wno-unused-variable -Wno-delete-non-virtual-dtor \
	-Wno-sign-compare -Wno-switch -Wno-unused-local-typedefs \
	-fno-strict-aliasing -fwrapv -fno-operator-names \
	-I.$(ANGRYLION_PATH) -DANGRYLION_RDP

TARGET = ares64.wbx

SRCS_PROCESSORS = \
	$(ARES_PATH)/component/processor/sm5k/sm5k.cpp

SRCS_ARES = \
	$(ARES_PATH)/ares/ares.cpp \
	$(ARES_PATH)/ares/memory/fixed-allocator.cpp

SRCS_N64 = \
	$(ARES_PATH)/n64/memory/memory.cpp \
	$(ARES_PATH)/n64/system/system.cpp \
	$(ARES_PATH)/n64/cartridge/cartridge.cpp \
	$(ARES_PATH)/n64/controller/controller.cpp \
	$(ARES_PATH)/n64/dd/dd.cpp \
	$(ARES_PATH)/n64/sp/sp.cpp \
	$(ARES_PATH)/n64/dp/dp.cpp \
	$(ARES_PATH)/n64/mi/mi.cpp \
	$(ARES_PATH)/n64/vi/vi.cpp \
	$(ARES_PATH)/n64/ai/ai.cpp \
	$(ARES_PATH)/n64/pi/pi.cpp \
	$(ARES_PATH)/n64/ri/ri.cpp \
	$(ARES_PATH)/n64/si/si.cpp \
	$(ARES_PATH)/n64/rdram/rdram.cpp \
	$(ARES_PATH)/n64/cpu/cpu.cpp \
	$(ARES_PATH)/n64/rdp/rdp.cpp \
	$(ARES_PATH)/n64/rsp/rsp.cpp

SRCS_ANGRYLION = \
	$(ANGRYLION_PATH)/main.cpp \
	$(ANGRYLION_PATH)/n64video.cpp

SRCS = $(SRCS_PROCESSORS) $(SRCS_ARES) $(SRCS_N64) $(SRCS_ANGRYLION) BizInterface.cpp

include ../common.mak
