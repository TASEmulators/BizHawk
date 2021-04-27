#import <Cocoa/Cocoa.h>
#include "GBView.h"
#include "GBImageView.h"
#include "GBSplitView.h"

@class GBCheatWindowController;

@interface Document : NSDocument <NSWindowDelegate, GBImageViewDelegate, NSTableViewDataSource, NSTableViewDelegate, NSSplitViewDelegate>
@property (strong) IBOutlet GBView *view;
@property (strong) IBOutlet NSTextView *consoleOutput;
@property (strong) IBOutlet NSPanel *consoleWindow;
@property (strong) IBOutlet NSTextField *consoleInput;
@property (strong) IBOutlet NSWindow *mainWindow;
@property (strong) IBOutlet NSView *memoryView;
@property (strong) IBOutlet NSPanel *memoryWindow;
@property (readonly) GB_gameboy_t *gameboy;
@property (strong) IBOutlet NSTextField *memoryBankInput;
@property (strong) IBOutlet NSToolbarItem *memoryBankItem;
@property (strong) IBOutlet GBImageView *tilesetImageView;
@property (strong) IBOutlet NSPopUpButton *tilesetPaletteButton;
@property (strong) IBOutlet GBImageView *tilemapImageView;
@property (strong) IBOutlet NSPopUpButton *tilemapPaletteButton;
@property (strong) IBOutlet NSPopUpButton *tilemapMapButton;
@property (strong) IBOutlet NSPopUpButton *TilemapSetButton;
@property (strong) IBOutlet NSButton *gridButton;
@property (strong) IBOutlet NSTabView *vramTabView;
@property (strong) IBOutlet NSPanel *vramWindow;
@property (strong) IBOutlet NSTextField *vramStatusLabel;
@property (strong) IBOutlet NSTableView *paletteTableView;
@property (strong) IBOutlet NSTableView *spritesTableView;
@property (strong) IBOutlet NSPanel *printerFeedWindow;
@property (strong) IBOutlet NSImageView *feedImageView;
@property (strong) IBOutlet NSTextView *debuggerSideViewInput;
@property (strong) IBOutlet NSTextView *debuggerSideView;
@property (strong) IBOutlet GBSplitView *debuggerSplitView;
@property (strong) IBOutlet NSBox *debuggerVerticalLine;
@property (strong) IBOutlet NSPanel *cheatsWindow;
@property (strong) IBOutlet GBCheatWindowController *cheatWindowController;

-(uint8_t) readMemory:(uint16_t) addr;
-(void) writeMemory:(uint16_t) addr value:(uint8_t)value;
-(void) performAtomicBlock: (void (^)())block;

@end

