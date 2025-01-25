#ifndef BINDINGS_H_
#define BINDINGS_H_
#include <stdint.h>

typedef enum {
	MOUSE_NONE,     //mouse is ignored
	MOUSE_ABSOLUTE, //really only useful for menu ROM
	MOUSE_RELATIVE, //for full screen
	MOUSE_CAPTURE   //for windowed mode
} mouse_modes;

void set_bindings(void);
void bindings_set_mouse_mode(uint8_t mode);
tern_node *get_binding_node_for_pad(int padnum);
void handle_keydown(int keycode, uint8_t scancode);
void handle_keyup(int keycode, uint8_t scancode);
void handle_joydown(int joystick, int button);
void handle_joyup(int joystick, int button);
void handle_joy_dpad(int joystick, int dpad, uint8_t state);
void handle_joy_axis(int joystick, int axis, int16_t value);
void handle_joy_added(int joystick);
void handle_mouse_moved(int mouse, uint16_t x, uint16_t y, int16_t deltax, int16_t deltay);
void handle_mousedown(int mouse, int button);
void handle_mouseup(int mouse, int button);

void bindings_release_capture(void);
void bindings_reacquire_capture(void);
void set_content_binding_state(uint8_t enabled);

#endif //BINDINGS_H_
