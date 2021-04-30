// The tester requires low-level access to the GB struct to detect failures
#define GB_INTERNAL

#include <stdio.h>
#include <stdbool.h>
#include <unistd.h>
#include <time.h>
#include <assert.h>
#include <signal.h>
#ifdef _WIN32
#include <direct.h>
#include <windows.h>
#define snprintf _snprintf
#else
#include <sys/wait.h>
#endif

#include <Core/gb.h>
#include <Core/random.h>

static bool running = false;
static char *filename;
static char *bmp_filename;
static char *log_filename;
static FILE *log_file;
static void replace_extension(const char *src, size_t length, char *dest, const char *ext);
static bool push_start_a, start_is_not_first, a_is_bad, b_is_confirm, push_faster, push_slower,
            do_not_stop, push_a_twice, start_is_bad, allow_weird_sp_values, large_stack, push_right,
            semi_random, limit_start, pointer_control;
static unsigned int test_length = 60 * 40;
GB_gameboy_t gb;

static unsigned int frames = 0;
const char bmp_header[] = {
0x42, 0x4D, 0x48, 0x68, 0x01, 0x00, 0x00, 0x00,
0x00, 0x00, 0x46, 0x00, 0x00, 0x00, 0x38, 0x00,
0x00, 0x00, 0xA0, 0x00, 0x00, 0x00, 0x70, 0xFF,
0xFF, 0xFF, 0x01, 0x00, 0x20, 0x00, 0x03, 0x00,
0x00, 0x00, 0x02, 0x68, 0x01, 0x00, 0x12, 0x0B,
0x00, 0x00, 0x12, 0x0B, 0x00, 0x00, 0x00, 0x00,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0xFF, 0x00, 0x00, 0xFF, 0x00, 0x00, 0xFF,
0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
};

uint32_t bitmap[160*144];

static char *async_input_callback(GB_gameboy_t *gb)
{
    return NULL;
}

static void handle_buttons(GB_gameboy_t *gb)
{
    /* Do not press any buttons during the last two seconds, this might cause a
     screenshot to be taken while the LCD is off if the press makes the game
     load graphics. */
    if (push_start_a && (frames < test_length - 120 || do_not_stop)) {
        unsigned combo_length = 40;
        if (start_is_not_first || push_a_twice) combo_length = 60; /* The start item in the menu is not the first, so also push down */
        else if (a_is_bad || start_is_bad) combo_length = 20; /* Pressing A has a negative effect (when trying to start the game). */
        
        if (semi_random) {
            if (frames % 10 == 0) {
                unsigned key = (((frames / 20) * 0x1337cafe) >> 29) & 7;
                gb->keys[0][key] = (frames % 20) == 0;
            }
        }
        else {
            switch ((push_faster ? frames * 2 :
                     push_slower ? frames / 2 :
                     push_a_twice? frames / 4:
                     frames) % combo_length + (start_is_bad? 20 : 0) ) {
                case 0:
                    if (!limit_start || frames < 20 * 60) {
                        GB_set_key_state(gb, push_right? GB_KEY_RIGHT: GB_KEY_START, true);
                    }
                    if (pointer_control) {
                        GB_set_key_state(gb, GB_KEY_LEFT, true);
                        GB_set_key_state(gb, GB_KEY_UP, true);
                    }
                    
                    break;
                case 10:
                    GB_set_key_state(gb, push_right? GB_KEY_RIGHT: GB_KEY_START, false);
                    if (pointer_control) {
                        GB_set_key_state(gb, GB_KEY_LEFT, false);
                        GB_set_key_state(gb, GB_KEY_UP, false);
                    }
                    break;
                case 20:
                    GB_set_key_state(gb, b_is_confirm? GB_KEY_B: GB_KEY_A, true);
                    break;
                case 30:
                    GB_set_key_state(gb, b_is_confirm? GB_KEY_B: GB_KEY_A, false);
                    break;
                case 40:
                    if (push_a_twice) {
                        GB_set_key_state(gb, b_is_confirm? GB_KEY_B: GB_KEY_A, true);
                    }
                    else if (gb->boot_rom_finished) {
                        GB_set_key_state(gb, GB_KEY_DOWN, true);
                    }
                    break;
                case 50:
                    GB_set_key_state(gb, b_is_confirm? GB_KEY_B: GB_KEY_A, false);
                    GB_set_key_state(gb, GB_KEY_DOWN, false);
                    break;
            }
        }
    }

}

static void vblank(GB_gameboy_t *gb)
{
    /* Detect common crashes and stop the test early */
    if (frames < test_length - 1) {
        if (gb->backtrace_size >= 0x200 + (large_stack? 0x80: 0) || (!allow_weird_sp_values && (gb->registers[GB_REGISTER_SP] >= 0xfe00 && gb->registers[GB_REGISTER_SP] < 0xff80))) {
            GB_log(gb, "A stack overflow has probably occurred. (SP = $%04x; backtrace size = %d) \n",
                   gb->registers[GB_REGISTER_SP], gb->backtrace_size);
            frames = test_length - 1;
        }
        if (gb->halted && !gb->interrupt_enable) {
            GB_log(gb, "The game is deadlocked.\n");
            frames = test_length - 1;
        }
    }

    if (frames >= test_length && !gb->disable_rendering) {
        bool is_screen_blank = true;
        for (unsigned i = 160*144; i--;) {
            if (bitmap[i] != bitmap[0]) {
                is_screen_blank = false;
                break;
            }
        }
        
        /* Let the test run for extra four seconds if the screen is off/disabled */
        if (!is_screen_blank || frames >= test_length + 60 * 4) {
            FILE *f = fopen(bmp_filename, "wb");
            fwrite(&bmp_header, 1, sizeof(bmp_header), f);
            fwrite(&bitmap, 1, sizeof(bitmap), f);
            fclose(f);
            if (!gb->boot_rom_finished) {
                GB_log(gb, "Boot ROM did not finish.\n");
            }
            if (is_screen_blank) {
                GB_log(gb, "Game probably stuck with blank screen. \n");
            }
            running = false;
        }
    }
    else if (frames >= test_length - 1) {
        gb->disable_rendering = false;
    }
}

static void log_callback(GB_gameboy_t *gb, const char *string, GB_log_attributes attributes)
{
    if (!log_file) log_file = fopen(log_filename, "w");
    fprintf(log_file, "%s", string);
}

#ifdef __APPLE__
#include <mach-o/dyld.h>
#endif

static const char *executable_folder(void)
{
    static char path[1024] = {0,};
    if (path[0]) {
        return path;
    }
    /* Ugly unportable code! :( */
#ifdef __APPLE__
    uint32_t length = sizeof(path) - 1;
    _NSGetExecutablePath(&path[0], &length);
#else
#ifdef __linux__
    size_t __attribute__((unused)) length = readlink("/proc/self/exe", &path[0], sizeof(path) - 1);
    assert(length != -1);
#else
#ifdef _WIN32
    HMODULE hModule = GetModuleHandle(NULL);
    GetModuleFileName(hModule, path, sizeof(path) - 1);
#else
    /* No OS-specific way, assume running from CWD */
    getcwd(&path[0], sizeof(path) - 1);
    return path;
#endif
#endif
#endif
    size_t pos = strlen(path);
    while (pos) {
        pos--;
#ifdef _WIN32
        if (path[pos] == '\\') {
#else
        if (path[pos] == '/') {
#endif
            path[pos] = 0;
            break;
        }
    }
    return path;
}

static char *executable_relative_path(const char *filename)
{
    static char path[1024];
    snprintf(path, sizeof(path), "%s/%s", executable_folder(), filename);
    return path;
}

static uint32_t rgb_encode(GB_gameboy_t *gb, uint8_t r, uint8_t g, uint8_t b)
{
    return (r << 24) | (g << 16) | (b << 8);
}

static void replace_extension(const char *src, size_t length, char *dest, const char *ext)
{
    memcpy(dest, src, length);
    dest[length] = 0;

    /* Remove extension */
    for (size_t i = length; i--;) {
        if (dest[i] == '/') break;
        if (dest[i] == '.') {
            dest[i] = 0;
            break;
        }
    }

    /* Add new extension */
    strcat(dest, ext);
}


int main(int argc, char **argv)
{
#define str(x) #x
#define xstr(x) str(x)
    fprintf(stderr, "SameBoy Tester v" xstr(VERSION) "\n");

    if (argc == 1) {
        fprintf(stderr, "Usage: %s [--dmg] [--start] [--length seconds] [--boot path to boot ROM]"
#ifndef _WIN32
                        " [--jobs number of tests to run simultaneously]"
#endif
                        " rom ...\n", argv[0]);
        exit(1);
    }

#ifndef _WIN32
    unsigned int max_forks = 1;
    unsigned int current_forks = 0;
#endif

    bool dmg = false;
    const char *boot_rom_path = NULL;
    
    GB_random_set_enabled(false);

    for (unsigned i = 1; i < argc; i++) {
        if (strcmp(argv[i], "--dmg") == 0) {
            fprintf(stderr, "Using DMG mode\n");
            dmg = true;
            continue;
        }

        if (strcmp(argv[i], "--start") == 0) {
            fprintf(stderr, "Pushing Start and A\n");
            push_start_a = true;
            continue;
        }
        
        if (strcmp(argv[i], "--length") == 0 && i != argc - 1) {
            test_length = atoi(argv[++i]) * 60;
            fprintf(stderr, "Test length is %d seconds\n", test_length / 60);
            continue;
        }
        
        if (strcmp(argv[i], "--boot") == 0 && i != argc - 1) {
            fprintf(stderr, "Using boot ROM %s\n", argv[i + 1]);
            boot_rom_path = argv[++i];
            continue;
        }
        
#ifndef _WIN32
        if (strcmp(argv[i], "--jobs") == 0 && i != argc - 1) {
            max_forks = atoi(argv[++i]);
            /* Make sure wrong input doesn't blow anything up. */
            if (max_forks < 1) max_forks = 1;
            if (max_forks > 16) max_forks = 16;
            fprintf(stderr, "Running up to %d tests simultaneously\n", max_forks);
            continue;
        }

        if (max_forks > 1) {
            while (current_forks >= max_forks) {
                int wait_out;
                while (wait(&wait_out) == -1);
                current_forks--;
            }
            
            current_forks++;
            if (fork() != 0) continue;
        }
#endif
        filename = argv[i];
        size_t path_length = strlen(filename);

        char bitmap_path[path_length + 5]; /* At the worst case, size is strlen(path) + 4 bytes for .bmp + NULL */
        replace_extension(filename, path_length, bitmap_path, ".bmp");
        bmp_filename = &bitmap_path[0];
        
        char log_path[path_length + 5];
        replace_extension(filename, path_length, log_path, ".log");
        log_filename = &log_path[0];
        
        fprintf(stderr, "Testing ROM %s\n", filename);
        
        if (dmg) {
            GB_init(&gb, GB_MODEL_DMG_B);
            if (GB_load_boot_rom(&gb, boot_rom_path ?: executable_relative_path("dmg_boot.bin"))) {
                fprintf(stderr, "Failed to load boot ROM from '%s'\n", boot_rom_path ?: executable_relative_path("dmg_boot.bin"));
                exit(1);
            }
        }
        else {
            GB_init(&gb, GB_MODEL_CGB_E);
            if (GB_load_boot_rom(&gb, boot_rom_path ?: executable_relative_path("cgb_boot.bin"))) {
                fprintf(stderr, "Failed to load boot ROM from '%s'\n", boot_rom_path ?: executable_relative_path("cgb_boot.bin"));
                exit(1);
            }
        }
        
        GB_set_vblank_callback(&gb, (GB_vblank_callback_t) vblank);
        GB_set_pixels_output(&gb, &bitmap[0]);
        GB_set_rgb_encode_callback(&gb, rgb_encode);
        GB_set_log_callback(&gb, log_callback);
        GB_set_async_input_callback(&gb, async_input_callback);
        GB_set_color_correction_mode(&gb, GB_COLOR_CORRECTION_EMULATE_HARDWARE);
        
        if (GB_load_rom(&gb, filename)) {
            perror("Failed to load ROM");
            exit(1);
        }
        
        /* Game specific hacks for start attempt automations */
        /* It's OK. No overflow is possible here. */
        start_is_not_first = strcmp((const char *)(gb.rom + 0x134), "NEKOJARA") == 0 ||
                             strcmp((const char *)(gb.rom + 0x134), "GINGA") == 0;
        a_is_bad = strcmp((const char *)(gb.rom + 0x134), "DESERT STRIKE") == 0 ||
                    /* Restarting in Puzzle Boy/Kwirk (Start followed by A) leaks stack. */
                   strcmp((const char *)(gb.rom + 0x134), "KWIRK") == 0 ||
                   strcmp((const char *)(gb.rom + 0x134), "PUZZLE BOY") == 0;
        start_is_bad = strcmp((const char *)(gb.rom + 0x134), "BLUESALPHA") == 0 ||
                       strcmp((const char *)(gb.rom + 0x134), "ONI 5") == 0;
        b_is_confirm = strcmp((const char *)(gb.rom + 0x134), "ELITE SOCCER") == 0 ||
                       strcmp((const char *)(gb.rom + 0x134), "SOCCER") == 0 ||
                       strcmp((const char *)(gb.rom + 0x134), "GEX GECKO") == 0;
        push_faster = strcmp((const char *)(gb.rom + 0x134), "MOGURA DE PON!") == 0 ||
                      strcmp((const char *)(gb.rom + 0x134), "HUGO2 1/2") == 0 ||
                      strcmp((const char *)(gb.rom + 0x134), "HUGO") == 0;
        push_slower = strcmp((const char *)(gb.rom + 0x134), "BAKENOU") == 0;
        do_not_stop = strcmp((const char *)(gb.rom + 0x134), "SPACE INVADERS") == 0;
        push_right = memcmp((const char *)(gb.rom + 0x134), "BOB ET BOB", strlen("BOB ET BOB")) == 0 ||
                     strcmp((const char *)(gb.rom + 0x134), "LITTLE MASTER") == 0 ||
                     /* M&M's Minis Madness Demo (which has no menu but the same title as the full game) */
                     (memcmp((const char *)(gb.rom + 0x134), "MINIMADNESSBMIE", strlen("MINIMADNESSBMIE")) == 0 &&
                      gb.rom[0x14e] == 0x6c);
        /* This game has some terrible menus. */
        semi_random = strcmp((const char *)(gb.rom + 0x134), "KUKU GAME") == 0;
        

        
        /* This game temporarily sets SP to OAM RAM */
        allow_weird_sp_values = strcmp((const char *)(gb.rom + 0x134), "WDL:TT") == 0 ||
        /* Some mooneye-gb tests abuse the stack */
                                strcmp((const char *)(gb.rom + 0x134), "mooneye-gb test") == 0;
        
        /* This game uses some recursive algorithms and therefore requires quite a large call stack */
        large_stack = memcmp((const char *)(gb.rom + 0x134), "MICRO EPAK1BM", strlen("MICRO EPAK1BM")) == 0 ||
                      strcmp((const char *)(gb.rom + 0x134), "TECMO BOWL") == 0;
        /* High quality game that leaks stack whenever you open the menu (with start),
         but requires pressing start to play it. */
        limit_start = strcmp((const char *)(gb.rom + 0x134), "DIVA STARS") == 0;
        large_stack |= limit_start;

        /* Pressing start while in the map in Tsuri Sensei will leak an internal screen-stack which
           will eventually overflow, override an array of jump-table indexes, jump to a random
           address, execute an invalid opcode, and crash. Pressing A twice while slowing down
           will prevent this scenario. */
        push_a_twice = strcmp((const char *)(gb.rom + 0x134), "TURI SENSEI V1") == 0;

        /* Yes, you should totally use a cursor point & click interface for the language select menu. */
        pointer_control = memcmp((const char *)(gb.rom + 0x134), "LEGO ATEAM BLPP", strlen("LEGO ATEAM BLPP")) == 0;
        push_faster |= pointer_control;
        
        /* Run emulation */
        running = true;
        gb.turbo = gb.turbo_dont_skip = gb.disable_rendering = true;
        frames = 0;
        unsigned cycles = 0;
        while (running) {
            cycles += GB_run(&gb);
            if (cycles >= 139810) { /* Approximately 1/60 a second. Intentionally not the actual length of a frame. */
                handle_buttons(&gb);
                cycles -= 139810;
                frames++;
            }
            /* This early crash test must not run in vblank because PC might not point to the next instruction. */
            if (gb.pc == 0x38 && frames < test_length - 1 && GB_read_memory(&gb, 0x38) == 0xFF) {
                GB_log(&gb, "The game is probably stuck in an FF loop.\n");
                frames = test_length - 1;
            }
        }
        
        
        if (log_file) {
            fclose(log_file);
            log_file = NULL;
        }
        
        GB_free(&gb);
#ifndef _WIN32
        if (max_forks > 1) {
            exit(0);
        }
#endif
    }
#ifndef _WIN32
    int wait_out;
    while (wait(&wait_out) != -1);
#endif
    return 0;
}

