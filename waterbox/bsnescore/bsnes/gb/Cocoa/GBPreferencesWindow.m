#import "GBPreferencesWindow.h"
#import "NSString+StringForKey.h"
#import "GBButtons.h"
#import "BigSurToolbar.h"
#import <Carbon/Carbon.h>

@implementation GBPreferencesWindow
{
    bool is_button_being_modified;
    NSInteger button_being_modified;
    signed joystick_configuration_state;
    NSString *joystick_being_configured;
    bool joypad_wait;

    NSPopUpButton *_graphicsFilterPopupButton;
    NSPopUpButton *_highpassFilterPopupButton;
    NSPopUpButton *_colorCorrectionPopupButton;
    NSPopUpButton *_frameBlendingModePopupButton;
    NSPopUpButton *_colorPalettePopupButton;
    NSPopUpButton *_displayBorderPopupButton;
    NSPopUpButton *_rewindPopupButton;
    NSButton *_aspectRatioCheckbox;
    NSButton *_analogControlsCheckbox;
    NSEventModifierFlags previousModifiers;
    
    NSPopUpButton *_dmgPopupButton, *_sgbPopupButton, *_cgbPopupButton;
    NSPopUpButton *_preferredJoypadButton;
    NSPopUpButton *_rumbleModePopupButton;
}

+ (NSArray *)filterList
{
    /* The filter list as ordered in the popup button */
    static NSArray * filters = nil;
    if (!filters) {
        filters = @[
                    @"NearestNeighbor",
                    @"Bilinear",
                    @"SmoothBilinear",
                    @"MonoLCD",
                    @"LCD",
                    @"CRT",
                    @"Scale2x",
                    @"Scale4x",
                    @"AAScale2x",
                    @"AAScale4x",
                    @"HQ2x",
                    @"OmniScale",
                    @"OmniScaleLegacy",
                    @"AAOmniScaleLegacy",
                    ];
    }
    return filters;
}

- (NSWindowToolbarStyle)toolbarStyle
{
    return NSWindowToolbarStylePreference;
}

- (void)close
{
    joystick_configuration_state = -1;
    [self.configureJoypadButton setEnabled:YES];
    [self.skipButton setEnabled:NO];
    [self.configureJoypadButton setTitle:@"Configure Controller"];
    [super close];
}

- (NSPopUpButton *)graphicsFilterPopupButton
{
    return _graphicsFilterPopupButton;
}

- (void)setGraphicsFilterPopupButton:(NSPopUpButton *)graphicsFilterPopupButton
{
    _graphicsFilterPopupButton = graphicsFilterPopupButton;
    NSString *filter = [[NSUserDefaults standardUserDefaults] objectForKey:@"GBFilter"];
    [_graphicsFilterPopupButton selectItemAtIndex:[[[self class] filterList] indexOfObject:filter]];
}

- (NSPopUpButton *)highpassFilterPopupButton
{
    return _highpassFilterPopupButton;
}

- (void)setColorCorrectionPopupButton:(NSPopUpButton *)colorCorrectionPopupButton
{
    _colorCorrectionPopupButton = colorCorrectionPopupButton;
    NSInteger mode = [[NSUserDefaults standardUserDefaults] integerForKey:@"GBColorCorrection"];
    [_colorCorrectionPopupButton selectItemAtIndex:mode];
}

- (NSPopUpButton *)colorCorrectionPopupButton
{
    return _colorCorrectionPopupButton;
}

- (void)setFrameBlendingModePopupButton:(NSPopUpButton *)frameBlendingModePopupButton
{
    _frameBlendingModePopupButton = frameBlendingModePopupButton;
    NSInteger mode = [[NSUserDefaults standardUserDefaults] integerForKey:@"GBFrameBlendingMode"];
    [_frameBlendingModePopupButton selectItemAtIndex:mode];
}

- (NSPopUpButton *)frameBlendingModePopupButton
{
    return _frameBlendingModePopupButton;
}

- (void)setColorPalettePopupButton:(NSPopUpButton *)colorPalettePopupButton
{
    _colorPalettePopupButton = colorPalettePopupButton;
    NSInteger mode = [[NSUserDefaults standardUserDefaults] integerForKey:@"GBColorPalette"];
    [_colorPalettePopupButton selectItemAtIndex:mode];
}

- (NSPopUpButton *)colorPalettePopupButton
{
    return _colorPalettePopupButton;
}

- (void)setDisplayBorderPopupButton:(NSPopUpButton *)displayBorderPopupButton
{
    _displayBorderPopupButton = displayBorderPopupButton;
    NSInteger mode = [[NSUserDefaults standardUserDefaults] integerForKey:@"GBBorderMode"];
    [_displayBorderPopupButton selectItemWithTag:mode];
}

- (NSPopUpButton *)displayBorderPopupButton
{
    return _displayBorderPopupButton;
}

- (void)setRumbleModePopupButton:(NSPopUpButton *)rumbleModePopupButton
{
    _rumbleModePopupButton = rumbleModePopupButton;
    NSInteger mode = [[NSUserDefaults standardUserDefaults] integerForKey:@"GBRumbleMode"];
    [_rumbleModePopupButton selectItemWithTag:mode];
}

- (NSPopUpButton *)rumbleModePopupButton
{
    return _rumbleModePopupButton;
}

- (void)setRewindPopupButton:(NSPopUpButton *)rewindPopupButton
{
    _rewindPopupButton = rewindPopupButton;
    NSInteger length = [[NSUserDefaults standardUserDefaults] integerForKey:@"GBRewindLength"];
    [_rewindPopupButton selectItemWithTag:length];
}

- (NSPopUpButton *)rewindPopupButton
{
    return _rewindPopupButton;
}

- (void)setHighpassFilterPopupButton:(NSPopUpButton *)highpassFilterPopupButton
{
    _highpassFilterPopupButton = highpassFilterPopupButton;
    [_highpassFilterPopupButton selectItemAtIndex:[[[NSUserDefaults standardUserDefaults] objectForKey:@"GBHighpassFilter"] unsignedIntegerValue]];
}

- (NSInteger)numberOfRowsInTableView:(NSTableView *)tableView
{
    if (self.playerListButton.selectedTag == 0) {
        return GBButtonCount;
    }
    return GBGameBoyButtonCount;
}

- (unsigned) usesForKey:(unsigned) key
{
    unsigned ret = 0;
    for (unsigned player = 4; player--;) {
        for (unsigned button = player == 0? GBButtonCount:GBGameBoyButtonCount; button--;) {
            NSNumber *other = [[NSUserDefaults standardUserDefaults] valueForKey:button_to_preference_name(button, player)];
            if (other && [other unsignedIntValue] == key) {
                ret++;
            }
        }
    }
    return ret;
}

- (id)tableView:(NSTableView *)tableView objectValueForTableColumn:(NSTableColumn *)tableColumn row:(NSInteger)row
{
    if ([tableColumn.identifier isEqualToString:@"keyName"]) {
        return GBButtonNames[row];
    }

    if (is_button_being_modified && button_being_modified == row) {
        return @"Select a new key...";
    }
    
    NSNumber *key = [[NSUserDefaults standardUserDefaults] valueForKey:button_to_preference_name(row, self.playerListButton.selectedTag)];
    if (key) {
        if ([self usesForKey:[key unsignedIntValue]] > 1) {
            return [[NSAttributedString alloc] initWithString:[NSString displayStringForKeyCode: [key unsignedIntegerValue]]
                                                   attributes:@{NSForegroundColorAttributeName: [NSColor colorWithRed:0.9375 green:0.25 blue:0.25 alpha:1.0],
                                                                NSFontAttributeName: [NSFont boldSystemFontOfSize:[NSFont systemFontSize]]
                                                   }];
        }
        return [NSString displayStringForKeyCode: [key unsignedIntegerValue]];
    }

    return @"";
}

- (BOOL)tableView:(NSTableView *)tableView shouldEditTableColumn:(NSTableColumn *)tableColumn row:(NSInteger)row
{

    dispatch_async(dispatch_get_main_queue(), ^{
        is_button_being_modified = true;
        button_being_modified = row;
        tableView.enabled = NO;
        self.playerListButton.enabled = NO;
        [tableView reloadData];
        [self makeFirstResponder:self];
    });
    return NO;
}

-(void)keyDown:(NSEvent *)theEvent
{
    if (!is_button_being_modified) {
        if (self.firstResponder != self.controlsTableView && [theEvent type] != NSEventTypeFlagsChanged) {
            [super keyDown:theEvent];
        }
        return;
    }

    is_button_being_modified = false;

    [[NSUserDefaults standardUserDefaults] setInteger:theEvent.keyCode
                                              forKey:button_to_preference_name(button_being_modified, self.playerListButton.selectedTag)];
    self.controlsTableView.enabled = YES;
    self.playerListButton.enabled = YES;
    [self.controlsTableView reloadData];
    [self makeFirstResponder:self.controlsTableView];
}

- (void) flagsChanged:(NSEvent *)event
{
    if (event.modifierFlags > previousModifiers) {
        [self keyDown:event];
    }
    
    previousModifiers = event.modifierFlags;
}

- (IBAction)graphicFilterChanged:(NSPopUpButton *)sender
{
    [[NSUserDefaults standardUserDefaults] setObject:[[self class] filterList][[sender indexOfSelectedItem]]
                                              forKey:@"GBFilter"];
    [[NSNotificationCenter defaultCenter] postNotificationName:@"GBFilterChanged" object:nil];
}

- (IBAction)highpassFilterChanged:(id)sender
{
    [[NSUserDefaults standardUserDefaults] setObject:@([sender indexOfSelectedItem])
                                              forKey:@"GBHighpassFilter"];
    [[NSNotificationCenter defaultCenter] postNotificationName:@"GBHighpassFilterChanged" object:nil];
}

- (IBAction)changeAnalogControls:(id)sender
{
    [[NSUserDefaults standardUserDefaults] setBool: [(NSButton *)sender state] == NSOnState
                                            forKey:@"GBAnalogControls"];
}

- (IBAction)changeAspectRatio:(id)sender
{
    [[NSUserDefaults standardUserDefaults] setBool: [(NSButton *)sender state] != NSOnState
                                            forKey:@"GBAspectRatioUnkept"];
    [[NSNotificationCenter defaultCenter] postNotificationName:@"GBAspectChanged" object:nil];
}

- (IBAction)colorCorrectionChanged:(id)sender
{
    [[NSUserDefaults standardUserDefaults] setObject:@([sender indexOfSelectedItem])
                                              forKey:@"GBColorCorrection"];
    [[NSNotificationCenter defaultCenter] postNotificationName:@"GBColorCorrectionChanged" object:nil];
}

- (IBAction)franeBlendingModeChanged:(id)sender
{
    [[NSUserDefaults standardUserDefaults] setObject:@([sender indexOfSelectedItem])
                                              forKey:@"GBFrameBlendingMode"];
    [[NSNotificationCenter defaultCenter] postNotificationName:@"GBFrameBlendingModeChanged" object:nil];
    
}

- (IBAction)colorPaletteChanged:(id)sender
{
    [[NSUserDefaults standardUserDefaults] setObject:@([sender indexOfSelectedItem])
                                              forKey:@"GBColorPalette"];
    [[NSNotificationCenter defaultCenter] postNotificationName:@"GBColorPaletteChanged" object:nil];
}

- (IBAction)displayBorderChanged:(id)sender
{
    [[NSUserDefaults standardUserDefaults] setObject:@([sender selectedItem].tag)
                                              forKey:@"GBBorderMode"];
    [[NSNotificationCenter defaultCenter] postNotificationName:@"GBBorderModeChanged" object:nil];
}

- (IBAction)rumbleModeChanged:(id)sender
{
    [[NSUserDefaults standardUserDefaults] setObject:@([sender selectedItem].tag)
                                              forKey:@"GBRumbleMode"];
    [[NSNotificationCenter defaultCenter] postNotificationName:@"GBRumbleModeChanged" object:nil];
}

- (IBAction)rewindLengthChanged:(id)sender
{
    [[NSUserDefaults standardUserDefaults] setObject:@([sender selectedTag])
                                              forKey:@"GBRewindLength"];
    [[NSNotificationCenter defaultCenter] postNotificationName:@"GBRewindLengthChanged" object:nil];
}

- (IBAction) configureJoypad:(id)sender
{
    [self.configureJoypadButton setEnabled:NO];
    [self.skipButton setEnabled:YES];
    joystick_being_configured = nil;
    [self advanceConfigurationStateMachine];
}

- (IBAction) skipButton:(id)sender
{
    [self advanceConfigurationStateMachine];
}

- (void) advanceConfigurationStateMachine
{
    joystick_configuration_state++;
    if (joystick_configuration_state == GBUnderclock) {
        [self.configureJoypadButton setTitle:@"Press Button for Slo-Mo"]; // Full name is too long :<
    }
    else if (joystick_configuration_state < GBButtonCount) {
        [self.configureJoypadButton setTitle:[NSString stringWithFormat:@"Press Button for %@", GBButtonNames[joystick_configuration_state]]];
    }
    else {
        joystick_configuration_state = -1;
        [self.configureJoypadButton setEnabled:YES];
        [self.skipButton setEnabled:NO];
        [self.configureJoypadButton setTitle:@"Configure Joypad"];
    }
}

- (void)controller:(JOYController *)controller buttonChangedState:(JOYButton *)button
{
    /* Debounce */
    if (joypad_wait) return;
    joypad_wait = true;
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(0.25 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
        joypad_wait = false;
    });
        
    if (!button.isPressed) return;
    if (joystick_configuration_state == -1) return;
    if (joystick_configuration_state == GBButtonCount) return;
    if (!joystick_being_configured) {
        joystick_being_configured = controller.uniqueID;
    }
    else if (![joystick_being_configured isEqualToString:controller.uniqueID]) {
        return;
    }
    
    NSMutableDictionary *instance_mappings = [[[NSUserDefaults standardUserDefaults] dictionaryForKey:@"JoyKitInstanceMapping"] mutableCopy];
    
    NSMutableDictionary *name_mappings = [[[NSUserDefaults standardUserDefaults] dictionaryForKey:@"JoyKitNameMapping"] mutableCopy];

    
    if (!instance_mappings) {
        instance_mappings = [[NSMutableDictionary alloc] init];
    }
    
    if (!name_mappings) {
        name_mappings = [[NSMutableDictionary alloc] init];
    }
    
    NSMutableDictionary *mapping = nil;
    if (joystick_configuration_state != 0) {
        mapping = [instance_mappings[controller.uniqueID] mutableCopy];
    }
    else {
        mapping = [[NSMutableDictionary alloc] init];
    }

    
    static const unsigned gb_to_joykit[] = {
    [GBRight]=JOYButtonUsageDPadRight,
    [GBLeft]=JOYButtonUsageDPadLeft,
    [GBUp]=JOYButtonUsageDPadUp,
    [GBDown]=JOYButtonUsageDPadDown,
    [GBA]=JOYButtonUsageA,
    [GBB]=JOYButtonUsageB,
    [GBSelect]=JOYButtonUsageSelect,
    [GBStart]=JOYButtonUsageStart,
    [GBTurbo]=JOYButtonUsageL1,
    [GBRewind]=JOYButtonUsageL2,
    [GBUnderclock]=JOYButtonUsageR1,
    };
    
    if (joystick_configuration_state == GBUnderclock) {
        for (JOYAxis *axis in controller.axes) {
            if (axis.value > 0.5) {
                mapping[@"AnalogUnderclock"] = @(axis.uniqueID);
            }
        }
    }
    
    if (joystick_configuration_state == GBTurbo) {
        for (JOYAxis *axis in controller.axes) {
            if (axis.value > 0.5) {
                mapping[@"AnalogTurbo"] = @(axis.uniqueID);
            }
        }
    }
    
    mapping[n2s(button.uniqueID)] = @(gb_to_joykit[joystick_configuration_state]);
    
    instance_mappings[controller.uniqueID] = mapping;
    name_mappings[controller.deviceName] = mapping;
    [[NSUserDefaults standardUserDefaults] setObject:instance_mappings forKey:@"JoyKitInstanceMapping"];
    [[NSUserDefaults standardUserDefaults] setObject:name_mappings forKey:@"JoyKitNameMapping"];
    [self advanceConfigurationStateMachine];
}

- (NSButton *)analogControlsCheckbox
{
    return _analogControlsCheckbox;
}

- (void)setAnalogControlsCheckbox:(NSButton *)analogControlsCheckbox
{
    _analogControlsCheckbox = analogControlsCheckbox;
    [_analogControlsCheckbox setState: [[NSUserDefaults standardUserDefaults] boolForKey:@"GBAnalogControls"]];
}

- (NSButton *)aspectRatioCheckbox
{
    return _aspectRatioCheckbox;
}

- (void)setAspectRatioCheckbox:(NSButton *)aspectRatioCheckbox
{
    _aspectRatioCheckbox = aspectRatioCheckbox;
    [_aspectRatioCheckbox setState: ![[NSUserDefaults standardUserDefaults] boolForKey:@"GBAspectRatioUnkept"]];
}

- (void)awakeFromNib
{
    [super awakeFromNib];
    [self updateBootROMFolderButton];
    [[NSDistributedNotificationCenter defaultCenter] addObserver:self.controlsTableView selector:@selector(reloadData) name:(NSString*)kTISNotifySelectedKeyboardInputSourceChanged object:nil];
    [JOYController registerListener:self];
    joystick_configuration_state = -1;
}

- (void)dealloc
{
    [JOYController unregisterListener:self];
    [[NSDistributedNotificationCenter defaultCenter] removeObserver:self.controlsTableView];
}

- (IBAction)selectOtherBootROMFolder:(id)sender
{
    NSOpenPanel *panel = [[NSOpenPanel alloc] init];
    [panel setCanChooseDirectories:YES];
    [panel setCanChooseFiles:NO];
    [panel setPrompt:@"Select"];
    [panel setDirectoryURL:[[NSUserDefaults standardUserDefaults] URLForKey:@"GBBootROMsFolder"]];
    [panel beginSheetModalForWindow:self completionHandler:^(NSModalResponse result) {
        if (result == NSModalResponseOK) {
            NSURL *url = [[panel URLs] firstObject];
            [[NSUserDefaults standardUserDefaults] setURL:url forKey:@"GBBootROMsFolder"];
        }
        [self updateBootROMFolderButton];
    }];

}

- (void) updateBootROMFolderButton
{
    NSURL *url = [[NSUserDefaults standardUserDefaults] URLForKey:@"GBBootROMsFolder"];
    BOOL is_dir = false;
    [[NSFileManager defaultManager] fileExistsAtPath:[url path] isDirectory:&is_dir];
    if (!is_dir) url = nil;
    
    if (url) {
        [self.bootROMsFolderItem setTitle:[url lastPathComponent]];
        NSImage *icon = [[NSWorkspace sharedWorkspace] iconForFile:[url path]];
        [icon setSize:NSMakeSize(16, 16)];
        [self.bootROMsFolderItem setHidden:NO];
        [self.bootROMsFolderItem setImage:icon];
        [self.bootROMsButton selectItemAtIndex:1];
    }
    else {
        [self.bootROMsFolderItem setHidden:YES];
        [self.bootROMsButton selectItemAtIndex:0];
    }
}

- (IBAction)useBuiltinBootROMs:(id)sender
{
    [[NSUserDefaults standardUserDefaults] setURL:nil forKey:@"GBBootROMsFolder"];
    [self updateBootROMFolderButton];
}

- (void)setDmgPopupButton:(NSPopUpButton *)dmgPopupButton
{
    _dmgPopupButton = dmgPopupButton;
    [_dmgPopupButton selectItemWithTag:[[NSUserDefaults standardUserDefaults] integerForKey:@"GBDMGModel"]];
}

- (NSPopUpButton *)dmgPopupButton
{
    return _dmgPopupButton;
}

- (void)setSgbPopupButton:(NSPopUpButton *)sgbPopupButton
{
    _sgbPopupButton = sgbPopupButton;
    [_sgbPopupButton selectItemWithTag:[[NSUserDefaults standardUserDefaults] integerForKey:@"GBSGBModel"]];
}

- (NSPopUpButton *)sgbPopupButton
{
    return _sgbPopupButton;
}

- (void)setCgbPopupButton:(NSPopUpButton *)cgbPopupButton
{
    _cgbPopupButton = cgbPopupButton;
    [_cgbPopupButton selectItemWithTag:[[NSUserDefaults standardUserDefaults] integerForKey:@"GBCGBModel"]];
}

- (NSPopUpButton *)cgbPopupButton
{
    return _cgbPopupButton;
}

- (IBAction)dmgModelChanged:(id)sender
{
    [[NSUserDefaults standardUserDefaults] setObject:@([sender selectedTag])
                                              forKey:@"GBDMGModel"];
    [[NSNotificationCenter defaultCenter] postNotificationName:@"GBDMGModelChanged" object:nil];

}

- (IBAction)sgbModelChanged:(id)sender
{
    [[NSUserDefaults standardUserDefaults] setObject:@([sender selectedTag])
                                              forKey:@"GBSGBModel"];
    [[NSNotificationCenter defaultCenter] postNotificationName:@"GBSGBModelChanged" object:nil];
}

- (IBAction)cgbModelChanged:(id)sender
{
    [[NSUserDefaults standardUserDefaults] setObject:@([sender selectedTag])
                                              forKey:@"GBCGBModel"];
    [[NSNotificationCenter defaultCenter] postNotificationName:@"GBCGBModelChanged" object:nil];
}

- (IBAction)reloadButtonsData:(id)sender
{
    [self.controlsTableView reloadData];
}

- (void)setPreferredJoypadButton:(NSPopUpButton *)preferredJoypadButton
{
    _preferredJoypadButton = preferredJoypadButton;
    [self refreshJoypadMenu:nil];
}

- (NSPopUpButton *)preferredJoypadButton
{
    return _preferredJoypadButton;
}

- (void)controllerConnected:(JOYController *)controller
{
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(0.25 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
        [self refreshJoypadMenu:nil];
    });
}

- (void)controllerDisconnected:(JOYController *)controller
{
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(0.25 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
        [self refreshJoypadMenu:nil];
    });
}

- (IBAction)refreshJoypadMenu:(id)sender
{
    bool preferred_is_connected = false;
    NSString *player_string = n2s(self.playerListButton.selectedTag);
    NSString *selected_controller = [[NSUserDefaults standardUserDefaults] dictionaryForKey:@"JoyKitDefaultControllers"][player_string];
    
    [self.preferredJoypadButton removeAllItems];
    [self.preferredJoypadButton addItemWithTitle:@"None"];
    for (JOYController *controller in [JOYController allControllers]) {
        [self.preferredJoypadButton addItemWithTitle:[NSString stringWithFormat:@"%@ (%@)", controller.deviceName, controller.uniqueID]];
        
        self.preferredJoypadButton.lastItem.identifier = controller.uniqueID;
        
        if ([controller.uniqueID isEqualToString:selected_controller]) {
            preferred_is_connected = true;
            [self.preferredJoypadButton selectItem:self.preferredJoypadButton.lastItem];
        }
    }
    
    if (!preferred_is_connected && selected_controller) {
        [self.preferredJoypadButton addItemWithTitle:[NSString stringWithFormat:@"Unavailable Controller (%@)", selected_controller]];
        self.preferredJoypadButton.lastItem.identifier = selected_controller;
        [self.preferredJoypadButton selectItem:self.preferredJoypadButton.lastItem];
    }
    

    if (!selected_controller) {
        [self.preferredJoypadButton selectItemWithTitle:@"None"];
    }
    [self.controlsTableView reloadData];
}

- (IBAction)changeDefaultJoypad:(id)sender
{
    NSMutableDictionary *default_joypads = [[[NSUserDefaults standardUserDefaults] dictionaryForKey:@"JoyKitDefaultControllers"] mutableCopy];
    if (!default_joypads) {
        default_joypads = [[NSMutableDictionary alloc] init];
    }

    NSString *player_string = n2s(self.playerListButton.selectedTag);
    if ([[sender titleOfSelectedItem] isEqualToString:@"None"]) {
        [default_joypads removeObjectForKey:player_string];
    }
    else {
        default_joypads[player_string] = [[sender selectedItem] identifier];
    }
    [[NSUserDefaults standardUserDefaults] setObject:default_joypads forKey:@"JoyKitDefaultControllers"];
}
@end
