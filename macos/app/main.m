#import <Cocoa/Cocoa.h>
#import <ApplicationServices/ApplicationServices.h>
#import "PetView.h"
#import "MacKeys.h"
#import <math.h>

@interface KCPetPanel : NSPanel
@end
@implementation KCPetPanel
- (BOOL)canBecomeKeyWindow { return NO; }
- (BOOL)canBecomeMainWindow { return NO; }
@end

@interface KCController : NSObject <NSApplicationDelegate,NSMenuDelegate,NSWindowDelegate>
- (void)receiveType:(CGEventType)type event:(CGEventRef)event;
- (void)reenableTap;
@end

static CGEventRef Monitor(CGEventTapProxy proxy, CGEventType type, CGEventRef event, void *context) {
    KCController *controller=(__bridge KCController *)context;
    if(type==kCGEventTapDisabledByTimeout || type==kCGEventTapDisabledByUserInput)
        [controller reenableTap];
    else if(event) [controller receiveType:type event:event];
    return event;
}
static double Now(void) { return NSProcessInfo.processInfo.systemUptime; }
static NSInteger StyleDefault(NSString *key, NSInteger fallback, NSInteger max) {
    NSInteger value=[NSUserDefaults.standardUserDefaults integerForKey:key];
    return value>=0&&value<=max?value:fallback;
}
static NSArray<NSNumber *> *Sizes(void) { return @[@.5,@.75,@1.0,@1.25,@1.5]; }

@implementation KCController {
    KCPetPanel *_window;
    KCPetView *_pet;
    NSStatusItem *_status;
    NSMenu *_menu;
    NSTimer *_timer;
    CFMachPortRef _tap;
    CFRunLoopSourceRef _source;
    double _lastConsentCheck, _scale;
    BOOL _alwaysOnTop, _through, _menuOpen, _permission;
    NSPanel *_cursorOverlay;
    NSImageView *_cursorView;
    NSMutableSet<NSNumber *> *_hiddenDisplays;
    BOOL _cursorOn;
    NSInteger _cursorStyle;
}
- (void)applicationDidFinishLaunching:(NSNotification *)notification {
    NSUserDefaults *defaults=NSUserDefaults.standardUserDefaults;
    [defaults registerDefaults:@{@"cat":@0,@"keyboard":@1,@"mouse":@3,@"labels":@YES,@"alwaysOnTop":@YES,
                                 @"mouseThrough":@NO,@"size":@1.0,@"cursorEnabled":@NO,@"cursorStyle":@0}];
    _scale=[defaults doubleForKey:@"size"];if(![Sizes() containsObject:@(_scale)])_scale=1;
    _through=[defaults boolForKey:@"mouseThrough"];_alwaysOnTop=[defaults boolForKey:@"alwaysOnTop"];
    NSRect frame=NSMakeRect(0,0,306*_scale,298*_scale);
    _window=[[KCPetPanel alloc] initWithContentRect:frame styleMask:NSWindowStyleMaskBorderless|NSWindowStyleMaskNonactivatingPanel
                                    backing:NSBackingStoreBuffered defer:NO];
    _window.title=@"键盘喵";_window.opaque=NO;_window.backgroundColor=NSColor.clearColor;
    _window.hasShadow=NO;_window.hidesOnDeactivate=NO;_window.releasedWhenClosed=NO;
    _window.level=_alwaysOnTop?NSFloatingWindowLevel:NSNormalWindowLevel;
    _window.collectionBehavior=NSWindowCollectionBehaviorCanJoinAllSpaces|NSWindowCollectionBehaviorFullScreenAuxiliary|
                               NSWindowCollectionBehaviorIgnoresCycle;
    _window.ignoresMouseEvents=_through;
    _pet=[[KCPetView alloc] initWithFrame:frame];_pet.catStyle=StyleDefault(@"cat",0,1);_pet.keyboardStyle=StyleDefault(@"keyboard",1,3);
    _pet.mouseStyle=StyleDefault(@"mouse",3,3);_cursorOn=[defaults boolForKey:@"cursorEnabled"];_cursorStyle=StyleDefault(@"cursorStyle",0,1);_pet.showsLabels=[defaults boolForKey:@"labels"];
    _pet.autoresizingMask=NSViewWidthSizable|NSViewHeightSizable;_window.contentView=_pet;
    _status=[NSStatusBar.systemStatusBar statusItemWithLength:NSSquareStatusItemLength];
    NSImage *icon=[NSImage imageWithSystemSymbolName:@"pawprint.fill" accessibilityDescription:@"键盘喵"];
    if(icon){icon.template=YES;_status.button.image=icon;} else _status.button.title=@"猫";
    _status.button.toolTip=@"键盘喵 · 设置";
    _menu=[[NSMenu alloc] initWithTitle:@"键盘喵"];_menu.autoenablesItems=NO;_menu.delegate=self;
    _status.menu=_menu;_pet.menu=_menu;
    NSString *origin=[defaults stringForKey:@"origin"];BOOL restored=NO;
    if(origin){NSPoint p=NSPointFromString(origin);if(isfinite(p.x)&&isfinite(p.y)){[_window setFrameOrigin:p];restored=[self windowOnScreen];}}
    if(!restored)[self dockToCorner];
    _window.delegate=self;[_window orderFrontRegardless];
    [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(displayChanged:) name:NSApplicationDidChangeScreenParametersNotification object:nil];
    [self refreshTap];[self rebuildMenu];[self applyCursor];
    _timer=[NSTimer timerWithTimeInterval:1./40. target:self selector:@selector(tick:) userInfo:nil repeats:YES];
    _timer.tolerance=.004;[[NSRunLoop mainRunLoop] addTimer:_timer forMode:NSRunLoopCommonModes];
    if(!_pet.hasArt){
        NSAlert *alert=[NSAlert new];alert.messageText=@"桌宠图像资源缺失";
        alert.informativeText=@"请从安装包重新安装完整的“键盘喵”应用。";[alert runModal];[NSApp terminate:nil];return;
    }
    if(!_permission && ![defaults boolForKey:@"explainedInput"]){
        [defaults setBool:YES forKey:@"explainedInput"];
        dispatch_async(dispatch_get_main_queue(),^{
            NSAlert *alert=[NSAlert new];alert.messageText=@"让小猫跟着你打字";
            alert.informativeText=@"允许“输入监控”后，小猫能在你使用其他应用时敲键盘。桌宠只使用物理按键编号驱动动画，按键名称短暂显示在本机。你可以随时在菜单栏的猫爪中关闭键帽显示。";
            [alert addButtonWithTitle:@"前往授权"];[alert addButtonWithTitle:@"稍后"];
            if([alert runModal]==NSAlertFirstButtonReturn)[self permissionAction:nil];
        });
    }
}
- (BOOL)windowOnScreen {
    NSPoint center=NSMakePoint(NSMidX(_window.frame),NSMidY(_window.frame));
    for(NSScreen *s in NSScreen.screens)if(NSPointInRect(center,s.visibleFrame))return YES;
    return NO;
}
- (void)dockToCorner {
    NSScreen *screen=_window.screen?:NSScreen.mainScreen?:NSScreen.screens.firstObject;
    if(!screen)return;NSRect area=screen.visibleFrame;
    [_window setFrameOrigin:NSMakePoint(NSMaxX(area)-_window.frame.size.width-16,NSMinY(area)+12)];
}
- (void)saveOrigin {
    if(!_window || ![self windowOnScreen])return;
    [NSUserDefaults.standardUserDefaults setObject:NSStringFromPoint(_window.frame.origin) forKey:@"origin"];
}
- (void)windowDidMove:(NSNotification *)n {
    [NSObject cancelPreviousPerformRequestsWithTarget:self selector:@selector(saveOrigin) object:nil];
    [self performSelector:@selector(saveOrigin) withObject:nil afterDelay:.25];
}
- (void)displayChanged:(NSNotification *)n {
    if(![self windowOnScreen]){[self dockToCorner];[self saveOrigin];}
    if(_cursorOn&&_window.visible)[self hideCursor:YES];
}
- (void)disconnectTap {
    if(_source){CFRunLoopRemoveSource(CFRunLoopGetMain(),_source,kCFRunLoopCommonModes);CFRelease(_source);_source=NULL;}
    if(_tap){CFMachPortInvalidate(_tap);CFRelease(_tap);_tap=NULL;}
}
- (void)reenableTap { if(_tap&&_permission&&_window.visible)CGEventTapEnable(_tap,true); }
- (void)refreshTap {
    _lastConsentCheck=Now();_permission=CGPreflightListenEventAccess();
    if(!_permission||!_window.visible){[self disconnectTap];return;}
    if(_tap&&CFMachPortIsValid(_tap)){if(!CGEventTapIsEnabled(_tap))[self reenableTap];return;}
    [self disconnectTap];
    CGEventMask mask=CGEventMaskBit(kCGEventKeyDown)|CGEventMaskBit(kCGEventFlagsChanged)|
        CGEventMaskBit(kCGEventLeftMouseDown)|CGEventMaskBit(kCGEventLeftMouseUp)|CGEventMaskBit(kCGEventRightMouseDown)|CGEventMaskBit(kCGEventRightMouseUp);
    _tap=CGEventTapCreate(kCGSessionEventTap,kCGHeadInsertEventTap,kCGEventTapOptionListenOnly,mask,Monitor,(__bridge void *)self);
    if(!_tap)return;
    _source=CFMachPortCreateRunLoopSource(kCFAllocatorDefault,_tap,0);
    if(!_source){[self disconnectTap];return;}
    CFRunLoopAddSource(CFRunLoopGetMain(),_source,kCFRunLoopCommonModes);CGEventTapEnable(_tap,true);
}
- (void)receiveType:(CGEventType)type event:(CGEventRef)event {
    if(!_window.visible||_menuOpen||NSApp.modalWindow)return;
    double t=Now();
    if(type==kCGEventKeyDown||type==kCGEventFlagsChanged){
        CGKeyCode code=(CGKeyCode)CGEventGetIntegerValueField(event,kCGKeyboardEventKeycode);
        if(type==kCGEventFlagsChanged && code!=57 && !CGEventSourceKeyState(kCGEventSourceStateCombinedSessionState,code))return;
        NSInteger vk=KCArtworkKey(code);[_pet strike:vk label:KCKeyLabel(vk) at:t];return;
    }
    if(type==kCGEventLeftMouseDown||type==kCGEventRightMouseDown||type==kCGEventLeftMouseUp||type==kCGEventRightMouseUp){
        NSInteger side=CGEventGetIntegerValueField(event,kCGMouseEventButtonNumber);
        [_pet button:side down:(type==kCGEventLeftMouseDown||type==kCGEventRightMouseDown) at:t];
    }
}
- (void)tick:(NSTimer *)timer {
    if(_cursorOn&&_cursorOverlay){
        NSPoint p=NSEvent.mouseLocation;
        NSPoint hot=[_pet cursorHotspot:_cursorStyle];
        NSSize size=_cursorOverlay.frame.size;
        [_cursorOverlay setFrameOrigin:NSMakePoint(p.x-hot.x,p.y-(size.height-hot.y))];
        if(!_cursorOverlay.isVisible)[_cursorOverlay orderFrontRegardless];
    }
    if(!_window.visible)return;double t=Now();
    if(t-_lastConsentCheck>3)[self refreshTap];
    NSPoint p=NSEvent.mouseLocation;
    NSPoint windowPoint=[_window convertPointFromScreen:p];NSPoint local=[_pet convertPoint:windowPoint fromView:nil];
    local=NSMakePoint(75+local.x/_scale,9+local.y/_scale);
    NSScreen *display=_window.screen?:NSScreen.mainScreen;
    for(NSScreen *s in NSScreen.screens)if(NSPointInRect(p,s.frame)){display=s;break;}
    NSRect f=display.frame;NSPoint norm=NSZeroPoint;
    if(f.size.width>0&&f.size.height>0)norm=NSMakePoint(2*(p.x-NSMinX(f))/f.size.width-1,1-2*(p.y-NSMinY(f))/f.size.height);
    NSUInteger buttons=NSEvent.pressedMouseButtons;
    if(!_menuOpen&&!NSApp.modalWindow)[_pet samplePointer:local onScreen:norm left:(buttons&1)!=0 right:(buttons&2)!=0 at:t];
    [_pet advance:t];
}
- (void)permissionAction:(id)sender {
    if(!CGPreflightListenEventAccess())CGRequestListenEventAccess();
    [self refreshTap];
    if(!_permission){
        NSURL *url=[NSURL URLWithString:@"x-apple.systempreferences:com.apple.preference.security?Privacy_ListenEvent"];
        [[NSWorkspace sharedWorkspace] openURL:url];
    }else if(!_tap&&_window.visible){
        NSAlert *alert=[NSAlert new];alert.messageText=@"请重新打开键盘喵";
        alert.informativeText=@"若系统已允许输入监控，请退出并重开应用，让授权生效。";[alert runModal];
    }
}
- (NSMenuItem *)entry:(NSString *)title action:(SEL)action menu:(NSMenu *)menu {
    NSMenuItem *item=[[NSMenuItem alloc] initWithTitle:title action:action keyEquivalent:@""];
    item.target=self;[menu addItem:item];return item;
}
- (NSMenuItem *)styleGroup:(NSString *)title choices:(NSArray<NSString *> *)choices current:(NSInteger)current
             action:(SEL)action resource:(NSString *)format {
    NSMenu *sub=[[NSMenu alloc] initWithTitle:title];
    NSMenuItem *group=[self entry:title action:NULL menu:_menu];group.submenu=sub;
    [choices enumerateObjectsUsingBlock:^(NSString *name,NSUInteger i,BOOL *stop){
        NSMenuItem *item=[self entry:name action:action menu:sub];item.tag=(NSInteger)i;
        item.state=current==(NSInteger)i?NSControlStateValueOn:NSControlStateValueOff;
        NSString *key=[NSString stringWithFormat:format,(long)i];
        item.image=[self->_pet thumbnail:key maxWidth:action==@selector(keyboardAction:)?75:30];
    }];
    return group;
}
- (void)rebuildMenu {
    [_menu removeAllItems];
    [self styleGroup:@"猫咪样式" choices:@[@"黑猫",@"奶牛猫"] current:_pet.catStyle action:@selector(catAction:) resource:@"portrait-%ld"];
    [self styleGroup:@"键盘样式" choices:@[@"经典奶油",@"薄荷打字机",@"冰蓝电竞",@"胡桃木"] current:_pet.keyboardStyle action:@selector(keyboardAction:) resource:@"keyboard-%ld"];
    [self styleGroup:@"鼠标样式" choices:@[@"奶油滚轮",@"雾紫鹅卵石",@"冰蓝电竞",@"酒红轨迹球"] current:_pet.mouseStyle action:@selector(mouseAction:) resource:@"mouse-%ld-normal"];
    NSMenuItem *cursorGroup=[self styleGroup:@"光标样式" choices:@[@"小鸟",@"逗猫棒"] current:_cursorStyle action:@selector(cursorStyleAction:) resource:@"cursor-%ld"];
    for(NSMenuItem *item in cursorGroup.submenu.itemArray)item.enabled=_cursorOn;
    NSMenu *sizes=[[NSMenu alloc] initWithTitle:@"小猫大小"];[self entry:@"小猫大小" action:NULL menu:_menu].submenu=sizes;
    for(NSNumber *size in Sizes()){
        NSMenuItem *item=[self entry:[NSString stringWithFormat:@"%ld%%",lround(size.doubleValue*100)] action:@selector(sizeAction:) menu:sizes];
        item.representedObject=size;item.state=(fabs(_scale-size.doubleValue)<.001)?NSControlStateValueOn:NSControlStateValueOff;
    }
    [_menu addItem:NSMenuItem.separatorItem];
    [self entry:@"显示浮动键帽" action:@selector(labelsAction:) menu:_menu].state=_pet.showsLabels?NSControlStateValueOn:NSControlStateValueOff;
    [self entry:@"启用光标样式" action:@selector(cursorEnabledAction:) menu:_menu].state=_cursorOn?NSControlStateValueOn:NSControlStateValueOff;
    [self entry:@"保持置顶" action:@selector(topAction:) menu:_menu].state=_alwaysOnTop?NSControlStateValueOn:NSControlStateValueOff;
    [self entry:@"鼠标穿透" action:@selector(throughAction:) menu:_menu].state=_through?NSControlStateValueOn:NSControlStateValueOff;
    if(!_permission)[self entry:@"开启键盘联动…" action:@selector(permissionAction:) menu:_menu];
    else if(!_tap&&_window.visible)[self entry:@"重新连接键盘…" action:@selector(permissionAction:) menu:_menu];
    [_menu addItem:NSMenuItem.separatorItem];
    [self entry:@"回到屏幕右下角" action:@selector(cornerAction:) menu:_menu];
    [self entry:_window.visible?@"暂时隐藏":@"显示小猫" action:@selector(toggleVisible:) menu:_menu];
    [self entry:@"退出键盘喵" action:@selector(quit:) menu:_menu];
}
- (void)menuNeedsUpdate:(NSMenu *)menu { if(menu==_menu)[self rebuildMenu]; }
- (void)menuWillOpen:(NSMenu *)menu { _menuOpen=YES; }
- (void)menuDidClose:(NSMenu *)menu { _menuOpen=NO; }
- (void)catAction:(NSMenuItem *)item { _pet.catStyle=item.tag;[NSUserDefaults.standardUserDefaults setInteger:item.tag forKey:@"cat"];_pet.needsDisplay=YES; }
- (void)keyboardAction:(NSMenuItem *)item { _pet.keyboardStyle=item.tag;[NSUserDefaults.standardUserDefaults setInteger:item.tag forKey:@"keyboard"];_pet.needsDisplay=YES; }
- (void)mouseAction:(NSMenuItem *)item { _pet.mouseStyle=item.tag;[NSUserDefaults.standardUserDefaults setInteger:item.tag forKey:@"mouse"];_pet.needsDisplay=YES; }
- (void)cursorStyleAction:(NSMenuItem *)item { _cursorStyle=item.tag;[NSUserDefaults.standardUserDefaults setInteger:_cursorStyle forKey:@"cursorStyle"];[self applyCursor]; }
- (void)cursorEnabledAction:(id)sender { _cursorOn=!_cursorOn;[NSUserDefaults.standardUserDefaults setBool:_cursorOn forKey:@"cursorEnabled"];[self applyCursor]; }
- (void)sizeAction:(NSMenuItem *)item {
    NSRect old=_window.frame;_scale=[item.representedObject doubleValue];
    [_window setFrame:NSMakeRect(NSMaxX(old)-306*_scale,NSMinY(old),306*_scale,298*_scale) display:YES];
    if(![self windowOnScreen])[self dockToCorner];
    [NSUserDefaults.standardUserDefaults setDouble:_scale forKey:@"size"];[self saveOrigin];
}
- (void)labelsAction:(id)sender { _pet.showsLabels=!_pet.showsLabels;[NSUserDefaults.standardUserDefaults setBool:_pet.showsLabels forKey:@"labels"]; }
- (void)topAction:(id)sender { _alwaysOnTop=!_alwaysOnTop;_window.level=_alwaysOnTop?NSFloatingWindowLevel:NSNormalWindowLevel;
    [NSUserDefaults.standardUserDefaults setBool:_alwaysOnTop forKey:@"alwaysOnTop"]; }
- (void)throughAction:(id)sender { _through=!_through;_window.ignoresMouseEvents=_through;[NSUserDefaults.standardUserDefaults setBool:_through forKey:@"mouseThrough"]; }
- (void)showPet {
    if(![self windowOnScreen])[self dockToCorner];[_window orderFrontRegardless];[self refreshTap];[self applyCursor];
}
- (void)cornerAction:(id)sender { [self dockToCorner];[self saveOrigin];[self showPet]; }
- (void)toggleVisible:(id)sender {
    if(_window.visible){[_window orderOut:nil];[_pet clearInput];[self disconnectTap];[self applyCursor];}else [self showPet];
}
- (BOOL)applicationShouldHandleReopen:(NSApplication *)app hasVisibleWindows:(BOOL)flag { [self showPet];return NO; }
- (void)quit:(id)sender { [NSApp terminate:nil]; }
- (void)applicationWillTerminate:(NSNotification *)n { [_timer invalidate];[self saveOrigin];[self disconnectTap];[self hideCursor:NO]; }
- (void)hideCursor:(BOOL)hide {
    // CGDisplayHideCursor is reference-counted per display; track each display we
    // hid so the counts stay balanced across apply cycles and screen changes.
    if(!_hiddenDisplays)_hiddenDisplays=[NSMutableSet set];
    for(NSScreen *s in NSScreen.screens){
        CGDirectDisplayID display=[[[s deviceDescription] objectForKey:@"NSScreenNumber"] unsignedIntValue];
        if(hide&&![_hiddenDisplays containsObject:@(display)]){CGDisplayHideCursor(display);[_hiddenDisplays addObject:@(display)];}
        else if(!hide&&[_hiddenDisplays containsObject:@(display)]){CGDisplayShowCursor(display);[_hiddenDisplays removeObject:@(display)];}
    }
}
- (void)applyCursor {
    BOOL on=_cursorOn&&_window.visible;
    if(on&&!_cursorOverlay){
        _cursorOverlay=[[NSPanel alloc] initWithContentRect:NSMakeRect(0,0,32,32) styleMask:NSWindowStyleMaskBorderless backing:NSBackingStoreBuffered defer:NO];
        _cursorOverlay.level=NSStatusWindowLevel;_cursorOverlay.opaque=NO;_cursorOverlay.backgroundColor=NSColor.clearColor;
        _cursorOverlay.hasShadow=NO;_cursorOverlay.releasedWhenClosed=NO;_cursorOverlay.ignoresMouseEvents=YES;
        _cursorOverlay.collectionBehavior=NSWindowCollectionBehaviorCanJoinAllSpaces|NSWindowCollectionBehaviorFullScreenAuxiliary|
                                           NSWindowCollectionBehaviorIgnoresCycle;
        _cursorView=[[NSImageView alloc] initWithFrame:NSMakeRect(0,0,32,32)];
        _cursorView.autoresizingMask=NSViewWidthSizable|NSViewHeightSizable;
        _cursorOverlay.contentView=_cursorView;
    }
    if(!_cursorOverlay)return;
    if(on){
        [_cursorOverlay setContentSize:[_pet cursorSize:_cursorStyle]];
        _cursorView.image=[_pet cursorImage:_cursorStyle];
    } else [_cursorOverlay orderOut:nil];
    [self hideCursor:on];
}
@end

int main(int argc,const char *argv[]) {
    @autoreleasepool {
        NSApplication *app=NSApplication.sharedApplication;[app setActivationPolicy:NSApplicationActivationPolicyAccessory];
        __attribute__((objc_precise_lifetime)) KCController *controller=[KCController new];
        app.delegate=controller;[app run];
    }
    return 0;
}


