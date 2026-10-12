# Agent Chat Screenshot Button

A floating screenshot button for desktop agents and web chat apps. Select a screen region, return to your original input, and paste the image without sending the message.

通用 agent 聊天截图按钮：先点击聊天输入框，再点击悬浮按钮框选截图，尝试自动粘贴到原输入框，不自动发送。

## Install / 安装

Windows 10/11 x64, .NET Framework 4.8. No Node.js, Python, administrator rights, API keys, or paid service required.

1. Download `AgentChatScreenshotButton-windows-x64.zip` from this repository's **Releases**.
2. Extract the ZIP (do not run inside the ZIP).
3. Double-click **Install.cmd**. The app starts and a Start menu shortcut is created.

For portable use, run `AgentChatScreenshotButton.exe` directly. To enable startup, run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\install.ps1 -Startup` from the extracted directory. Startup is opt-in.

下载 Release ZIP → 解压 → 双击 **Install.cmd**，即可安装并启动。也可以直接运行 EXE，免安装使用。

This early release is unsigned. Windows may display a publisher warning. Verify the release SHA-256 file and inspect the source before running. No system-wide execution policy is changed by the installer.

## Use / 使用

1. Click the destination chat input in your desktop app or browser.
2. Click the floating **截图** button, or press **Ctrl+Alt+S**.
3. Drag to select a region. Release to capture; **Esc** or right click cancels.
4. The tool restores the original window and editable control, then sends **Ctrl+V** if focus is verified. Check that an image attachment appeared before sending your message yourself.

Drag the button to reposition it. Right-click the button or tray icon to disable auto paste, open the capture folder, copy the last capture, or exit. If the shortcut is occupied, the floating button still works.

## Compatibility and limits / 兼容范围

The integration uses Windows UI Automation, the clipboard, and a paste keystroke. It does not modify the agent, inject code, use private APIs, or require a browser extension. It targets the window and editable control focused before capture, not an account, chat ID, or DOM selector.

| Destination | Expected behavior | Status |
| --- | --- | --- |
| Desktop agent with accessible input and image paste | Restore input, attempt image paste | Requires manual validation |
| Chromium/Electron web chat with accessible editable input and image paste | Restore input, attempt PNG paste | Requires manual validation |
| Input not exposed through Windows accessibility | Copy/save screenshot, manual Ctrl+V or upload | Supported fallback |
| Chat does not accept images from clipboard | Upload saved PNG using the chat's upload button | Supported fallback |
| macOS / Linux / mobile | No build yet | Not supported in v0.1 |

“通用”指不绑定某一家 agent。**不能保证所有聊天工具都支持图片粘贴。** 当前尚未完成 Codex、ChatGPT、Claude 等产品的人工兼容性测试，不宣称已全量适配。

Auto paste only proceeds when an editable control is exposed and its restored focus can be verified. A successful paste keystroke does not prove the chat accepted the attachment. If the input disappears, the browser changes tabs, Windows refuses foreground activation, or accessibility is unavailable, use the clipboard or saved file. Click the desired input before every screenshot. Use the capture shortcut after releasing other keys. Both apps should normally run without administrator privileges.

Snapshots are captured across the virtual desktop, including monitors at negative coordinates. The app is system-DPI aware; mixed-DPI and HDR displays need manual validation. Protected video, secure desktops, and remote session restrictions can produce unavailable or black screen content.

## Privacy / 隐私

All processing is local. No network calls, telemetry, API keys, or uploads. Only your selected region is saved as PNG in `%LOCALAPPDATA%\AgentChatScreenshotButton\Captures`. A temporary full-desktop image exists in memory during selection and is disposed afterward. Capturing replaces clipboard contents. Files accumulate until you delete them; the tray menu opens the folder. Screenshots are uploaded only if you send them through your target chat app.

## Uninstall / 卸载

Exit the tool, then run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$env:LOCALAPPDATA\AgentChatScreenshotButton\uninstall.ps1"
```

This removes the executable and Start menu/startup shortcuts. Captures are preserved. Add `-RemoveCaptures` to explicitly delete saved captures.

## Build / 开发

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test
```

The build uses the Windows .NET Framework C# compiler and framework libraries. No package download is required. Output: `dist\AgentChatScreenshotButton.exe` plus installer and documentation.

`--self-test` verifies selection geometry, PNG encoding, and native SendInput structure layout without capturing the screen or changing the clipboard. This is not an end-to-end compatibility test.

Before a public release, complete [MANUAL-TESTS.md](MANUAL-TESTS.md). Release automation compiles and packages the app but cannot validate real agent chat attachment behavior.

## Publish on GitHub / 发布

Create a public repository named `agent-chat-screenshot-button` and push this directory. Push a `v*` tag, for example `v0.1.0`, to trigger the release workflow. It builds a ZIP and SHA-256 file, and publishes a **prerelease** until compatibility has been validated.

```powershell
gh auth login
git init -b main
git add .
git commit -m "Add universal floating screenshot tool"
gh repo create agent-chat-screenshot-button --public --source . --remote origin --push
git tag v0.1.0
git push origin v0.1.0
```

Run these commands from this project directory. Login and publication require your GitHub account authorization. This project is independent and is not affiliated with OpenAI or other agent vendors.

MIT licensed.
