#import "GBCheatWindowController.h"
#import "GBWarningPopover.h"
#import "GBCheatTextFieldCell.h"

@implementation GBCheatWindowController

+ (NSString *)addressStringFromCheat:(const GB_cheat_t *)cheat
{
    if (cheat->bank != GB_CHEAT_ANY_BANK) {
        return [NSString stringWithFormat:@"$%x:$%04x", cheat->bank, cheat->address];
    }
    return [NSString stringWithFormat:@"$%04x", cheat->address];
}

+ (NSString *)actionDescriptionForCheat:(const GB_cheat_t *)cheat
{
    if (cheat->use_old_value) {
        return [NSString stringWithFormat:@"[%@]($%02x) = $%02x", [self addressStringFromCheat:cheat], cheat->old_value, cheat->value];
    }
    return [NSString stringWithFormat:@"[%@] = $%02x", [self addressStringFromCheat:cheat], cheat->value];
}

- (NSInteger)numberOfRowsInTableView:(NSTableView *)tableView
{
    GB_gameboy_t *gb = self.document.gameboy;
    if (!gb) return 0;
    size_t cheatCount;
    GB_get_cheats(gb, &cheatCount);
    return cheatCount + 1;
}

- (NSCell *)tableView:(NSTableView *)tableView dataCellForTableColumn:(NSTableColumn *)tableColumn row:(NSInteger)row
{
    GB_gameboy_t *gb = self.document.gameboy;
    if (!gb) return nil;
    size_t cheatCount;
    GB_get_cheats(gb, &cheatCount);
    NSUInteger columnIndex = [[tableView tableColumns] indexOfObject:tableColumn];
    if (row >= cheatCount && columnIndex == 0) {
        return [[NSCell alloc] init];
    }
    return nil;
}

- (nullable id)tableView:(NSTableView *)tableView objectValueForTableColumn:(nullable NSTableColumn *)tableColumn row:(NSInteger)row
{
    size_t cheatCount;
    GB_gameboy_t *gb = self.document.gameboy;
    if (!gb) return nil;
    const GB_cheat_t *const *cheats = GB_get_cheats(gb, &cheatCount);
    NSUInteger columnIndex = [[tableView tableColumns] indexOfObject:tableColumn];
    if (row >= cheatCount) {
        switch (columnIndex) {
            case 0:
                return @(YES);
                
            case 1:
                return @NO;
                
            case 2:
                return @"Add Cheat...";
            
            case 3:
                return @"";
        }
    }
    
    switch (columnIndex) {
        case 0:
            return @(NO);
            
        case 1:
            return @(cheats[row]->enabled);
            
        case 2:
            return @(cheats[row]->description);
            
        case 3:
            return [GBCheatWindowController actionDescriptionForCheat:cheats[row]];
    }
    
    return nil;
}

- (IBAction)importCheat:(id)sender
{
    GB_gameboy_t *gb = self.document.gameboy;
    if (!gb) return;

    [self.document performAtomicBlock:^{
        if (GB_import_cheat(gb,
                            self.importCodeField.stringValue.UTF8String,
                            self.importDescriptionField.stringValue.UTF8String,
                            true)) {
            self.importCodeField.stringValue = @"";
            self.importDescriptionField.stringValue = @"";
            [self.cheatsTable reloadData];
            [self tableViewSelectionDidChange:nil];
        }
        else {
            NSBeep();
            [GBWarningPopover popoverWithContents:@"This code is not a valid GameShark or GameGenie code" onView:self.importCodeField];
        }
    }];
}

- (void)tableView:(NSTableView *)tableView setObjectValue:(id)object forTableColumn:(NSTableColumn *)tableColumn row:(NSInteger)row
{
    GB_gameboy_t *gb = self.document.gameboy;
    if (!gb) return;
    size_t cheatCount;
    const GB_cheat_t *const *cheats = GB_get_cheats(gb, &cheatCount);
    NSUInteger columnIndex = [[tableView tableColumns] indexOfObject:tableColumn];
    [self.document performAtomicBlock:^{
        if (columnIndex == 1) {
            if (row >= cheatCount) {
                GB_add_cheat(gb, "New Cheat", 0, 0, 0, 0, false, true);
            }
            else {
                GB_update_cheat(gb, cheats[row], cheats[row]->description, cheats[row]->address, cheats[row]->bank, cheats[row]->value, cheats[row]->old_value, cheats[row]->use_old_value, !cheats[row]->enabled);
            }
        }
        else if (row < cheatCount) {
            GB_remove_cheat(gb, cheats[row]);
        }
    }];
    [self.cheatsTable reloadData];
    [self tableViewSelectionDidChange:nil];
}

- (void)tableViewSelectionDidChange:(NSNotification *)notification
{
    GB_gameboy_t *gb = self.document.gameboy;
    if (!gb) return;

    size_t cheatCount;
    const GB_cheat_t *const *cheats = GB_get_cheats(gb, &cheatCount);
    unsigned row = self.cheatsTable.selectedRow;
    const GB_cheat_t *cheat = NULL;
    if (row >= cheatCount) {
        static const GB_cheat_t template = {
            .address = 0,
            .bank = 0,
            .value = 0,
            .old_value = 0,
            .use_old_value = false,
            .enabled = false,
            .description = "New Cheat",
        };
        cheat = &template;
    }
    else {
        cheat = cheats[row];
    }
    
    self.addressField.stringValue = [GBCheatWindowController addressStringFromCheat:cheat];
    self.valueField.stringValue = [NSString stringWithFormat:@"$%02x", cheat->value];
    self.oldValueField.stringValue = [NSString stringWithFormat:@"$%02x", cheat->old_value];
    self.oldValueCheckbox.state = cheat->use_old_value;
    self.descriptionField.stringValue = @(cheat->description);
}

- (void)awakeFromNib
{
    [self tableViewSelectionDidChange:nil];
    ((GBCheatTextFieldCell *)self.addressField.cell).usesAddressFormat = true;
}

- (void)controlTextDidChange:(NSNotification *)obj
{
    [self updateCheat:nil];
}

- (IBAction)updateCheat:(id)sender
{
    GB_gameboy_t *gb = self.document.gameboy;
    if (!gb) return;

    uint16_t address = 0;
    uint16_t bank = GB_CHEAT_ANY_BANK;
    if ([self.addressField.stringValue rangeOfString:@":"].location != NSNotFound) {
        sscanf(self.addressField.stringValue.UTF8String, "$%hx:$%hx", &bank, &address);
    }
    else {
        sscanf(self.addressField.stringValue.UTF8String, "$%hx", &address);
    }
    
    uint8_t value = 0;
    if ([self.valueField.stringValue characterAtIndex:0] == '$') {
        sscanf(self.valueField.stringValue.UTF8String, "$%02hhx", &value);
    }
    else {
        sscanf(self.valueField.stringValue.UTF8String, "%hhd", &value);
    }
    
    uint8_t oldValue = 0;
    if ([self.oldValueField.stringValue characterAtIndex:0] == '$') {
        sscanf(self.oldValueField.stringValue.UTF8String, "$%02hhx", &oldValue);
    }
    else {
        sscanf(self.oldValueField.stringValue.UTF8String, "%hhd", &oldValue);
    }
    
    size_t cheatCount;
    const GB_cheat_t *const *cheats = GB_get_cheats(gb, &cheatCount);
    unsigned row = self.cheatsTable.selectedRow;
    
    [self.document performAtomicBlock:^{
        if (row >= cheatCount) {
            GB_add_cheat(gb,
                         self.descriptionField.stringValue.UTF8String,
                         address,
                         bank,
                         value,
                         oldValue,
                         self.oldValueCheckbox.state,
                         false);
        }
        else {
            GB_update_cheat(gb,
                            cheats[row],
                            self.descriptionField.stringValue.UTF8String,
                            address,
                            bank,
                            value,
                            oldValue,
                            self.oldValueCheckbox.state,
                            cheats[row]->enabled);
        }
    }];
    [self.cheatsTable reloadData];
}

- (void)cheatsUpdated
{
    [self.cheatsTable reloadData];
    [self tableViewSelectionDidChange:nil];
}

@end
