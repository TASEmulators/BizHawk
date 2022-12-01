ROOT_DIR := $(realpath .)
OUTPUTDLL_DIR := $(realpath $(ROOT_DIR)/../../Assets/dll)
OUTPUTDLLCOPY_DIR := $(realpath $(ROOT_DIR)/../../output/dll)

OBJ_DIR := $(ROOT_DIR)/obj

TARGET := msabi_sysv
TARGET_DLL := libbizabiadapter_$(TARGET).dll

OUT_OBJ := $(OBJ_DIR)/$(TARGET).o
OUT_DLL := $(OBJ_DIR)/$(TARGET_DLL)

LDFLAGS := -shared -s

$(OBJ_DIR)/%.o: %.s
	@echo nasm $<
	@mkdir -p $(@D)
	@nasm -fwin64 -o $@ $<

.DEFAULT_GOAL := install

.PHONY: release install

release: $(OUT_DLL)

$(OUT_DLL): $(OUT_OBJ)
	@echo ld $@
	@ld -o $@ $(LDFLAGS) $(OUT_OBJ)

install: $(OUT_DLL)
	@cp -f $< $(OUTPUTDLL_DIR)
	@cp $(OUTPUTDLL_DIR)/$(TARGET_DLL) $(OUTPUTDLLCOPY_DIR)/$(TARGET_DLL) || true
	@echo Release build of $(TARGET_DLL) installed.

.PHONY: clean

clean:
	rm -rf $(OBJ_DIR)
