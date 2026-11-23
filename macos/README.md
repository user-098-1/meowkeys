# macOS 构建

在 Mac 上安装 Xcode 或 Xcode 命令行工具，然后从仓库根目录运行以下命令。

```sh
bash macos/build.sh
```

脚本会编译 Intel 和 Apple 芯片两个版本，合并为一个支持 macOS 11 及以上版本的通用应用。构建成功后，`macos/build/` 中会生成 `键盘喵.app` 和对应版本的 `.pkg` 安装包。

本地构建的应用使用临时签名（ad hoc）。安装包会将应用放入「应用程序」文件夹。首次打开后，可按提示在「输入监控」中授权键盘联动。
