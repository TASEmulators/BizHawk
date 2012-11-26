// TODO WIP EXPERIMENTAL

enum
{
 MDFNKEY_ESCAPE,
 MDFNKEY_F1,
 MDFNKEY_F2,
 MDFNKEY_F3,
 MDFNKEY_F4,
 MDFNKEY_F5,
 MDFNKEY_F6,
 MDFNKEY_F7,
 MDFNKEY_F8,
 MDFNKEY_F9,
 MDFNKEY_F10,
 MDFNKEY_F11,
 MDFNKEY_F12,

 MDFNKEY_PRINTSCREEN,
 MDFNKEY_SCROLLLOCK,
 MDFNKEY_PAUSE,

 MDFNKEY_GRAVE,
 MDFNKEY_0,
 MDFNKEY_1,
 MDFNKEY_2,
 MDFNKEY_3,
 MDFNKEY_4,
 MDFNKEY_5,
 MDFNKEY_6,
 MDFNKEY_7,
 MDFNKEY_8,
 MDFNKEY_9,
 MDFNKEY_MINUS,
 MDFNKEY_EQUAL,
 MDFNKEY_BACKSPACE


 MDFNKEY_INSERT,
 MDFNKEY_HOME,
 MDFNKEY_PAGEUP,
 MDFNKEY_PAGEDOWN,
 MDFNKEY_DELETE,
 MDFNKEY_END,

 MDFNKEY_UP,
 MDFNKEY_DOWN,
 MDFNKEY_LEFT,
 MDFNKEY_RIGHT
};

#define MDFNKEYMOD_LSHIFT	0x0001
#define MDFNKEYMOD_RSHIFT	0x0002
#define MDFNKEYMOD_LCTRL	0x0004
#define MDFNKEYMOD_RCTRL	0x0008
#define MDFNKEYMOD_LALT		0x0010
#define MDFNKEYMOD_RALT		0x0020

struct MDFN_KeyEvent
{
 enum { PressEvent = 1, ReleaseEvent = 2 };
 uint8 type;
 uint8 device;
 uint8 keycode;		// MDFNKEY_* (a select few scancodes translated into common key codes for default hotkey/input configuration)
 uint16 modifier;	// MDFNKEYMOD_*
 uint16 scancode;	// Rawish keyboard scancode.  Used for custom input configurations.
 uint32 unicode;	// Unicode glyph(0 if not applicable), based on scancode, modifier, and MAYBE DRAGONS.  For text entry.
};

class KeyboardManager
{
 public:

 KeyboardManager();
 ~KeyboardManager();

 void Reset_BC_ChangeCheck(void);
 bool Do_BC_ChangeCheck(ButtConfig *bc);

 void UpdateKeyboards(std::vector<MDFN_KeyEvent> *event_queue = NULL);	// Maybe use a fixed-size queue/FIFO instead to eliminate memory allocs?

 //unsigned GetIndexByUniqueID(uint64 unique_id);	// Returns ~0U if joystick was not found.
 //unsigned GetUniqueIDByIndex(unsigned index);

 private:
 std::vector<JoystickDriver *> JoystickDrivers;
 std::vector<JoystickManager_Cache> JoystickCache;
 ButtConfig BCPending;
};
