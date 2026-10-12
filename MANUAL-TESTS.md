# Manual release checklist

Mark each item with OS, app version, browser version if applicable, and observed result. All items below are **pending** until tested on a real desktop.

- [ ] Portable EXE opens a visible, draggable button and tray icon; a second instance does not open a duplicate.
- [ ] Clicking the floating button does not steal the target's input focus.
- [ ] Capture a region in Codex with the composer focused; a preview attachment appears, text draft stays intact, and nothing is sent.
- [ ] Repeat in a web chat in Chrome and Edge; check the attachment, not just the clipboard.
- [ ] Repeat in a second desktop agent with image support.
- [ ] Two browser windows/tabs and two agent windows: return to the original input, never another conversation.
- [ ] Non-editable focus: screenshot saved, no automatic paste.
- [ ] Close the target while selecting: saved screenshot, no paste into a different app.
- [ ] Esc/right-click cancels without changing clipboard or saving a screenshot.
- [ ] Tiny region is rejected; reverse-direction drags work.
- [ ] Multi-monitor, negative coordinates, 100% / 150% / 200% and mixed DPI: crop matches selection.
- [ ] Button and selection hint do not appear in the captured crop.
- [ ] Busy clipboard and unsupported paste: clear fallback; PNG opens correctly.
- [ ] Tray auto-paste toggle, copy last image and open folder work.
- [ ] Ctrl+Alt+S works; occupied shortcut reports fallback; exit releases the shortcut.
- [ ] Installer works for a standard user, with a space/non-ASCII name in the path.
- [ ] Startup is off by default; opt-in startup creates the expected shortcut.
- [ ] Uninstall removes app and shortcuts, preserving captures by default.
