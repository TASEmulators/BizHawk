This is the original 6502 cpu core. It was replaced by "MOS 6502X".
It's pretty decent. It does its job well.. it wasnt ever validated for the nitty gritty details:
 1. spurious memory accesses
 2. fine details of irq timing
 3. others?
Moreover, this runs instructions one at a time instead of 6502X's one cycle at a time (several iterations for one cpu instruction)
Therefore, this core might be useful for something where the accuracy doesnt matter that much but more speed is warranted.