OUT = ..\..\out\Release
OBJ = ..\..\obj\Release\ATBasic
SRC = source

SRCFILES = \
	source\atbasic.s \
	source\data.s \
	source\error.s \
	source\evaluator.s \
	source\exec.s \
	source\functions.s \
	source\io.s \
	source\list.s \
	makefile \
	source\math.s \
	source\memory.s \
	source\parser.s \
	source\parserbytecode.s \
	source\statements.s \
	source\system.inc \
	source\tokens.inc \
	source\util.s \
	source\variables.s

.SUFFIXES: .s .xex .bin

all: madscheck $(OUT)\atbasicx.xex $(OUT)\atbasic.bin

clean:
	del "$(OUT)\atbasicx.xex" "$(OUT)\atbasic.bin"

madscheck: $(OBJ)\madscheck.bin

$(OBJ)\madscheck.bin: makefile
	@if not exist "$(MADS)" ((where "$(MADS)" /q 2>nul) || (echo Error: Cannot find MADS assembler at path: $(MADS). Consult BUILD-HOWTO.html for how to resolve. && exit /b 20))
	@if not exist "$(OBJ)" md "$(OBJ)"
	@echo. >"$(OBJ)\madscheck.bin"

$(OUT)\atbasicx.xex:	$(SRCFILES)
	@if not exist $(OUT) md $(OUT)
	@for %x in (source\atbasic.s) do "$(MADS)" -c -s -d:CART=0 -o:$@ -l:$(@R).lst -t:$(@R).lab "%~fx"
	
$(OUT)\atbasic.bin:		$(SRCFILES)
	@if not exist $(OUT) md $(OUT)
	@for %x in (source\atbasic.s) do "$(MADS)" -c -s -d:CART=1 -b:$$a000 -o:$@ -l:$(@R).lst -t:$(@R).lab "%~fx"

