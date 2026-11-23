# 构建 Windows 版

在 Windows 10 或 11 上使用 Windows PowerShell 5.1 或 PowerShell 7。需要安装 .NET Framework 4.8 或 4.8.1（其中包含 WPF 程序集和 Framework 自带的 C# 编译器；Windows 10 2019 年 5 月更新及以上已内置）。

在仓库根目录运行以下命令，编译免安装的便携版桌宠。

```powershell
powershell -File .\windows\app\build.ps1
```

生成的文件是 `windows\build\键盘喵.exe`。这是自足的单文件程序，双击即可运行，不需要安装步骤。需要自定义输出位置时可以使用 `-Output` 参数。

```powershell
powershell -File .\windows\app\build.ps1 -Output '.\artifacts\键盘喵.exe'
```

卸载时先在托盘菜单退出小猫，再删除 exe 即可，不写入注册表。样式、位置等设置保存在 `%LOCALAPPDATA%\MeowkeysDesktop\preferences.xml`，删除 exe 不会清掉这些设置，重新下载后自动沿用。
