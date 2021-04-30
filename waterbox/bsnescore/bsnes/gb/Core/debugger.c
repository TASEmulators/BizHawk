#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include "gb.h"

typedef struct {
    bool has_bank;
    uint16_t bank:9;
    uint16_t value;
} value_t;

typedef struct {
    enum {
        LVALUE_MEMORY,
        LVALUE_MEMORY16,
        LVALUE_REG16,
        LVALUE_REG_H,
        LVALUE_REG_L,
    } kind;
    union {
        uint16_t *register_address;
        value_t memory_address;
    };
} lvalue_t;

#define VALUE_16(x) ((value_t){false, 0, (x)})

struct GB_breakpoint_s {
    union {
        struct {
        uint16_t addr;
        uint16_t bank; /* -1 = any bank*/
        };
        uint32_t key; /* For sorting and comparing */
    };
    char *condition;
    bool is_jump_to;
};

#define BP_KEY(x) (((struct GB_breakpoint_s){.addr = ((x).value), .bank = (x).has_bank? (x).bank : -1 }).key)

#define GB_WATCHPOINT_R (1)
#define GB_WATCHPOINT_W (2)

struct GB_watchpoint_s {
    union {
        struct {
            uint16_t addr;
            uint16_t bank; /* -1 = any bank*/
        };
        uint32_t key; /* For sorting and comparing */
    };
    char *condition;
    uint8_t flags;
};

#define WP_KEY(x) (((struct GB_watchpoint_s){.addr = ((x).value), .bank = (x).has_bank? (x).bank : -1 }).key)

static uint16_t bank_for_addr(GB_gameboy_t *gb, uint16_t addr)
{
    if (addr < 0x4000) {
        return gb->mbc_rom0_bank;
    }

    if (addr < 0x8000) {
        return gb->mbc_rom_bank;
    }

    if (addr < 0xD000) {
        return 0;
    }

    if (addr < 0xE000) {
        return gb->cgb_ram_bank;
    }

    return 0;
}

typedef struct {
    uint16_t rom0_bank;
    uint16_t rom_bank;
    uint8_t mbc_ram_bank;
    bool mbc_ram_enable;
    uint8_t ram_bank;
    uint8_t vram_bank;
} banking_state_t;

static inline void save_banking_state(GB_gameboy_t *gb, banking_state_t *state)
{
    state->rom0_bank = gb->mbc_rom0_bank;
    state->rom_bank = gb->mbc_rom_bank;
    state->mbc_ram_bank = gb->mbc_ram_bank;
    state->mbc_ram_enable = gb->mbc_ram_enable;
    state->ram_bank = gb->cgb_ram_bank;
    state->vram_bank = gb->cgb_vram_bank;
}

static inline void restore_banking_state(GB_gameboy_t *gb, banking_state_t *state)
{

    gb->mbc_rom0_bank = state->rom0_bank;
    gb->mbc_rom_bank = state->rom_bank;
    gb->mbc_ram_bank = state->mbc_ram_bank;
    gb->mbc_ram_enable = state->mbc_ram_enable;
    gb->cgb_ram_bank = state->ram_bank;
    gb->cgb_vram_bank = state->vram_bank;
}

static inline void switch_banking_state(GB_gameboy_t *gb, uint16_t bank)
{
    gb->mbc_rom0_bank = bank;
    gb->mbc_rom_bank = bank;
    gb->mbc_ram_bank = bank;
    gb->mbc_ram_enable = true;
    if (GB_is_cgb(gb)) {
        gb->cgb_ram_bank = bank & 7;
        gb->cgb_vram_bank = bank & 1;
        if (gb->cgb_ram_bank == 0) {
            gb->cgb_ram_bank = 1;
        }
    }
}

static const char *value_to_string(GB_gameboy_t *gb, uint16_t value, bool prefer_name)
{
    static __thread char output[256];
    const GB_bank_symbol_t *symbol = GB_debugger_find_symbol(gb, value);

    if (symbol && (value - symbol->addr > 0x1000 || symbol->addr == 0) ) {
        symbol = NULL;
    }

    /* Avoid overflow */
    if (symbol && strlen(symbol->name) >= 240) {
        symbol = NULL;
    }

    if (!symbol) {
        sprintf(output, "$%04x", value);
    }

    else if (symbol->addr == value) {
        if (prefer_name) {
            sprintf(output, "%s ($%04x)", symbol->name, value);
        }
        else {
            sprintf(output, "$%04x (%s)", value, symbol->name);
        }
    }

    else {
        if (prefer_name) {
            sprintf(output, "%s+$%03x ($%04x)", symbol->name, value - symbol->addr, value);
        }
        else {
            sprintf(output, "$%04x (%s+$%03x)", value, symbol->name, value - symbol->addr);
        }
    }
    return output;
}

static const char *debugger_value_to_string(GB_gameboy_t *gb, value_t value, bool prefer_name)
{
    if (!value.has_bank) return value_to_string(gb, value.value, prefer_name);

    static __thread char output[256];
    const GB_bank_symbol_t *symbol = GB_map_find_symbol(gb->bank_symbols[value.bank], value.value);

    if (symbol && (value.value - symbol->addr > 0x1000 || symbol->addr == 0) ) {
        symbol = NULL;
    }

    /* Avoid overflow */
    if (symbol && strlen(symbol->name) >= 240) {
        symbol = NULL;
    }

    if (!symbol) {
        sprintf(output, "$%02x:$%04x", value.bank, value.value);
    }

    else if (symbol->addr == value.value) {
        if (prefer_name) {
            sprintf(output, "%s ($%02x:$%04x)", symbol->name, value.bank, value.value);
        }
        else {
            sprintf(output, "$%02x:$%04x (%s)", value.bank, value.value, symbol->name);
        }
    }

    else {
        if (prefer_name) {
            sprintf(output, "%s+$%03x ($%02x:$%04x)", symbol->name, value.value - symbol->addr, value.bank, value.value);
        }
        else {
            sprintf(output, "$%02x:$%04x (%s+$%03x)", value.bank, value.value, symbol->name, value.value - symbol->addr);
        }
    }
    return output;
}

static value_t read_lvalue(GB_gameboy_t *gb, lvalue_t lvalue)
{
    /* Not used until we add support for operators like += */
    switch (lvalue.kind) {
        case LVALUE_MEMORY:
            if (lvalue.memory_address.has_bank) {
                banking_state_t state;
                save_banking_state(gb, &state);
                switch_banking_state(gb, lvalue.memory_address.bank);
                value_t r = VALUE_16(GB_read_memory(gb, lvalue.memory_address.value));
                restore_banking_state(gb, &state);
                return r;
            }
            return VALUE_16(GB_read_memory(gb, lvalue.memory_address.value));

        case LVALUE_MEMORY16:
            if (lvalue.memory_address.has_bank) {
                banking_state_t state;
                save_banking_state(gb, &state);
                switch_banking_state(gb, lvalue.memory_address.bank);
                value_t r = VALUE_16(GB_read_memory(gb, lvalue.memory_address.value) |
                                   (GB_read_memory(gb, lvalue.memory_address.value + 1) * 0x100));
                restore_banking_state(gb, &state);
                return r;
            }
            return VALUE_16(GB_read_memory(gb, lvalue.memory_address.value) |
                            (GB_read_memory(gb, lvalue.memory_address.value + 1) * 0x100));

        case LVALUE_REG16:
            return VALUE_16(*lvalue.register_address);

        case LVALUE_REG_L:
            return VALUE_16(*lvalue.register_address & 0x00FF);

        case LVALUE_REG_H:
            return VALUE_16(*lvalue.register_address >> 8);
    }

    return VALUE_16(0);
}

static void write_lvalue(GB_gameboy_t *gb, lvalue_t lvalue, uint16_t value)
{
    switch (lvalue.kind) {
        case LVALUE_MEMORY:
            if (lvalue.memory_address.has_bank) {
                banking_state_t state;
                save_banking_state(gb, &state);
                switch_banking_state(gb, lvalue.memory_address.bank);
                GB_write_memory(gb, lvalue.memory_address.value, value);
                restore_banking_state(gb, &state);
                return;
            }
            GB_write_memory(gb, lvalue.memory_address.value, value);
            return;

        case LVALUE_MEMORY16:
            if (lvalue.memory_address.has_bank) {
                banking_state_t state;
                save_banking_state(gb, &state);
                switch_banking_state(gb, lvalue.memory_address.bank);
                GB_write_memory(gb, lvalue.memory_address.value, value);
                GB_write_memory(gb, lvalue.memory_address.value + 1, value >> 8);
                restore_banking_state(gb, &state);
                return;
            }
            GB_write_memory(gb, lvalue.memory_address.value, value);
            GB_write_memory(gb, lvalue.memory_address.value + 1, value >> 8);
            return;

        case LVALUE_REG16:
            *lvalue.register_address = value;
            return;

        case LVALUE_REG_L:
            *lvalue.register_address &= 0xFF00;
            *lvalue.register_address |= value & 0xFF;
            return;

        case LVALUE_REG_H:
            *lvalue.register_address &= 0x00FF;
            *lvalue.register_address |= value << 8;
            return;
    }
}

/* 16 bit value   <op> 16 bit value   = 16 bit value
   25 bit address <op> 16 bit value   = 25 bit address
   16 bit value   <op> 25 bit address = 25 bit address
   25 bit address <op> 25 bit address = 16 bit value (since adding pointers, for examples, makes no sense)

   Boolean operators always return a 16-bit value
   */
#define FIX_BANK(x) ((value_t){a.has_bank ^ b.has_bank, a.has_bank? a.bank : b.bank, (x)})

static value_t add(value_t a, value_t b) {return FIX_BANK(a.value + b.value);}
static value_t sub(value_t a, value_t b) {return FIX_BANK(a.value - b.value);}
static value_t mul(value_t a, value_t b) {return FIX_BANK(a.value * b.value);}
static value_t _div(value_t a, value_t b) 
{
    if (b.value == 0) {
        return FIX_BANK(0);
    }
    return FIX_BANK(a.value / b.value);
};
static value_t mod(value_t a, value_t b) 
{
    if (b.value == 0) {
        return FIX_BANK(0);
    }
    return FIX_BANK(a.value % b.value);
};
static value_t and(value_t a, value_t b) {return FIX_BANK(a.value & b.value);}
static value_t or(value_t a, value_t b) {return FIX_BANK(a.value | b.value);}
static value_t xor(value_t a, value_t b) {return FIX_BANK(a.value ^ b.value);}
static value_t shleft(value_t a, value_t b) {return FIX_BANK(a.value << b.value);}
static value_t shright(value_t a, value_t b) {return FIX_BANK(a.value >> b.value);}
static value_t assign(GB_gameboy_t *gb, lvalue_t a, uint16_t b)
{
    write_lvalue(gb, a, b);
    return read_lvalue(gb, a);
}

static value_t bool_and(value_t a, value_t b) {return VALUE_16(a.value && b.value);}
static value_t bool_or(value_t a, value_t b) {return VALUE_16(a.value || b.value);}
static value_t equals(value_t a, value_t b) {return VALUE_16(a.value == b.value);}
static value_t different(value_t a, value_t b) {return VALUE_16(a.value != b.value);}
static value_t lower(value_t a, value_t b) {return VALUE_16(a.value < b.value);}
static value_t greater(value_t a, value_t b) {return VALUE_16(a.value > b.value);}
static value_t lower_equals(value_t a, value_t b) {return VALUE_16(a.value <= b.value);}
static value_t greater_equals(value_t a, value_t b) {return VALUE_16(a.value >= b.value);}
static value_t bank(value_t a, value_t b) {return (value_t) {true, a.value, b.value};}


static struct {
    const char *string;
    int8_t priority;
    value_t (*operator)(value_t, value_t);
    value_t (*lvalue_operator)(GB_gameboy_t *, lvalue_t, uint16_t);
} operators[] =
{
    // Yes. This is not C-like. But it makes much more sense.
    // Deal with it.
    {"+", 0, add},
    {"-", 0, sub},
    {"||", 0, bool_or},
    {"|", 0, or},
    {"*", 1, mul},
    {"/", 1, _div},
    {"%", 1, mod},
    {"&&", 1, bool_and},
    {"&", 1, and},
    {"^", 1, xor},
    {"<<", 2, shleft},
    {"<=", 3, lower_equals},
    {"<", 3, lower},
    {">>", 2, shright},
    {">=", 3, greater_equals},
    {">", 3, greater},
    {"==", 3, equals},
    {"=", -1, NULL, assign},
    {"!=", 3, different},
    {":", 4, bank},
};

value_t debugger_evaluate(GB_gameboy_t *gb, const char *string,
                           size_t length, bool *error,
                           uint16_t *watchpoint_address, uint8_t *watchpoint_new_value);

static lvalue_t debugger_evaluate_lvalue(GB_gameboy_t *gb, const char *string,
                                         size_t length, bool *error,
                                         uint16_t *watchpoint_address, uint8_t *watchpoint_new_value)
{
    *error = false;
    // Strip whitespace
    while (length && (string[0] == ' ' || string[0] == '\n' || string[0] == '\r' || string[0] == '\t')) {
        string++;
        length--;
    }
    while (length && (string[length-1] == ' ' || string[length-1] == '\n' || string[length-1] == '\r' || string[length-1] == '\t')) {
        length--;
    }
    if (length == 0) { 
        GB_log(gb, "Expected expression.\n");
        *error = true;
        return (lvalue_t){0,};
    }
    if (string[0] == '(' && string[length - 1] == ')') {
        // Attempt to strip parentheses
        signed depth = 0;
        for (unsigned i = 0; i < length; i++) {
            if (string[i] == '(') depth++;
            if (depth == 0) {
                // First and last are not matching
                depth = 1;
                break;
            }
            if (string[i] == ')') depth--;
        }
        if (depth == 0) return debugger_evaluate_lvalue(gb, string + 1, length - 2, error, watchpoint_address, watchpoint_new_value);
    }
    else if (string[0] == '[' && string[length - 1] == ']') {
        // Attempt to strip square parentheses (memory dereference)
        signed depth = 0;
        for (unsigned i = 0; i < length; i++) {
            if (string[i] == '[') depth++;
            if (depth == 0) {
                // First and last are not matching
                depth = 1;
                break;
            }
            if (string[i] == ']') depth--;
        }
        if (depth == 0) {
            return (lvalue_t){LVALUE_MEMORY, .memory_address = debugger_evaluate(gb, string + 1, length - 2, error, watchpoint_address, watchpoint_new_value)};
        }
    }
    else if (string[0] == '{' && string[length - 1] == '}') {
        // Attempt to strip curly parentheses (memory dereference)
        signed depth = 0;
        for (unsigned i = 0; i < length; i++) {
            if (string[i] == '{') depth++;
            if (depth == 0) {
                // First and last are not matching
                depth = 1;
                break;
            }
            if (string[i] == '}') depth--;
        }
        if (depth == 0) {
            return (lvalue_t){LVALUE_MEMORY16, .memory_address = debugger_evaluate(gb, string + 1, length - 2, error, watchpoint_address, watchpoint_new_value)};
        }
    }

    // Registers
    if (string[0] != '$' && (string[0] < '0' || string[0] > '9')) {
        if (length == 1) {
            switch (string[0]) {
                case 'a': return (lvalue_t){LVALUE_REG_H, .register_address = &gb->registers[GB_REGISTER_AF]};
                case 'f': return (lvalue_t){LVALUE_REG_L, .register_address = &gb->registers[GB_REGISTER_AF]};
                case 'b': return (lvalue_t){LVALUE_REG_H, .register_address = &gb->registers[GB_REGISTER_BC]};
                case 'c': return (lvalue_t){LVALUE_REG_L, .register_address = &gb->registers[GB_REGISTER_BC]};
                case 'd': return (lvalue_t){LVALUE_REG_H, .register_address = &gb->registers[GB_REGISTER_DE]};
                case 'e': return (lvalue_t){LVALUE_REG_L, .register_address = &gb->registers[GB_REGISTER_DE]};
                case 'h': return (lvalue_t){LVALUE_REG_H, .register_address = &gb->registers[GB_REGISTER_HL]};
                case 'l': return (lvalue_t){LVALUE_REG_L, .register_address = &gb->registers[GB_REGISTER_HL]};
            }
        }
        else if (length == 2) {
            switch (string[0]) {
                case 'a': if (string[1] == 'f') return (lvalue_t){LVALUE_REG16, .register_address = &gb->registers[GB_REGISTER_AF]};
                case 'b': if (string[1] == 'c') return (lvalue_t){LVALUE_REG16, .register_address = &gb->registers[GB_REGISTER_BC]};
                case 'd': if (string[1] == 'e') return (lvalue_t){LVALUE_REG16, .register_address = &gb->registers[GB_REGISTER_DE]};
                case 'h': if (string[1] == 'l') return (lvalue_t){LVALUE_REG16, .register_address = &gb->registers[GB_REGISTER_HL]};
                case 's': if (string[1] == 'p') return (lvalue_t){LVALUE_REG16, .register_address = &gb->registers[GB_REGISTER_SP]};
                case 'p': if (string[1] == 'c') return (lvalue_t){LVALUE_REG16, .register_address = &gb->pc};
            }
        }
        GB_log(gb, "Unknown register: %.*s\n", (unsigned) length, string);
        *error = true;
        return (lvalue_t){0,};
    }

    GB_log(gb, "Expression is not an lvalue: %.*s\n", (unsigned) length, string);
    *error = true;
    return (lvalue_t){0,};
}

#define ERROR ((value_t){0,})
value_t debugger_evaluate(GB_gameboy_t *gb, const char *string,
                          size_t length, bool *error,
                          uint16_t *watchpoint_address, uint8_t *watchpoint_new_value)
{
    /* Disable watchpoints while evaulating expressions */
    uint16_t n_watchpoints = gb->n_watchpoints;
    gb->n_watchpoints = 0;

    value_t ret = ERROR;

    *error = false;
    // Strip whitespace
    while (length && (string[0] == ' ' || string[0] == '\n' || string[0] == '\r' || string[0] == '\t')) {
        string++;
        length--;
    }
    while (length && (string[length-1] == ' ' || string[length-1] == '\n' || string[length-1] == '\r' || string[length-1] == '\t')) {
        length--;
    }
    if (length == 0) { 
        GB_log(gb, "Expected expression.\n");
        *error = true;
        goto exit;
    }
    if (string[0] == '(' && string[length - 1] == ')') {
        // Attempt to strip parentheses
        signed depth = 0;
        for (unsigned i = 0; i < length; i++) {
            if (string[i] == '(') depth++;
            if (depth == 0) {
                // First and last are not matching
                depth = 1;
                break;
            }
            if (string[i] == ')') depth--;
        }
        if (depth == 0) {
            ret = debugger_evaluate(gb, string + 1, length - 2, error, watchpoint_address, watchpoint_new_value);
            goto exit;
        }
    }
    else if (string[0] == '[' && string[length - 1] == ']') {
        // Attempt to strip square parentheses (memory dereference)
        signed depth = 0;
        for (unsigned i = 0; i < length; i++) {
            if (string[i] == '[') depth++;
            if (depth == 0) {
                // First and last are not matching
                depth = 1;
                break;
            }
            if (string[i] == ']') depth--;
        }

        if (depth == 0) {
            value_t addr = debugger_evaluate(gb, string + 1, length - 2, error, watchpoint_address, watchpoint_new_value);
            banking_state_t state;
            if (addr.bank) {
                save_banking_state(gb, &state);
                switch_banking_state(gb, addr.bank);
            }
            ret = VALUE_16(GB_read_memory(gb, addr.value));
            if (addr.bank) {
                restore_banking_state(gb, &state);
            }
            goto exit;
        }
    }
    else if (string[0] == '{' && string[length - 1] == '}') {
        // Attempt to strip curly parentheses (memory dereference)
        signed depth = 0;
        for (unsigned i = 0; i < length; i++) {
            if (string[i] == '{') depth++;
            if (depth == 0) {
                // First and last are not matching
                depth = 1;
                break;
            }
            if (string[i] == '}') depth--;
        }

        if (depth == 0) {
            value_t addr = debugger_evaluate(gb, string + 1, length - 2, error, watchpoint_address, watchpoint_new_value);
            banking_state_t state;
            if (addr.bank) {
                save_banking_state(gb, &state);
                switch_banking_state(gb, addr.bank);
            }
            ret = VALUE_16(GB_read_memory(gb, addr.value) | (GB_read_memory(gb, addr.value + 1) * 0x100));
            if (addr.bank) {
                restore_banking_state(gb, &state);
            }
            goto exit;
        }
    }
    // Search for lowest priority operator
    signed depth = 0;
    unsigned operator_index = -1;
    unsigned operator_pos = 0;
    for (unsigned i = 0; i < length; i++) {
        if (string[i] == '(') depth++;
        else if (string[i] == ')') depth--;
        else if (string[i] == '[') depth++;
        else if (string[i] == ']') depth--;
        else if (depth == 0) {
            for (unsigned j = 0; j < sizeof(operators) / sizeof(operators[0]); j++) {
                unsigned operator_length = strlen(operators[j].string);
                if (operator_length > length - i) continue; // Operator too long
                
                if (memcmp(string + i, operators[j].string, operator_length) == 0) {
                    if (operator_index != -1 && operators[operator_index].priority < operators[j].priority) {
                        /* for supporting = vs ==, etc*/
                        i += operator_length - 1;
                        break;
                    }
                    // Found an operator!
                    operator_pos = i;
                    operator_index = j;
                    /* for supporting = vs ==, etc*/
                    i += operator_length - 1;
                    break;
                }
            }
        }
    }
    if (operator_index != -1) {
        unsigned right_start = (unsigned)(operator_pos + strlen(operators[operator_index].string));
        value_t right = debugger_evaluate(gb, string + right_start, length - right_start, error, watchpoint_address, watchpoint_new_value);
        if (*error) goto exit;
        if (operators[operator_index].lvalue_operator) {
            lvalue_t left = debugger_evaluate_lvalue(gb, string, operator_pos, error, watchpoint_address, watchpoint_new_value);
            if (*error) goto exit;
            ret = operators[operator_index].lvalue_operator(gb, left, right.value);
            goto exit;
        }
        value_t left = debugger_evaluate(gb, string, operator_pos, error, watchpoint_address, watchpoint_new_value);
        if (*error) goto exit;
        ret = operators[operator_index].operator(left, right);
        goto exit;
    }

    // Not an expression - must be a register or a literal

    // Registers
    if (string[0] != '$' && (string[0] < '0' || string[0] > '9')) {
        if (length == 1) {
            switch (string[0]) {
                case 'a': ret = VALUE_16(gb->registers[GB_REGISTER_AF] >> 8); goto exit;
                case 'f': ret = VALUE_16(gb->registers[GB_REGISTER_AF] & 0xFF); goto exit;
                case 'b': ret = VALUE_16(gb->registers[GB_REGISTER_BC] >> 8); goto exit;
                case 'c': ret = VALUE_16(gb->registers[GB_REGISTER_BC] & 0xFF); goto exit;
                case 'd': ret = VALUE_16(gb->registers[GB_REGISTER_DE] >> 8); goto exit;
                case 'e': ret = VALUE_16(gb->registers[GB_REGISTER_DE] & 0xFF); goto exit;
                case 'h': ret = VALUE_16(gb->registers[GB_REGISTER_HL] >> 8); goto exit;
                case 'l': ret = VALUE_16(gb->registers[GB_REGISTER_HL] & 0xFF); goto exit;
            }
        }
        else if (length == 2) {
            switch (string[0]) {
                case 'a': if (string[1] == 'f') {ret = VALUE_16(gb->registers[GB_REGISTER_AF]); goto exit;}
                case 'b': if (string[1] == 'c') {ret = VALUE_16(gb->registers[GB_REGISTER_BC]); goto exit;}
                case 'd': if (string[1] == 'e') {ret = VALUE_16(gb->registers[GB_REGISTER_DE]); goto exit;}
                case 'h': if (string[1] == 'l') {ret = VALUE_16(gb->registers[GB_REGISTER_HL]); goto exit;}
                case 's': if (string[1] == 'p') {ret = VALUE_16(gb->registers[GB_REGISTER_SP]); goto exit;}
                case 'p': if (string[1] == 'c') {ret = (value_t){true, bank_for_addr(gb, gb->pc), gb->pc};  goto exit;}
            }
        }
        else if (length == 3) {
            if (watchpoint_address && memcmp(string, "old", 3) == 0) {
                ret = VALUE_16(GB_read_memory(gb, *watchpoint_address));
                goto exit;
            }

            if (watchpoint_new_value && memcmp(string, "new", 3) == 0) {
                ret = VALUE_16(*watchpoint_new_value);
                goto exit;
            }

            /* $new is identical to $old in read conditions */
            if (watchpoint_address && memcmp(string, "new", 3) == 0) {
                ret = VALUE_16(GB_read_memory(gb, *watchpoint_address));
                goto exit;
            }
        }

        char symbol_name[length + 1];
        memcpy(symbol_name, string, length);
        symbol_name[length] = 0;
        const GB_symbol_t *symbol = GB_reversed_map_find_symbol(&gb->reversed_symbol_map, symbol_name);
        if (symbol) {
            ret = (value_t){true, symbol->bank, symbol->addr};
            goto exit;
        }

        GB_log(gb, "Unknown register or symbol: %.*s\n", (unsigned) length, string);
        *error = true;
        goto exit;
    }

    char *end;
    unsigned base = 10;
    if (string[0] == '$') {
        string++;
        base = 16;
        length--;
    }
    uint16_t literal = (uint16_t) (strtol(string, &end, base));
    if (end != string + length) {
        GB_log(gb, "Failed to parse: %.*s\n", (unsigned) length, string);
        *error = true;
        goto exit;
    }
    ret = VALUE_16(literal);
exit:
    gb->n_watchpoints = n_watchpoints;
    return ret;
}

struct debugger_command_s;
typedef bool debugger_command_imp_t(GB_gameboy_t *gb, char *arguments, char *modifiers, const struct debugger_command_s *command);
typedef char *debugger_completer_imp_t(GB_gameboy_t *gb, const char *string, uintptr_t *context);

typedef struct debugger_command_s {
    const char *command;
    uint8_t min_length;
    debugger_command_imp_t *implementation;
    const char *help_string; // Null if should not appear in help
    const char *arguments_format; // For usage message
    const char *modifiers_format; // For usage message
    debugger_completer_imp_t *argument_completer;
    debugger_completer_imp_t *modifiers_completer;
} debugger_command_t;

static const char *lstrip(const char *str)
{
    while (*str == ' ' || *str == '\t') {
        str++;
    }
    return str;
}

#define STOPPED_ONLY \
if (!gb->debug_stopped) { \
GB_log(gb, "Program is running. \n"); \
return false; \
}

#define NO_MODIFIERS \
if (modifiers) { \
print_usage(gb, command); \
return true; \
}

static void print_usage(GB_gameboy_t *gb, const debugger_command_t *command)
{
    GB_log(gb, "Usage: %s", command->command);

    if (command->modifiers_format) {
        GB_log(gb, "[/%s]", command->modifiers_format);
    }

    if (command->arguments_format) {
        GB_log(gb, " %s", command->arguments_format);
    }

    GB_log(gb, "\n");
}

static bool cont(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    NO_MODIFIERS
    STOPPED_ONLY

    if (strlen(lstrip(arguments))) {
        print_usage(gb, command);
        return true;
    }

    gb->debug_stopped = false;
    return false;
}

static bool next(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    NO_MODIFIERS
    STOPPED_ONLY

    if (strlen(lstrip(arguments))) {
        print_usage(gb, command);
        return true;
    }

    gb->debug_stopped = false;
    gb->debug_next_command = true;
    gb->debug_call_depth = 0;
    return false;
}

static bool step(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    NO_MODIFIERS
    STOPPED_ONLY

    if (strlen(lstrip(arguments))) {
        print_usage(gb, command);
        return true;
    }

    return false;
}

static bool finish(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    NO_MODIFIERS
    STOPPED_ONLY

    if (strlen(lstrip(arguments))) {
        print_usage(gb, command);
        return true;
    }

    gb->debug_stopped = false;
    gb->debug_fin_command = true;
    gb->debug_call_depth = 0;
    return false;
}

static bool stack_leak_detection(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    NO_MODIFIERS
    STOPPED_ONLY

    if (strlen(lstrip(arguments))) {
        print_usage(gb, command);
        return true;
    }

    gb->debug_stopped = false;
    gb->stack_leak_detection = true;
    gb->debug_call_depth = 0;
    return false;
}

static bool registers(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    NO_MODIFIERS
    if (strlen(lstrip(arguments))) {
        print_usage(gb, command);
        return true;
    }


    GB_log(gb, "AF  = $%04x (%c%c%c%c)\n", gb->registers[GB_REGISTER_AF], /* AF can't really be an address */
           (gb->f & GB_CARRY_FLAG)?      'C' : '-',
           (gb->f & GB_HALF_CARRY_FLAG)? 'H' : '-',
           (gb->f & GB_SUBTRACT_FLAG)?   'N' : '-',
           (gb->f & GB_ZERO_FLAG)?       'Z' : '-');
    GB_log(gb, "BC  = %s\n", value_to_string(gb, gb->registers[GB_REGISTER_BC], false));
    GB_log(gb, "DE  = %s\n", value_to_string(gb, gb->registers[GB_REGISTER_DE], false));
    GB_log(gb, "HL  = %s\n", value_to_string(gb, gb->registers[GB_REGISTER_HL], false));
    GB_log(gb, "SP  = %s\n", value_to_string(gb, gb->registers[GB_REGISTER_SP], false));
    GB_log(gb, "PC  = %s\n", value_to_string(gb, gb->pc, false));
    GB_log(gb, "IME = %s\n", gb->ime? "Enabled" : "Disabled");
    return true;
}

static char *on_off_completer(GB_gameboy_t *gb, const char *string, uintptr_t *context)
{
    size_t length = strlen(string);
    const char *suggestions[] = {"on", "off"};
    while (*context < sizeof(suggestions) / sizeof(suggestions[0])) {
        if (memcmp(string, suggestions[*context], length) == 0) {
            return strdup(suggestions[(*context)++] + length);
        }
        (*context)++;
    }
    return NULL;
}

/* Enable or disable software breakpoints */
static bool softbreak(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    NO_MODIFIERS
    if (strcmp(lstrip(arguments), "on") == 0 || !strlen(lstrip(arguments))) {
        gb->has_software_breakpoints = true;
    }
    else if (strcmp(lstrip(arguments), "off") == 0) {
        gb->has_software_breakpoints = false;
    }
    else {
        print_usage(gb, command);
    }

    return true;
}

/* Find the index of the closest breakpoint equal or greater to addr */
static uint16_t find_breakpoint(GB_gameboy_t *gb, value_t addr)
{
    if (!gb->breakpoints) {
        return 0;
    }

    uint32_t key = BP_KEY(addr);

    unsigned min = 0;
    unsigned max = gb->n_breakpoints;
    while (min < max) {
        uint16_t pivot = (min + max) / 2;
        if (gb->breakpoints[pivot].key == key) return pivot;
        if (gb->breakpoints[pivot].key > key) {
            max = pivot;
        }
        else {
            min = pivot + 1;
        }
    }
    return (uint16_t) min;
}

static inline bool is_legal_symbol_char(char c)
{
    if (c >= '0' && c <= '9') return true;
    if (c >= 'A' && c <= 'Z') return true;
    if (c >= 'a' && c <= 'z') return true;
    if (c == '_') return true;
    if (c == '.') return true;
    return false;
}

static char *symbol_completer(GB_gameboy_t *gb, const char *string, uintptr_t *_context)
{
    const char *symbol_prefix = string;
    while (*string) {
        if (!is_legal_symbol_char(*string)) {
            symbol_prefix = string + 1;
        }
        string++;
    }
    
    if (*symbol_prefix == '$') {
        return NULL;
    }
    
    struct {
        uint16_t bank;
        uint32_t symbol;
    } *context = (void *)_context;
    
    
    size_t length = strlen(symbol_prefix);
    while (context->bank < 0x200) {
        if (gb->bank_symbols[context->bank] == NULL ||
            context->symbol >= gb->bank_symbols[context->bank]->n_symbols) {
            context->bank++;
            context->symbol = 0;
            continue;
        }
        const char *candidate = gb->bank_symbols[context->bank]->symbols[context->symbol++].name;
        if (memcmp(symbol_prefix, candidate, length) == 0) {
            return strdup(candidate + length);
        }
    }
    return NULL;
}

static char *j_completer(GB_gameboy_t *gb, const char *string, uintptr_t *context)
{
    size_t length = strlen(string);
    const char *suggestions[] = {"j"};
    while (*context < sizeof(suggestions) / sizeof(suggestions[0])) {
        if (memcmp(string, suggestions[*context], length) == 0) {
            return strdup(suggestions[(*context)++] + length);
        }
        (*context)++;
    }
    return NULL;
}

static bool breakpoint(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    bool is_jump_to = true;
    if (!modifiers) {
        is_jump_to = false;
    }
    else if (strcmp(modifiers, "j") != 0) {
        print_usage(gb, command);
        return true;
    }

    if (strlen(lstrip(arguments)) == 0) {
        print_usage(gb, command);
        return true;
    }

    if (gb->n_breakpoints == (typeof(gb->n_breakpoints)) -1) {
        GB_log(gb, "Too many breakpoints set\n");
        return true;
    }

    char *condition = NULL;
    if ((condition = strstr(arguments, " if "))) {
        *condition = 0;
        condition += strlen(" if ");
        /* Verify condition is sane (Todo: This might have side effects!) */
        bool error;
        debugger_evaluate(gb, condition, (unsigned)strlen(condition), &error, NULL, NULL);
        if (error) return true;

    }

    bool error;
    value_t result = debugger_evaluate(gb, arguments, (unsigned)strlen(arguments), &error, NULL, NULL);
    uint32_t key = BP_KEY(result);

    if (error) return true;

    uint16_t index = find_breakpoint(gb, result);
    if (index < gb->n_breakpoints && gb->breakpoints[index].key == key) {
        GB_log(gb, "Breakpoint already set at %s\n", debugger_value_to_string(gb, result, true));
        if (!gb->breakpoints[index].condition && condition) {
            GB_log(gb, "Added condition to breakpoint\n");
            gb->breakpoints[index].condition = strdup(condition);
        }
        else if (gb->breakpoints[index].condition && condition) {
            GB_log(gb, "Replaced breakpoint condition\n");
            free(gb->breakpoints[index].condition);
            gb->breakpoints[index].condition = strdup(condition);
        }
        else if (gb->breakpoints[index].condition && !condition) {
            GB_log(gb, "Removed breakpoint condition\n");
            free(gb->breakpoints[index].condition);
            gb->breakpoints[index].condition = NULL;
        }
        return true;
    }

    gb->breakpoints = realloc(gb->breakpoints, (gb->n_breakpoints + 1) * sizeof(gb->breakpoints[0]));
    memmove(&gb->breakpoints[index + 1], &gb->breakpoints[index], (gb->n_breakpoints - index) * sizeof(gb->breakpoints[0]));
    gb->breakpoints[index].key = key;

    if (condition) {
        gb->breakpoints[index].condition = strdup(condition);
    }
    else {
        gb->breakpoints[index].condition = NULL;
    }
    gb->n_breakpoints++;

    gb->breakpoints[index].is_jump_to = is_jump_to;

    if (is_jump_to) {
        gb->has_jump_to_breakpoints = true;
    }

    GB_log(gb, "Breakpoint set at %s\n", debugger_value_to_string(gb, result, true));
    return true;
}

static bool delete(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    NO_MODIFIERS
    if (strlen(lstrip(arguments)) == 0) {
        for (unsigned i = gb->n_breakpoints; i--;) {
            if (gb->breakpoints[i].condition) {
                free(gb->breakpoints[i].condition);
            }
        }
        free(gb->breakpoints);
        gb->breakpoints = NULL;
        gb->n_breakpoints = 0;
        return true;
    }

    bool error;
    value_t result = debugger_evaluate(gb, arguments, (unsigned)strlen(arguments), &error, NULL, NULL);
    uint32_t key = BP_KEY(result);

    if (error) return true;

    uint16_t index = 0;
    for (unsigned i = 0; i < gb->n_breakpoints; i++) {
        if (gb->breakpoints[i].key == key) {
            /* Full match */
            index = i;
            break;
        }
        if (gb->breakpoints[i].addr == result.value && result.has_bank != (gb->breakpoints[i].bank != (uint16_t) -1)) {
            /* Partial match */
            index = i;
        }
    }

    if (index >= gb->n_breakpoints) {
        GB_log(gb, "No breakpoint set at %s\n", debugger_value_to_string(gb, result, true));
        return true;
    }

    result.bank = gb->breakpoints[index].bank;
    result.has_bank = gb->breakpoints[index].bank != (uint16_t) -1;

    if (gb->breakpoints[index].condition) {
        free(gb->breakpoints[index].condition);
    }

    if (gb->breakpoints[index].is_jump_to) {
        gb->has_jump_to_breakpoints = false;
        for (unsigned i = 0; i < gb->n_breakpoints; i++) {
            if (i == index) continue;
            if (gb->breakpoints[i].is_jump_to) {
                gb->has_jump_to_breakpoints = true;
                break;
            }
        }
    }

    memmove(&gb->breakpoints[index], &gb->breakpoints[index + 1], (gb->n_breakpoints - index - 1) * sizeof(gb->breakpoints[0]));
    gb->n_breakpoints--;
    gb->breakpoints = realloc(gb->breakpoints, gb->n_breakpoints * sizeof(gb->breakpoints[0]));

    GB_log(gb, "Breakpoint removed from %s\n", debugger_value_to_string(gb, result, true));
    return true;
}

/* Find the index of the closest watchpoint equal or greater to addr */
static uint16_t find_watchpoint(GB_gameboy_t *gb, value_t addr)
{
    if (!gb->watchpoints) {
        return 0;
    }
    uint32_t key = WP_KEY(addr);
    unsigned min = 0;
    unsigned max = gb->n_watchpoints;
    while (min < max) {
        uint16_t pivot = (min + max) / 2;
        if (gb->watchpoints[pivot].key == key) return pivot;
        if (gb->watchpoints[pivot].key > key) {
            max = pivot;
        }
        else {
            min = pivot + 1;
        }
    }
    return (uint16_t) min;
}

static char *rw_completer(GB_gameboy_t *gb, const char *string, uintptr_t *context)
{
    size_t length = strlen(string);
    const char *suggestions[] = {"r", "rw", "w"};
    while (*context < sizeof(suggestions) / sizeof(suggestions[0])) {
        if (memcmp(string, suggestions[*context], length) == 0) {
            return strdup(suggestions[(*context)++] + length);
        }
        (*context)++;
    }
    return NULL;
}

static bool watch(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    if (strlen(lstrip(arguments)) == 0) {
print_usage:
        print_usage(gb, command);
        return true;
    }

    if (gb->n_watchpoints == (typeof(gb->n_watchpoints)) -1) {
        GB_log(gb, "Too many watchpoints set\n");
        return true;
    }

    if (!modifiers) {
        modifiers = "w";
    }

    uint8_t flags = 0;
    while (*modifiers) {
        switch (*modifiers) {
            case 'r':
                flags |= GB_WATCHPOINT_R;
                break;
            case 'w':
                flags |= GB_WATCHPOINT_W;
                break;
            default:
                goto print_usage;
        }
        modifiers++;
    }

    if (!flags) {
        goto print_usage;
    }

    char *condition = NULL;
    if ((condition = strstr(arguments, " if "))) {
        *condition = 0;
        condition += strlen(" if ");
        /* Verify condition is sane (Todo: This might have side effects!) */
        bool error;
        /* To make $new and $old legal */
        uint16_t dummy = 0;
        uint8_t dummy2 = 0;
        debugger_evaluate(gb, condition, (unsigned)strlen(condition), &error, &dummy, &dummy2);
        if (error) return true;

    }

    bool error;
    value_t result = debugger_evaluate(gb, arguments, (unsigned)strlen(arguments), &error, NULL, NULL);
    uint32_t key = WP_KEY(result);

    if (error) return true;

    uint16_t index = find_watchpoint(gb, result);
    if (index < gb->n_watchpoints && gb->watchpoints[index].key == key) {
        GB_log(gb, "Watchpoint already set at %s\n", debugger_value_to_string(gb, result, true));
        if (gb->watchpoints[index].flags != flags) {
            GB_log(gb, "Modified watchpoint type\n");
            gb->watchpoints[index].flags = flags;
        }
        if (!gb->watchpoints[index].condition && condition) {
            GB_log(gb, "Added condition to watchpoint\n");
            gb->watchpoints[index].condition = strdup(condition);
        }
        else if (gb->watchpoints[index].condition && condition) {
            GB_log(gb, "Replaced watchpoint condition\n");
            free(gb->watchpoints[index].condition);
            gb->watchpoints[index].condition = strdup(condition);
        }
        else if (gb->watchpoints[index].condition && !condition) {
            GB_log(gb, "Removed watchpoint condition\n");
            free(gb->watchpoints[index].condition);
            gb->watchpoints[index].condition = NULL;
        }
        return true;
    }

    gb->watchpoints = realloc(gb->watchpoints, (gb->n_watchpoints + 1) * sizeof(gb->watchpoints[0]));
    memmove(&gb->watchpoints[index + 1], &gb->watchpoints[index], (gb->n_watchpoints - index) * sizeof(gb->watchpoints[0]));
    gb->watchpoints[index].key = key;
    gb->watchpoints[index].flags = flags;
    if (condition) {
        gb->watchpoints[index].condition = strdup(condition);
    }
    else {
        gb->watchpoints[index].condition = NULL;
    }
    gb->n_watchpoints++;

    GB_log(gb, "Watchpoint set at %s\n", debugger_value_to_string(gb, result, true));
    return true;
}

static bool unwatch(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    NO_MODIFIERS
    if (strlen(lstrip(arguments)) == 0) {
        for (unsigned i = gb->n_watchpoints; i--;) {
            if (gb->watchpoints[i].condition) {
                free(gb->watchpoints[i].condition);
            }
        }
        free(gb->watchpoints);
        gb->watchpoints = NULL;
        gb->n_watchpoints = 0;
        return true;
    }

    bool error;
    value_t result = debugger_evaluate(gb, arguments, (unsigned)strlen(arguments), &error, NULL, NULL);
    uint32_t key = WP_KEY(result);

    if (error) return true;

    uint16_t index = 0;
    for (unsigned i = 0; i < gb->n_watchpoints; i++) {
        if (gb->watchpoints[i].key == key) {
            /* Full match */
            index = i;
            break;
        }
        if (gb->watchpoints[i].addr == result.value && result.has_bank != (gb->watchpoints[i].bank != (uint16_t) -1)) {
            /* Partial match */
            index = i;
        }
    }

    if (index >= gb->n_watchpoints) {
        GB_log(gb, "No watchpoint set at %s\n", debugger_value_to_string(gb, result, true));
        return true;
    }

    result.bank = gb->watchpoints[index].bank;
    result.has_bank = gb->watchpoints[index].bank != (uint16_t) -1;

    if (gb->watchpoints[index].condition) {
        free(gb->watchpoints[index].condition);
    }

    memmove(&gb->watchpoints[index], &gb->watchpoints[index + 1], (gb->n_watchpoints - index - 1) * sizeof(gb->watchpoints[0]));
    gb->n_watchpoints--;
    gb->watchpoints = realloc(gb->watchpoints, gb->n_watchpoints *sizeof(gb->watchpoints[0]));

    GB_log(gb, "Watchpoint removed from %s\n", debugger_value_to_string(gb, result, true));
    return true;
}

static bool list(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    NO_MODIFIERS
    if (strlen(lstrip(arguments))) {
        print_usage(gb, command);
        return true;
    }

    if (gb->n_breakpoints == 0) {
        GB_log(gb, "No breakpoints set.\n");
    }
    else {
        GB_log(gb, "%d breakpoint(s) set:\n", gb->n_breakpoints);
        for (uint16_t i = 0; i < gb->n_breakpoints; i++) {
            value_t addr = (value_t){gb->breakpoints[i].bank != (uint16_t)-1, gb->breakpoints[i].bank, gb->breakpoints[i].addr};
            if (gb->breakpoints[i].condition) {
                GB_log(gb, " %d. %s (%sCondition: %s)\n", i + 1,
                                                        debugger_value_to_string(gb, addr, addr.has_bank),
                                                        gb->breakpoints[i].is_jump_to? "Jump to, ": "",
                                                        gb->breakpoints[i].condition);
            }
            else {
                GB_log(gb, " %d. %s%s\n", i + 1,
                                          debugger_value_to_string(gb, addr, addr.has_bank),
                                          gb->breakpoints[i].is_jump_to? " (Jump to)" : "");
            }
        }
    }

    if (gb->n_watchpoints == 0) {
        GB_log(gb, "No watchpoints set.\n");
    }
    else {
        GB_log(gb, "%d watchpoint(s) set:\n", gb->n_watchpoints);
        for (uint16_t i = 0; i < gb->n_watchpoints; i++) {
            value_t addr = (value_t){gb->watchpoints[i].bank != (uint16_t)-1, gb->watchpoints[i].bank, gb->watchpoints[i].addr};
            if (gb->watchpoints[i].condition) {
                GB_log(gb, " %d. %s (%c%c, Condition: %s)\n", i + 1, debugger_value_to_string(gb, addr, addr.has_bank),
                                                              (gb->watchpoints[i].flags & GB_WATCHPOINT_R)? 'r' : '-',
                                                              (gb->watchpoints[i].flags & GB_WATCHPOINT_W)? 'w' : '-',
                                                              gb->watchpoints[i].condition);
            }
            else {
                GB_log(gb, " %d. %s (%c%c)\n", i + 1, debugger_value_to_string(gb, addr, addr.has_bank),
                                               (gb->watchpoints[i].flags & GB_WATCHPOINT_R)? 'r' : '-',
                                               (gb->watchpoints[i].flags & GB_WATCHPOINT_W)? 'w' : '-');
            }
        }
    }

    return true;
}

static bool _should_break(GB_gameboy_t *gb, value_t addr, bool jump_to)
{
    uint16_t index = find_breakpoint(gb, addr);
    uint32_t key = BP_KEY(addr);

    if (index < gb->n_breakpoints && gb->breakpoints[index].key == key && gb->breakpoints[index].is_jump_to == jump_to) {
        if (!gb->breakpoints[index].condition) {
            return true;
        }
        bool error;
        bool condition = debugger_evaluate(gb, gb->breakpoints[index].condition,
                                           (unsigned)strlen(gb->breakpoints[index].condition), &error, NULL, NULL).value;
        if (error) {
            /* Should never happen */
            GB_log(gb, "An internal error has occured\n");
            return true;
        }
        return condition;
    }
    return false;
}

static bool should_break(GB_gameboy_t *gb, uint16_t addr, bool jump_to)
{
    /* Try any-bank breakpoint */
    value_t full_addr = (VALUE_16(addr));
    if (_should_break(gb, full_addr, jump_to)) return true;

    /* Try bank-specific breakpoint */
    full_addr.has_bank = true;
    full_addr.bank = bank_for_addr(gb, addr);
    return _should_break(gb, full_addr, jump_to);
}

static char *format_completer(GB_gameboy_t *gb, const char *string, uintptr_t *context)
{
    size_t length = strlen(string);
    const char *suggestions[] = {"a", "b", "d", "o", "x"};
    while (*context < sizeof(suggestions) / sizeof(suggestions[0])) {
        if (memcmp(string, suggestions[*context], length) == 0) {
            return strdup(suggestions[(*context)++] + length);
        }
        (*context)++;
    }
    return NULL;
}

static bool print(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    if (strlen(lstrip(arguments)) == 0) {
        print_usage(gb, command);
        return true;
    }

    if (!modifiers || !modifiers[0]) {
        modifiers = "a";
    }
    else if (modifiers[1]) {
        print_usage(gb, command);
        return true;
    }

    bool error;
    value_t result = debugger_evaluate(gb, arguments, (unsigned)strlen(arguments), &error, NULL, NULL);
    if (!error) {
        switch (modifiers[0]) {
            case 'a':
                GB_log(gb, "=%s\n", debugger_value_to_string(gb, result, false));
                break;
            case 'd':
                GB_log(gb, "=%d\n", result.value);
                break;
            case 'x':
                GB_log(gb, "=$%x\n", result.value);
                break;
            case 'o':
                GB_log(gb, "=0%o\n", result.value);
                break;
            case 'b':
            {
                if (!result.value) {
                    GB_log(gb, "=%%0\n");
                    break;
                }
                char binary[17];
                binary[16] = 0;
                char *ptr = &binary[16];
                while (result.value) {
                    *(--ptr) = (result.value & 1)? '1' : '0';
                    result.value >>= 1;
                }
                GB_log(gb, "=%%%s\n", ptr);
                break;
            }
            default:
                break;
        }
    }
    return true;
}

static bool examine(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    if (strlen(lstrip(arguments)) == 0) {
        print_usage(gb, command);
        return true;
    }

    bool error;
    value_t addr = debugger_evaluate(gb, arguments, (unsigned)strlen(arguments), &error, NULL, NULL);
    uint16_t count = 32;

    if (modifiers) {
        char *end;
        count = (uint16_t) (strtol(modifiers, &end, 10));
        if (*end) {
            print_usage(gb, command);
            return true;
        }
    }

    if (!error) {
        if (addr.has_bank) {
            banking_state_t old_state;
            save_banking_state(gb, &old_state);
            switch_banking_state(gb, addr.bank);

            while (count) {
                GB_log(gb, "%02x:%04x: ", addr.bank, addr.value);
                for (unsigned i = 0; i < 16 && count; i++) {
                    GB_log(gb, "%02x ", GB_read_memory(gb, addr.value + i));
                    count--;
                }
                addr.value += 16;
                GB_log(gb, "\n");
            }

            restore_banking_state(gb, &old_state);
        }
        else {
            while (count) {
                GB_log(gb, "%04x: ", addr.value);
                for (unsigned i = 0; i < 16 && count; i++) {
                    GB_log(gb, "%02x ", GB_read_memory(gb, addr.value + i));
                    count--;
                }
                addr.value += 16;
                GB_log(gb, "\n");
            }
        }
    }
    return true;
}

static bool disassemble(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    if (strlen(lstrip(arguments)) == 0) {
        arguments = "pc";
    }

    bool error;
    value_t addr = debugger_evaluate(gb, arguments, (unsigned)strlen(arguments), &error, NULL, NULL);
    uint16_t count = 5;

    if (modifiers) {
        char *end;
        count = (uint16_t) (strtol(modifiers, &end, 10));
        if (*end) {
            print_usage(gb, command);
            return true;
        }
    }

    if (!error) {
        if (addr.has_bank) {
            banking_state_t old_state;
            save_banking_state(gb, &old_state);
            switch_banking_state(gb, addr.bank);

            GB_cpu_disassemble(gb, addr.value, count);

            restore_banking_state(gb, &old_state);
        }
        else {
            GB_cpu_disassemble(gb, addr.value, count);
        }
    }
    return true;
}

static bool mbc(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    NO_MODIFIERS

    if (strlen(lstrip(arguments))) {
        print_usage(gb, command);
        return true;
    }

    const GB_cartridge_t *cartridge = gb->cartridge_type;

    if (cartridge->has_ram) {
        GB_log(gb, "Cartridge includes%s RAM: $%x bytes\n", cartridge->has_battery? " battery-backed": "", gb->mbc_ram_size);
    }
    else {
        GB_log(gb, "No cartridge RAM\n");
    }

    if (cartridge->mbc_type) {
        if (gb->is_mbc30) {
            GB_log(gb, "MBC30\n");
        }
        else {
            static const char *const mapper_names[] = {
                [GB_MBC1] = "MBC1",
                [GB_MBC2] = "MBC2",
                [GB_MBC3] = "MBC3",
                [GB_MBC5] = "MBC5",
                [GB_HUC1] = "HUC-1",
                [GB_HUC3] = "HUC-3",
            };
            GB_log(gb, "%s\n", mapper_names[cartridge->mbc_type]);
        }
        GB_log(gb, "Current mapped ROM bank: %x\n", gb->mbc_rom_bank);
        if (cartridge->has_ram) {
            GB_log(gb, "Current mapped RAM bank: %x\n", gb->mbc_ram_bank);
            if (gb->cartridge_type->mbc_type != GB_HUC1) {
                GB_log(gb, "RAM is curently %s\n", gb->mbc_ram_enable? "enabled" : "disabled");
            }
        }
        if (cartridge->mbc_type == GB_MBC1 && gb->mbc1_wiring == GB_STANDARD_MBC1_WIRING) {
            GB_log(gb, "MBC1 banking mode is %s\n", gb->mbc1.mode == 1 ? "RAM" : "ROM");
        }
        if (cartridge->mbc_type == GB_MBC1 && gb->mbc1_wiring == GB_MBC1M_WIRING) {
            GB_log(gb, "MBC1 uses MBC1M wiring. \n");
            GB_log(gb, "Current mapped ROM0 bank: %x\n", gb->mbc_rom0_bank);
            GB_log(gb, "MBC1 multicart banking mode is %s\n", gb->mbc1.mode == 1 ? "enabled" : "disabled");
        }

    }
    else {
        GB_log(gb, "No MBC\n");
    }

    if (cartridge->has_rumble) {
        GB_log(gb, "Cart contains a Rumble Pak\n");
    }

    if (cartridge->has_rtc) {
        GB_log(gb, "Cart contains a real time clock\n");
    }

    return true;
}

static bool backtrace(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    NO_MODIFIERS

    if (strlen(lstrip(arguments))) {
        print_usage(gb, command);
        return true;
    }

    GB_log(gb, "  1. %s\n", debugger_value_to_string(gb, (value_t){true, bank_for_addr(gb, gb->pc), gb->pc}, true));
    for (unsigned i = gb->backtrace_size; i--;) {
        GB_log(gb, "%3d. %s\n", gb->backtrace_size - i + 1, debugger_value_to_string(gb, (value_t){true, gb->backtrace_returns[i].bank, gb->backtrace_returns[i].addr}, true));
    }

    return true;
}

static bool ticks(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    NO_MODIFIERS
    STOPPED_ONLY

    if (strlen(lstrip(arguments))) {
        print_usage(gb, command);
        return true;
    }

    GB_log(gb, "Ticks: %llu. (Resetting)\n", (unsigned long long)gb->debugger_ticks);
    gb->debugger_ticks = 0;

    return true;
}


static bool palettes(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    NO_MODIFIERS
    if (strlen(lstrip(arguments))) {
        print_usage(gb, command);
        return true;
    }

    if (!GB_is_cgb(gb)) {
        GB_log(gb, "Not available on a DMG.\n");
        return true;
    }

    GB_log(gb, "Background palettes: \n");
    for (unsigned i = 0; i < 32; i++) {
        GB_log(gb, "%04x ", ((uint16_t *)&gb->background_palettes_data)[i]);
        if (i % 4 == 3) {
            GB_log(gb, "\n");
        }
    }

    GB_log(gb, "Sprites palettes: \n");
    for (unsigned i = 0; i < 32; i++) {
        GB_log(gb, "%04x ", ((uint16_t *)&gb->sprite_palettes_data)[i]);
        if (i % 4 == 3) {
            GB_log(gb, "\n");
        }
    }

    return true;
}

static bool lcd(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    NO_MODIFIERS
    if (strlen(lstrip(arguments))) {
        print_usage(gb, command);
        return true;
    }
    GB_log(gb, "LCDC:\n");
    GB_log(gb, "    LCD enabled: %s\n",(gb->io_registers[GB_IO_LCDC] & 128)? "Enabled" : "Disabled");
    GB_log(gb, "    %s: %s\n", (gb->cgb_mode? "Sprite priority flags" : "Background and Window"),
                               (gb->io_registers[GB_IO_LCDC] & 1)? "Enabled" : "Disabled");
    GB_log(gb, "    Objects: %s\n", (gb->io_registers[GB_IO_LCDC] & 2)? "Enabled" : "Disabled");
    GB_log(gb, "    Object size: %s\n", (gb->io_registers[GB_IO_LCDC] & 4)? "8x16" : "8x8");
    GB_log(gb, "    Background tilemap: %s\n", (gb->io_registers[GB_IO_LCDC] & 8)? "$9C00" : "$9800");
    GB_log(gb, "    Background and Window Tileset: %s\n", (gb->io_registers[GB_IO_LCDC] & 16)? "$8000" : "$8800");
    GB_log(gb, "    Window: %s\n", (gb->io_registers[GB_IO_LCDC] & 32)? "Enabled" : "Disabled");
    GB_log(gb, "    Window tilemap: %s\n", (gb->io_registers[GB_IO_LCDC] & 64)? "$9C00" : "$9800");

    GB_log(gb, "\nSTAT:\n");
    static const char *modes[] = {"Mode 0, H-Blank", "Mode 1, V-Blank", "Mode 2, OAM", "Mode 3, Rendering"};
    GB_log(gb, "    Current mode: %s\n", modes[gb->io_registers[GB_IO_STAT] & 3]);
    GB_log(gb, "    LYC flag: %s\n", (gb->io_registers[GB_IO_STAT] & 4)? "On" : "Off");
    GB_log(gb, "    H-Blank interrupt: %s\n", (gb->io_registers[GB_IO_STAT] & 8)? "Enabled" : "Disabled");
    GB_log(gb, "    V-Blank interrupt: %s\n", (gb->io_registers[GB_IO_STAT] & 16)? "Enabled" : "Disabled");
    GB_log(gb, "    OAM interrupt: %s\n", (gb->io_registers[GB_IO_STAT] & 32)? "Enabled" : "Disabled");
    GB_log(gb, "    LYC interrupt: %s\n", (gb->io_registers[GB_IO_STAT] & 64)? "Enabled" : "Disabled");



    GB_log(gb, "\nCurrent line: %d\n", gb->current_line);
    GB_log(gb, "Current state: ");
    if (!(gb->io_registers[GB_IO_LCDC] & 0x80)) {
        GB_log(gb, "Off\n");
    }
    else if (gb->display_state == 7 || gb->display_state == 8) {
        GB_log(gb, "Reading OAM data (%d/40)\n", gb->display_state == 8? gb->oam_search_index : 0);
    }
    else if (gb->display_state <= 3 || gb->display_state == 24 || gb->display_state == 31) {
        GB_log(gb, "Glitched line 0 OAM mode (%d cycles to next event)\n", -gb->display_cycles / 2);
    }
    else if (gb->mode_for_interrupt == 3) {
        signed pixel = gb->position_in_line > 160? (int8_t) gb->position_in_line : gb->position_in_line;
        GB_log(gb, "Rendering pixel (%d/160)\n", pixel);
    }
    else {
        GB_log(gb, "Sleeping (%d cycles to next event)\n", -gb->display_cycles / 2);
    }
    GB_log(gb, "LY: %d\n", gb->io_registers[GB_IO_LY]);
    GB_log(gb, "LYC: %d\n", gb->io_registers[GB_IO_LYC]);
    GB_log(gb, "Window position: %d, %d\n", (signed) gb->io_registers[GB_IO_WX] - 7, gb->io_registers[GB_IO_WY]);
    GB_log(gb, "Interrupt line: %s\n", gb->stat_interrupt_line? "On" : "Off");

    return true;
}

static bool apu(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    NO_MODIFIERS
    if (strlen(lstrip(arguments))) {
        print_usage(gb, command);
        return true;
    }


    GB_log(gb, "Current state: ");
    if (!gb->apu.global_enable) {
        GB_log(gb, "Disabled\n");
    }
    else {
        GB_log(gb, "Enabled\n");
        for (uint8_t channel = 0; channel < GB_N_CHANNELS; channel++) {
            GB_log(gb, "CH%u is %s, DAC %s; current sample = 0x%x\n", channel + 1,
                gb->apu.is_active[channel] ? "active  " : "inactive",
                GB_apu_is_DAC_enabled(gb, channel) ? "active  " : "inactive",
                gb->apu.samples[channel]);
        }
    }

    GB_log(gb, "SO1 (left output):  volume %u,", gb->io_registers[GB_IO_NR50] & 0x07);
    if (gb->io_registers[GB_IO_NR51] & 0x0f) {
        for (uint8_t channel = 0, mask = 0x01; channel < GB_N_CHANNELS; channel++, mask <<= 1) {
            if (gb->io_registers[GB_IO_NR51] & mask) {
                GB_log(gb, " CH%u", channel + 1);
            }
        }
    }
    else {
        GB_log(gb, " no channels");
    }
    GB_log(gb, "%s\n", gb->io_registers[GB_IO_NR50] & 0x80 ? " VIN": "");

    GB_log(gb, "SO2 (right output): volume %u,", gb->io_registers[GB_IO_NR50] & 0x70 >> 4);
    if (gb->io_registers[GB_IO_NR51] & 0xf0) {
        for (uint8_t channel = 0, mask = 0x10; channel < GB_N_CHANNELS; channel++, mask <<= 1) {
            if (gb->io_registers[GB_IO_NR51] & mask) {
                GB_log(gb, " CH%u", channel + 1);
            }
        }
    }
    else {
        GB_log(gb, " no channels");
    }
    GB_log(gb, "%s\n", gb->io_registers[GB_IO_NR50] & 0x80 ? " VIN": "");


    for (uint8_t channel = GB_SQUARE_1; channel <= GB_SQUARE_2; channel++) {
        GB_log(gb, "\nCH%u:\n", channel + 1);
        GB_log(gb, "    Current volume: %u, current sample length: %u APU ticks (next in %u ticks)\n",
             gb->apu.square_channels[channel].current_volume,
            (gb->apu.square_channels[channel].sample_length ^ 0x7FF) * 2 + 1,
             gb->apu.square_channels[channel].sample_countdown);

        uint8_t nrx2 = gb->io_registers[channel == GB_SQUARE_1? GB_IO_NR12 : GB_IO_NR22];
        GB_log(gb, "    %u 256 Hz ticks till next volume %screase (out of %u)\n",
            gb->apu.square_channels[channel].volume_countdown,
            nrx2 & 8 ? "in" : "de",
            nrx2 & 7);

        uint8_t duty = gb->io_registers[channel == GB_SQUARE_1? GB_IO_NR11 :GB_IO_NR21] >> 6;
        GB_log(gb, "    Duty cycle %s%% (%s), current index %u/8%s\n",
               duty > 3? "" : (const char *[]){"12.5", "  25", "  50", "  75"}[duty],
               duty > 3? "" : (const char *[]){"_______-", "-______-", "-____---", "_------_"}[duty],
               gb->apu.square_channels[channel].current_sample_index & 0x7f,
               gb->apu.square_channels[channel].current_sample_index >> 7 ? " (suppressed)" : "");

        if (channel == GB_SQUARE_1) {
            GB_log(gb, "    Frequency sweep %s and %s (next in %u APU ticks)\n",
                   gb->apu.sweep_enabled? "active" : "inactive",
                   gb->apu.sweep_decreasing? "decreasing" : "increasing",
                   gb->apu.square_sweep_calculate_countdown);
        }

        if (gb->apu.square_channels[channel].length_enabled) {
            GB_log(gb, "    Channel will end in %u 256 Hz ticks\n",
                gb->apu.square_channels[channel].pulse_length);
        }
    }


    GB_log(gb, "\nCH3:\n");
    GB_log(gb, "    Wave:");
    for (uint8_t i = 0; i < 32; i++) {
        GB_log(gb, "%s%X", i%4?"":" ", gb->apu.wave_channel.wave_form[i]);
    }
    GB_log(gb, "\n");
    GB_log(gb, "    Current position: %u\n", gb->apu.wave_channel.current_sample_index);

    GB_log(gb, "    Volume %s (right-shifted %u times)\n",
           gb->apu.wave_channel.shift > 4? "" : (const char *[]){"100%", "50%", "25%", "", "muted"}[gb->apu.wave_channel.shift],
           gb->apu.wave_channel.shift);

    GB_log(gb, "    Current sample length: %u APU ticks (next in %u ticks)\n",
        gb->apu.wave_channel.sample_length ^ 0x7ff,
        gb->apu.wave_channel.sample_countdown);

    if (gb->apu.wave_channel.length_enabled) {
        GB_log(gb, "    Channel will end in %u 256 Hz ticks\n",
            gb->apu.wave_channel.pulse_length);
    }


    GB_log(gb, "\nCH4:\n");
    GB_log(gb, "    Current volume: %u, current sample length: %u APU ticks (next in %u ticks)\n",
        gb->apu.noise_channel.current_volume,
        gb->apu.noise_channel.sample_length * 4 + 3,
        gb->apu.noise_channel.sample_countdown);

    GB_log(gb, "    %u 256 Hz ticks till next volume %screase (out of %u)\n",
        gb->apu.noise_channel.volume_countdown,
        gb->io_registers[GB_IO_NR42] & 8 ? "in" : "de",
        gb->io_registers[GB_IO_NR42] & 7);

    GB_log(gb, "    LFSR in %u-step mode, current value ",
        gb->apu.noise_channel.narrow? 7 : 15);
    for (uint16_t lfsr = gb->apu.noise_channel.lfsr, i = 15; i--; lfsr <<= 1) {
        GB_log(gb, "%u%s", (lfsr >> 14) & 1, i%4 ? "" : " ");
    }

    if (gb->apu.noise_channel.length_enabled) {
        GB_log(gb, "    Channel will end in %u 256 Hz ticks\n",
            gb->apu.noise_channel.pulse_length);
    }


    GB_log(gb, "\n\nReminder: APU ticks are @ 2 MiHz\n");

    return true;
}

static char *wave_completer(GB_gameboy_t *gb, const char *string, uintptr_t *context)
{
    size_t length = strlen(string);
    const char *suggestions[] = {"c", "f", "l"};
    while (*context < sizeof(suggestions) / sizeof(suggestions[0])) {
        if (memcmp(string, suggestions[*context], length) == 0) {
            return strdup(suggestions[(*context)++] + length);
        }
        (*context)++;
    }
    return NULL;
}

static bool wave(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command)
{
    if (strlen(lstrip(arguments)) || (modifiers && !strchr("fcl", modifiers[0]))) {
        print_usage(gb, command);
        return true;
    }

    uint8_t shift_amount = 1, mask;
    if (modifiers) {
        switch (modifiers[0]) {
            case 'c':
                shift_amount = 2;
                break;
            case 'l':
                shift_amount = 8;
                break;
        }
    }
    mask = (0xf << (shift_amount - 1)) & 0xf;

    for (int8_t cur_val = 0xf & mask; cur_val >= 0; cur_val -= shift_amount) {
        for (uint8_t i = 0; i < 32; i++) {
            if ((gb->apu.wave_channel.wave_form[i] & mask) == cur_val) {
                GB_log(gb, "%X", gb->apu.wave_channel.wave_form[i]);
            }
            else {
                GB_log(gb, "%c", i%4 == 2 ? '-' : ' ');
            }
        }
        GB_log(gb, "\n");
    }

    return true;
}

static bool help(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *command);

#define HELP_NEWLINE "\n             "

/* Commands without implementations are aliases of the previous non-alias commands */
static const debugger_command_t commands[] = {
    {"continue", 1, cont, "Continue running until next stop"},
    {"next", 1, next, "Run the next instruction, skipping over function calls"},
    {"step", 1, step, "Run the next instruction, stepping into function calls"},
    {"finish", 1, finish, "Run until the current function returns"},
    {"backtrace", 2, backtrace, "Displays the current call stack"},
    {"bt", 2, }, /* Alias */
    {"sld", 3, stack_leak_detection, "Like finish, but stops if a stack leak is detected"},
    {"ticks", 2, ticks, "Displays the number of CPU ticks since the last time 'ticks' was" HELP_NEWLINE
                        "used"},
    {"registers", 1, registers, "Print values of processor registers and other important registers"},
    {"cartridge", 2, mbc, "Displays information about the MBC and cartridge"},
    {"mbc", 3, }, /* Alias */
    {"apu", 3, apu, "Displays information about the current state of the audio chip"},
    {"wave", 3, wave, "Prints a visual representation of the wave RAM." HELP_NEWLINE
                      "Modifiers can be used for a (f)ull print (the default)," HELP_NEWLINE
        "a more (c)ompact one, or a one-(l)iner", "", "(f|c|l)", .modifiers_completer = wave_completer},
    {"lcd", 3, lcd, "Displays information about the current state of the LCD controller"},
    {"palettes", 3, palettes, "Displays the current CGB palettes"},
    {"softbreak", 2, softbreak, "Enables or disables software breakpoints", "(on|off)", .argument_completer = on_off_completer},
    {"breakpoint", 1, breakpoint, "Add a new breakpoint at the specified address/expression" HELP_NEWLINE
                                  "Can also modify the condition of existing breakpoints." HELP_NEWLINE
                                  "If the j modifier is used, the breakpoint will occur just before" HELP_NEWLINE
                                  "jumping to the target.",
                                  "<expression>[ if <condition expression>]", "j",
                                  .argument_completer = symbol_completer, .modifiers_completer = j_completer},
    {"delete", 2, delete, "Delete a breakpoint by its address, or all breakpoints", "[<expression>]", .argument_completer = symbol_completer},
    {"watch", 1, watch, "Add a new watchpoint at the specified address/expression." HELP_NEWLINE
                        "Can also modify the condition and type of existing watchpoints." HELP_NEWLINE
                        "Default watchpoint type is write-only.",
                        "<expression>[ if <condition expression>]", "(r|w|rw)",
                        .argument_completer = symbol_completer, .modifiers_completer = rw_completer
    },
    {"unwatch", 3, unwatch, "Delete a watchpoint by its address, or all watchpoints", "[<expression>]", .argument_completer = symbol_completer},
    {"list", 1, list, "List all set breakpoints and watchpoints"},
    {"print", 1, print, "Evaluate and print an expression" HELP_NEWLINE
                        "Use modifier to format as an address (a, default) or as a number in" HELP_NEWLINE
                        "decimal (d), hexadecimal (x), octal (o) or binary (b).",
                        "<expression>", "format", .argument_completer = symbol_completer, .modifiers_completer = format_completer},
    {"eval", 2, }, /* Alias */
    {"examine", 2, examine, "Examine values at address", "<expression>", "count", .argument_completer = symbol_completer},
    {"x", 1, }, /* Alias */
    {"disassemble", 1, disassemble, "Disassemble instructions at address", "<expression>", "count", .argument_completer = symbol_completer},


    {"help", 1, help, "List available commands or show help for the specified command", "[<command>]"},
    {NULL,}, /* Null terminator */
};

static const debugger_command_t *find_command(const char *string)
{
    size_t length = strlen(string);
    for (const debugger_command_t *command = commands; command->command; command++) {
        if (command->min_length > length) continue;
        if (memcmp(command->command, string, length) == 0) { /* Is a substring? */
            /* Aliases */
            while (!command->implementation) {
                command--;
            }
            return command;
        }
    }

    return NULL;
}

static void print_command_shortcut(GB_gameboy_t *gb, const debugger_command_t *command)
{
    GB_attributed_log(gb, GB_LOG_BOLD | GB_LOG_UNDERLINE, "%.*s", command->min_length, command->command);
    GB_attributed_log(gb, GB_LOG_BOLD, "%s", command->command + command->min_length);
}

static void print_command_description(GB_gameboy_t *gb, const debugger_command_t *command)
{
    print_command_shortcut(gb, command);
    GB_log(gb, ": ");
    GB_log(gb, (const char *)&"           %s\n" + strlen(command->command), command->help_string);
}

static bool help(GB_gameboy_t *gb, char *arguments, char *modifiers, const debugger_command_t *ignored)
{
    const debugger_command_t *command = find_command(arguments);
    if (command) {
        print_command_description(gb, command);
        GB_log(gb, "\n");
        print_usage(gb, command);

        command++;
        if (command->command && !command->implementation) { /* Command has aliases*/
            GB_log(gb, "\nAliases: ");
            do {
                print_command_shortcut(gb, command);
                GB_log(gb, " ");
                command++;
            } while (command->command && !command->implementation);
            GB_log(gb, "\n");
        }
        return true;
    }
    for (command = commands; command->command; command++) {
        if (command->help_string) {
            print_command_description(gb, command);
        }
    }
    return true;
}

void GB_debugger_call_hook(GB_gameboy_t *gb, uint16_t call_addr)
{
    /* Called just after the CPU calls a function/enters an interrupt/etc... */

    if (gb->stack_leak_detection) {
        if (gb->debug_call_depth >= sizeof(gb->sp_for_call_depth) / sizeof(gb->sp_for_call_depth[0])) {
            GB_log(gb, "Potential stack overflow detected (Functions nest too much). \n");
            gb->debug_stopped = true;
        }
        else {
            gb->sp_for_call_depth[gb->debug_call_depth] = gb->registers[GB_REGISTER_SP];
            gb->addr_for_call_depth[gb->debug_call_depth] = gb->pc;
        }
    }

    if (gb->backtrace_size < sizeof(gb->backtrace_sps) / sizeof(gb->backtrace_sps[0])) {

        while (gb->backtrace_size) {
            if (gb->backtrace_sps[gb->backtrace_size - 1] < gb->registers[GB_REGISTER_SP]) {
                gb->backtrace_size--;
            }
            else {
                break;
            }
        }

        gb->backtrace_sps[gb->backtrace_size] = gb->registers[GB_REGISTER_SP];
        gb->backtrace_returns[gb->backtrace_size].bank = bank_for_addr(gb, call_addr);
        gb->backtrace_returns[gb->backtrace_size].addr = call_addr;
        gb->backtrace_size++;
    }

    gb->debug_call_depth++;
}

void GB_debugger_ret_hook(GB_gameboy_t *gb)
{
    /* Called just before the CPU runs ret/reti */

    gb->debug_call_depth--;

    if (gb->stack_leak_detection) {
        if (gb->debug_call_depth < 0) {
            GB_log(gb, "Function finished without a stack leak.\n");
            gb->debug_stopped = true;
        }
        else {
            if (gb->registers[GB_REGISTER_SP] != gb->sp_for_call_depth[gb->debug_call_depth]) {
                GB_log(gb, "Stack leak detected for function %s!\n", value_to_string(gb, gb->addr_for_call_depth[gb->debug_call_depth], true));
                GB_log(gb, "SP is $%04x, should be $%04x.\n", gb->registers[GB_REGISTER_SP],
                                                            gb->sp_for_call_depth[gb->debug_call_depth]);
                gb->debug_stopped = true;
            }
        }
    }

    while (gb->backtrace_size) {
        if (gb->backtrace_sps[gb->backtrace_size - 1] <= gb->registers[GB_REGISTER_SP]) {
            gb->backtrace_size--;
        }
        else {
            break;
        }
    }
}

static bool _GB_debugger_test_write_watchpoint(GB_gameboy_t *gb, value_t addr, uint8_t value)
{
    uint16_t index = find_watchpoint(gb, addr);
    uint32_t key = WP_KEY(addr);

    if (index < gb->n_watchpoints && gb->watchpoints[index].key == key) {
        if (!(gb->watchpoints[index].flags & GB_WATCHPOINT_W)) {
            return false;
        }
        if (!gb->watchpoints[index].condition) {
            gb->debug_stopped = true;
            GB_log(gb, "Watchpoint: [%s] = $%02x\n", debugger_value_to_string(gb, addr, true), value);
            return true;
        }
        bool error;
        bool condition = debugger_evaluate(gb, gb->watchpoints[index].condition,
                                           (unsigned)strlen(gb->watchpoints[index].condition), &error, &addr.value, &value).value;
        if (error) {
            /* Should never happen */
            GB_log(gb, "An internal error has occured\n");
            return false;
        }
        if (condition) {
            gb->debug_stopped = true;
            GB_log(gb, "Watchpoint: [%s] = $%02x\n", debugger_value_to_string(gb, addr, true), value);
            return true;
        }
    }
    return false;
}

void GB_debugger_test_write_watchpoint(GB_gameboy_t *gb, uint16_t addr, uint8_t value)
{
    if (gb->debug_stopped) return;

    /* Try any-bank breakpoint */
    value_t full_addr = (VALUE_16(addr));
    if (_GB_debugger_test_write_watchpoint(gb, full_addr, value)) return;

    /* Try bank-specific breakpoint */
    full_addr.has_bank = true;
    full_addr.bank = bank_for_addr(gb, addr);
    _GB_debugger_test_write_watchpoint(gb, full_addr, value);
}

static bool _GB_debugger_test_read_watchpoint(GB_gameboy_t *gb, value_t addr)
{
    uint16_t index = find_watchpoint(gb, addr);
    uint32_t key = WP_KEY(addr);

    if (index < gb->n_watchpoints && gb->watchpoints[index].key == key) {
        if (!(gb->watchpoints[index].flags & GB_WATCHPOINT_R)) {
            return false;
        }
        if (!gb->watchpoints[index].condition) {
            gb->debug_stopped = true;
            GB_log(gb, "Watchpoint: [%s]\n", debugger_value_to_string(gb, addr, true));
            return true;
        }
        bool error;
        bool condition = debugger_evaluate(gb, gb->watchpoints[index].condition,
                                           (unsigned)strlen(gb->watchpoints[index].condition), &error, &addr.value, NULL).value;
        if (error) {
            /* Should never happen */
            GB_log(gb, "An internal error has occured\n");
            return false;
        }
        if (condition) {
            gb->debug_stopped = true;
            GB_log(gb, "Watchpoint: [%s]\n", debugger_value_to_string(gb, addr, true));
            return true;
        }
    }
    return false;
}

void GB_debugger_test_read_watchpoint(GB_gameboy_t *gb, uint16_t addr)
{
    if (gb->debug_stopped) return;

    /* Try any-bank breakpoint */
    value_t full_addr = (VALUE_16(addr));
    if (_GB_debugger_test_read_watchpoint(gb, full_addr)) return;

    /* Try bank-specific breakpoint */
    full_addr.has_bank = true;
    full_addr.bank = bank_for_addr(gb, addr);
    _GB_debugger_test_read_watchpoint(gb, full_addr);
}

/* Returns true if debugger waits for more commands */
bool GB_debugger_execute_command(GB_gameboy_t *gb, char *input)
{
    if (!input[0]) {
        return true;
    }

    char *command_string = input;
    char *arguments = strchr(input, ' ');
    if (arguments) {
        /* Actually "split" the string. */
        arguments[0] = 0;
        arguments++;
    }
    else {
        arguments = "";
    }

    char *modifiers = strchr(command_string, '/');
    if (modifiers) {
        /* Actually "split" the string. */
        modifiers[0] = 0;
        modifiers++;
    }

    const debugger_command_t *command = find_command(command_string);
    if (command) {
        return command->implementation(gb, arguments, modifiers, command);
    }
    else {
        GB_log(gb, "%s: no such command.\n", command_string);
        return true;
    }
}

/* Returns true if debugger waits for more commands */
char *GB_debugger_complete_substring(GB_gameboy_t *gb, char *input, uintptr_t *context)
{   
    char *command_string = input;
    char *arguments = strchr(input, ' ');
    if (arguments) {
        /* Actually "split" the string. */
        arguments[0] = 0;
        arguments++;
    }
    
    char *modifiers = strchr(command_string, '/');
    if (modifiers) {
        /* Actually "split" the string. */
        modifiers[0] = 0;
        modifiers++;
    }
    
    const debugger_command_t *command = find_command(command_string);
    if (command && command->implementation == help && arguments) {
        command_string = arguments;
        arguments = NULL;
    }
    
    /* No commands and no modifiers, complete the command */
    if (!arguments && !modifiers) {
        size_t length = strlen(command_string);
        if (*context >= sizeof(commands) / sizeof(commands[0])) {
            return NULL;
        }
        for (const debugger_command_t *command = &commands[*context]; command->command; command++) {
            (*context)++;
            if (memcmp(command->command, command_string, length) == 0) { /* Is a substring? */
                return strdup(command->command + length);
            }
        }
        return NULL;
    }
    
    if (command) {
        if (arguments) {
            if (command->argument_completer) {
                return command->argument_completer(gb, arguments, context);
            }
            return NULL;
        }
        
        if (modifiers) {
            if (command->modifiers_completer) {
                return command->modifiers_completer(gb, modifiers, context);
            }
            return NULL;
        }
    }
    return NULL;
}

typedef enum {
    JUMP_TO_NONE,
    JUMP_TO_BREAK,
    JUMP_TO_NONTRIVIAL,
} jump_to_return_t;

static jump_to_return_t test_jump_to_breakpoints(GB_gameboy_t *gb, uint16_t *address);

void GB_debugger_run(GB_gameboy_t *gb)
{
    if (gb->debug_disable) return;

    char *input = NULL;
    if (gb->debug_next_command && gb->debug_call_depth <= 0 && !gb->halted) {
        gb->debug_stopped = true;
    }
    if (gb->debug_fin_command && gb->debug_call_depth == -1) {
        gb->debug_stopped = true;
    }
    if (gb->debug_stopped) {
        GB_cpu_disassemble(gb, gb->pc, 5);
    }
next_command:
    if (input) {
        free(input);
    }
    if (gb->breakpoints && !gb->debug_stopped && should_break(gb, gb->pc, false)) {
        gb->debug_stopped = true;
        GB_log(gb, "Breakpoint: PC = %s\n", value_to_string(gb, gb->pc, true));
        GB_cpu_disassemble(gb, gb->pc, 5);
    }

    if (gb->breakpoints && !gb->debug_stopped) {
        uint16_t address = 0;
        jump_to_return_t jump_to_result = test_jump_to_breakpoints(gb, &address);

        bool should_delete_state = true;
        if (gb->nontrivial_jump_state && should_break(gb, gb->pc, true)) {
            if (gb->non_trivial_jump_breakpoint_occured) {
                gb->non_trivial_jump_breakpoint_occured = false;
            }
            else {
                gb->non_trivial_jump_breakpoint_occured = true;
                GB_log(gb, "Jumping to breakpoint: PC = %s\n", value_to_string(gb, gb->pc, true));
                GB_cpu_disassemble(gb, gb->pc, 5);
                GB_load_state_from_buffer(gb, gb->nontrivial_jump_state, -1);
                gb->debug_stopped = true;
            }
        }
        else if (jump_to_result == JUMP_TO_BREAK) {
            gb->debug_stopped = true;
            GB_log(gb, "Jumping to breakpoint: PC = %s\n", value_to_string(gb, address, true));
            GB_cpu_disassemble(gb, gb->pc, 5);
            gb->non_trivial_jump_breakpoint_occured = false;
        }
        else if (jump_to_result == JUMP_TO_NONTRIVIAL) {
            if (!gb->nontrivial_jump_state) {
                gb->nontrivial_jump_state = malloc(GB_get_save_state_size(gb));
            }
            GB_save_state_to_buffer(gb, gb->nontrivial_jump_state);
            gb->non_trivial_jump_breakpoint_occured = false;
            should_delete_state = false;
        }
        else {
            gb->non_trivial_jump_breakpoint_occured = false;
        }

        if (should_delete_state) {
            if (gb->nontrivial_jump_state) {
                free(gb->nontrivial_jump_state);
                gb->nontrivial_jump_state = NULL;
            }
        }
    }

    if (gb->debug_stopped && !gb->debug_disable) {
        gb->debug_next_command = false;
        gb->debug_fin_command = false;
        gb->stack_leak_detection = false;
        input = gb->input_callback(gb);

        if (input == NULL) {
            /* Debugging is no currently available, continue running */
            gb->debug_stopped = false;
            return;
        }

        if (GB_debugger_execute_command(gb, input)) {
            goto next_command;
        }

        free(input);
    }
}

void GB_debugger_handle_async_commands(GB_gameboy_t *gb)
{
    char *input = NULL;

    while (gb->async_input_callback && (input = gb->async_input_callback(gb))) {
        GB_debugger_execute_command(gb, input);
        free(input);
    }
}

void GB_debugger_add_symbol(GB_gameboy_t *gb, uint16_t bank, uint16_t address, const char *symbol)
{
    bank &= 0x1FF;

    if (!gb->bank_symbols[bank]) {
        gb->bank_symbols[bank] = GB_map_alloc();
    }
    GB_bank_symbol_t *allocated_symbol = GB_map_add_symbol(gb->bank_symbols[bank], address, symbol);
    if (allocated_symbol) {
        GB_reversed_map_add_symbol(&gb->reversed_symbol_map, bank, allocated_symbol);
    }
}

void GB_debugger_load_symbol_file(GB_gameboy_t *gb, const char *path)
{
    FILE *f = fopen(path, "r");
    if (!f) return;

    char *line = NULL;
    size_t size = 0;
    size_t length = 0;
    while ((length = getline(&line, &size, f)) != -1) {
        for (unsigned i = 0; i < length; i++) {
            if (line[i] == ';' || line[i] == '\n' || line[i] == '\r') {
                line[i] = 0;
                length = i;
                break;
            }
        }
        if (length == 0) continue;

        unsigned bank, address;
        char symbol[length];

        if (sscanf(line, "%x:%x %s", &bank, &address, symbol) == 3) {
            GB_debugger_add_symbol(gb, bank, address, symbol);
        }
    }
    free(line);
    fclose(f);
}

void GB_debugger_clear_symbols(GB_gameboy_t *gb)
{
    for (unsigned i = sizeof(gb->bank_symbols) / sizeof(gb->bank_symbols[0]); i--;) {
        if (gb->bank_symbols[i]) {
            GB_map_free(gb->bank_symbols[i]);
            gb->bank_symbols[i] = 0;
        }
    }
    for (unsigned i = sizeof(gb->reversed_symbol_map.buckets) / sizeof(gb->reversed_symbol_map.buckets[0]); i--;) {
        while (gb->reversed_symbol_map.buckets[i]) {
            GB_symbol_t *next = gb->reversed_symbol_map.buckets[i]->next;
            free(gb->reversed_symbol_map.buckets[i]);
            gb->reversed_symbol_map.buckets[i] = next;
        }
    }
}

const GB_bank_symbol_t *GB_debugger_find_symbol(GB_gameboy_t *gb, uint16_t addr)
{
    uint16_t bank = bank_for_addr(gb, addr);

    const GB_bank_symbol_t *symbol = GB_map_find_symbol(gb->bank_symbols[bank], addr);
    if (symbol) return symbol;
    if (bank != 0) return GB_map_find_symbol(gb->bank_symbols[0], addr); /* Maybe the symbol incorrectly uses bank 0? */

    return NULL;
}

const char *GB_debugger_name_for_address(GB_gameboy_t *gb, uint16_t addr)
{
    const GB_bank_symbol_t *symbol = GB_debugger_find_symbol(gb, addr);
    if (symbol && symbol->addr == addr) return symbol->name;
    return NULL;
}

/* The public version of debugger_evaluate */
bool GB_debugger_evaluate(GB_gameboy_t *gb, const char *string, uint16_t *result, uint16_t *result_bank)
{
    bool error = false;
    value_t value = debugger_evaluate(gb, string, strlen(string), &error, NULL, NULL);
    if (result) {
        *result = value.value;
    }
    if (result_bank) {
        *result_bank = value.has_bank? value.value : -1;
    }
    return error;
}

void GB_debugger_break(GB_gameboy_t *gb)
{
    gb->debug_stopped = true;
}

bool GB_debugger_is_stopped(GB_gameboy_t *gb)
{
    return gb->debug_stopped;
}

void GB_debugger_set_disabled(GB_gameboy_t *gb, bool disabled)
{
    gb->debug_disable = disabled;
}

/* Jump-to breakpoints */

static bool is_in_trivial_memory(uint16_t addr)
{
    /* ROM */
    if (addr < 0x8000) {
        return true;
    }

    /* HRAM */
    if (addr >= 0xFF80 && addr < 0xFFFF) {
        return true;
    }

    /* RAM */
    if (addr >= 0xC000 && addr < 0xE000) {
        return true;
    }

    return false;
}

typedef uint16_t GB_opcode_address_getter_t(GB_gameboy_t *gb, uint8_t opcode);

uint16_t trivial_1(GB_gameboy_t *gb, uint8_t opcode)
{
    return gb->pc + 1;
}

uint16_t trivial_2(GB_gameboy_t *gb, uint8_t opcode)
{
    return gb->pc + 2;
}

uint16_t trivial_3(GB_gameboy_t *gb, uint8_t opcode)
{
    return gb->pc + 3;
}

static uint16_t jr_r8(GB_gameboy_t *gb, uint8_t opcode)
{
    return gb->pc + 2 + (int8_t)GB_read_memory(gb, gb->pc + 1);
}

static bool condition_code(GB_gameboy_t *gb, uint8_t opcode)
{
    switch ((opcode >> 3) & 0x3) {
        case 0:
            return !(gb->registers[GB_REGISTER_AF] & GB_ZERO_FLAG);
        case 1:
            return (gb->registers[GB_REGISTER_AF] & GB_ZERO_FLAG);
        case 2:
            return !(gb->registers[GB_REGISTER_AF] & GB_CARRY_FLAG);
        case 3:
            return (gb->registers[GB_REGISTER_AF] & GB_CARRY_FLAG);
    }

    return false;
}

static uint16_t jr_cc_r8(GB_gameboy_t *gb, uint8_t opcode)
{
    if (!condition_code(gb, opcode)) {
        return gb->pc + 2;
    }

    return gb->pc + 2 + (int8_t)GB_read_memory(gb, gb->pc + 1);
}

static uint16_t ret(GB_gameboy_t *gb, uint8_t opcode)
{
    return GB_read_memory(gb, gb->registers[GB_REGISTER_SP]) |
           (GB_read_memory(gb, gb->registers[GB_REGISTER_SP] + 1) << 8);
}


static uint16_t ret_cc(GB_gameboy_t *gb, uint8_t opcode)
{
    if (condition_code(gb, opcode)) {
        return ret(gb, opcode);
    }
    else {
        return gb->pc + 1;
    }
}

static uint16_t jp_a16(GB_gameboy_t *gb, uint8_t opcode)
{
    return GB_read_memory(gb, gb->pc + 1) |
           (GB_read_memory(gb, gb->pc + 2) << 8);
}

static uint16_t jp_cc_a16(GB_gameboy_t *gb, uint8_t opcode)
{
    if (condition_code(gb, opcode)) {
        return jp_a16(gb, opcode);
    }
    else {
        return gb->pc + 3;
    }
}

static uint16_t rst(GB_gameboy_t *gb, uint8_t opcode)
{
    return opcode ^ 0xC7;
}

static uint16_t jp_hl(GB_gameboy_t *gb, uint8_t opcode)
{
    return gb->hl;
}

static GB_opcode_address_getter_t *opcodes[256] = {
    /*  X0          X1          X2          X3          X4          X5          X6          X7                */
    /*  X8          X9          Xa          Xb          Xc          Xd          Xe          Xf                */
    trivial_1,  trivial_3,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_2,  trivial_1,   /* 0X */
    trivial_3,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_2,  trivial_1,
    trivial_2,  trivial_3,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_2,  trivial_1,  /* 1X */
    jr_r8,      trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_2,  trivial_1,
    jr_cc_r8,   trivial_3,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_2,  trivial_1,  /* 2X */
    jr_cc_r8,   trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_2,  trivial_1,
    jr_cc_r8,   trivial_3,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_2,  trivial_1,  /* 3X */
    jr_cc_r8,   trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_2,  trivial_1,
    trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  /* 4X */
    trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,
    trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  /* 5X */
    trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,
    trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  /* 6X */
    trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,
    trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  NULL,       trivial_1,  /* 7X */
    trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,
    trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  /* 8X */
    trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,
    trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  /* 9X */
    trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,
    trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  /* aX */
    trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,
    trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  /* bX */
    trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,  trivial_1,
    ret_cc,     trivial_1,  jp_cc_a16,  jp_a16,     jp_cc_a16,  trivial_1,  trivial_2,  rst,        /* cX */
    ret_cc,     ret,        jp_cc_a16,  trivial_2,  jp_cc_a16,  jp_a16,     trivial_2,  rst,
    ret_cc,     trivial_1,  jp_cc_a16,  NULL,       jp_cc_a16,  trivial_1,  trivial_2,  rst,        /* dX */
    ret_cc,     ret,        jp_cc_a16,  NULL,       jp_cc_a16,  NULL,       trivial_2,  rst,
    trivial_2,  trivial_1,  trivial_1,  NULL,       NULL,       trivial_1,  trivial_2,  rst,        /* eX */
    trivial_2,  jp_hl,      trivial_3,  NULL,       NULL,       NULL,       trivial_2,  rst,
    trivial_2,  trivial_1,  trivial_1,  trivial_1,  NULL,       trivial_1,  trivial_2,  rst,        /* fX */
    trivial_2,  trivial_1,  trivial_3,  trivial_1,  NULL,       NULL,       trivial_2,  rst,
};

static jump_to_return_t test_jump_to_breakpoints(GB_gameboy_t *gb, uint16_t *address)
{
    if (!gb->has_jump_to_breakpoints) return JUMP_TO_NONE;

    if (!is_in_trivial_memory(gb->pc) || !is_in_trivial_memory(gb->pc + 2) ||
        !is_in_trivial_memory(gb->registers[GB_REGISTER_SP]) || !is_in_trivial_memory(gb->registers[GB_REGISTER_SP] + 1)) {
        return JUMP_TO_NONTRIVIAL;
    }

    /* Interrupts */
    if (gb->ime) {
        for (unsigned i = 0; i < 5; i++) {
            if ((gb->interrupt_enable & (1 << i)) && (gb->io_registers[GB_IO_IF] & (1 << i))) {
                if (should_break(gb, 0x40 + i * 8, true)) {
                    if (address) {
                        *address = 0x40 + i * 8;
                    }
                    return JUMP_TO_BREAK;
                }
            }
        }
    }

    uint16_t n_watchpoints = gb->n_watchpoints;
    gb->n_watchpoints = 0;

    uint8_t opcode = GB_read_memory(gb, gb->pc);

    if (opcode == 0x76) {
        gb->n_watchpoints = n_watchpoints;
        if (gb->ime) { /* Already handled in above */
            return JUMP_TO_NONE;
        }

        if (gb->interrupt_enable & gb->io_registers[GB_IO_IF] & 0x1F) {
            return JUMP_TO_NONTRIVIAL; /* HALT bug could occur */
        }

        return JUMP_TO_NONE;
    }

    GB_opcode_address_getter_t *getter = opcodes[opcode];
    if (!getter) {
        gb->n_watchpoints = n_watchpoints;
        return JUMP_TO_NONE;
    }

    uint16_t new_pc = getter(gb, opcode);

    gb->n_watchpoints = n_watchpoints;

    if (address) {
        *address = new_pc;
    }

    return should_break(gb, new_pc, true) ? JUMP_TO_BREAK : JUMP_TO_NONE;
}
