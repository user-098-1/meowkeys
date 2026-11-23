# 构建 Windows 版

在 Windows 10 或 11 上使用 Windows PowerShell 5.1 或 PowerShell 7。需要安装 .NET Framework 4.8 或 4.8.1，其中包含 WPF 程序集和 Framework 自带的 C# 编译器。构建安装包还需要 NSIS 3，安装时保留 Modern UI 2 与简体中文语言文件。

在仓库根目录运行以下命令，同时编译桌宠和离线安装包。

```powershell
.\windows\build.ps1
```

生成的文件是 `windows/build/键盘喵.exe` 和 `windows/build/键盘喵-2.0.0-Windows安装包.exe`。脚本会从 PATH 或 NSIS 的常规安装位置寻找 `makensis.exe`。需要自定义编译器位置或输出目录时可以使用下面的参数。

```powershell
.\windows\build.ps1 -Compiler '.\tools\NSIS\makensis.exe' -OutputDirectory '.\artifacts'
```

只编译便携版时，运行下面的脚本。它只需要 .NET Framework，可以用 `-Output` 指定目标 EXE。

```powershell
.\windows\app\build.ps1
```

安装包默认安装到当前账户的 `%LOCALAPPDATA%\Programs\MeowkeysDesktop`，创建开始菜单快捷方式，并可选择添加桌面快捷方式。首次安装时采用黑猫、薄荷打字机键盘和酒红轨迹球。已有设置优先，保存在 `%LOCALAPPDATA%\MeowkeysDesktop\preferences.xml`。可从 Windows 的应用列表卸载，之后重新安装仍能沿用这些设置。
