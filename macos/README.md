# macOS 构建

在 Mac 上安装 Xcode 或 Xcode 命令行工具，然后从仓库根目录运行以下命令。

```sh
bash macos/build.sh
```

脚本会编译 Intel 和 Apple 芯片两个版本，合并为一个支持 macOS 11 及以上版本的通用应用。构建成功后，`macos/build/` 中会生成 `键盘喵.app` 和用于分发的 `键盘喵-<版本>-macOS.zip`。

使用时解压 zip，把 `键盘喵.app` 拖入「应用程序」文件夹。应用为临时签名（ad hoc），首次打开若被 Gatekeeper 拦截，请右键选择「打开」，或在「系统设置 → 隐私与安全性」中点「仍要打开」。首次启动后，按提示在「输入监控」中授权键盘联动。
