# 键盘喵

一只会跟着你的键盘和鼠标一起动的小猫桌宠。Windows 与 macOS 双平台，以 MIT 许可证完全开源。

[下载最新版本](https://github.com/user-098-1/meowkeys/releases/latest)

| 平台 | 文件 | 使用方式 |
| --- | --- | --- |
| Windows 10 / 11 | `Meowkeys-<版本>-Windows.exe` | 免安装，下载后双击运行 |
| macOS 11 及以上 | `Meowkeys-<版本>-macOS.zip` | 解压后把 `键盘喵.app` 拖入「应用程序」 |

首次运行提示：

- Windows：若 SmartScreen 弹出"已保护你的电脑"，点击**更多信息 → 仍要运行**。
- macOS：若提示"无法验证开发者"，右键应用选**打开**，或到**系统设置 → 隐私与安全性**点**仍要打开**；随后按应用内提示在「输入监控」中授权键盘联动。
- 小猫只读取物理按键编号驱动动画，不会读取、记录或上传你输入的内容。
- 卸载：Windows 在托盘菜单退出小猫后删除 exe；macOS 把应用拖入废纸篓。设置都保存在本机，重装后自动沿用。

## 从源码构建

请在仓库根目录运行对应命令。

| 平台 | 准备 | 构建命令 |
| --- | --- | --- |
| Windows 10 / 11 | .NET Framework 4.8 | `powershell -File .\windows\app\build.ps1` |
| macOS | Xcode 命令行工具 | `bash macos/build.sh` |

[Windows 源码](windows/) · [macOS 源码](macos/) · [美术资源导出工具](art-tools/) · [MIT 许可证](LICENSE)
