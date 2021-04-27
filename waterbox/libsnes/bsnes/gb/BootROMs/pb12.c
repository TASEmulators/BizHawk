#include <stdio.h>
#include <stdint.h>
#include <stdbool.h>
#include <stdlib.h>
#include <unistd.h>
#include <assert.h>

void opts(uint8_t byte, uint8_t *options)
{
    *(options++) = byte | ((byte << 1) & 0xff);
    *(options++) = byte & (byte << 1);
    *(options++) = byte | ((byte >> 1) & 0xff);
    *(options++) = byte & (byte >> 1);
}

void write_all(int fd, const void *buf, size_t count) {
    while (count) {
        ssize_t written = write(fd, buf, count);
        if (written < 0) {
            fprintf(stderr, "write");
            exit(1);
        }
        count -= written;
        buf += written;
    }
}

int main()
{
    static uint8_t source[0x4000];
    size_t size = read(STDIN_FILENO, &source, sizeof(source));
    unsigned pos = 0;
    assert(size <= 0x4000);
    while (size && source[size - 1] == 0) {
        size--;
    }
    
    uint8_t literals[8];
    size_t literals_size = 0;
    unsigned bits = 0;
    unsigned control = 0;
    unsigned prev[2] = {-1, -1}; // Unsigned to allow "not set" values
    
    while (true) {

        uint8_t byte = 0;
        if (pos == size){
            if (bits == 0) break;
        }
        else {
            byte = source[pos++];
        }
        
        if (byte == prev[0] || byte == prev[1]) {
            bits += 2;
            control <<= 1;
            control |= 1;
            control <<= 1;
            if (byte == prev[1]) {
                control |= 1;
            }
        }
        else {
            bits += 2;
            control <<= 2;
            uint8_t options[4];
            opts(prev[1], options);
            bool found = false;
            for (unsigned i = 0; i < 4; i++) {
                if (options[i] == byte) {
                    // 01 = modify
                    control |= 1;
                    
                    bits += 2;
                    control <<= 2;
                    control |= i;
                    found = true;
                    break;
                }
            }
            if (!found) {
                literals[literals_size++] = byte;
            }
        }
        
        prev[0] = prev[1];
        prev[1] = byte;
        if (bits >= 8) {
            uint8_t outctl = control >> (bits - 8);
            assert(outctl != 1);
            write_all(STDOUT_FILENO, &outctl, 1);
            write_all(STDOUT_FILENO, literals, literals_size);
            bits -= 8;
            control &= (1 << bits) - 1;
            literals_size = 0;
        }
    }
    uint8_t end_byte = 1;
    write_all(STDOUT_FILENO, &end_byte, 1);

    return 0;
}
