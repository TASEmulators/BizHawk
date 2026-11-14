# 简体中文界面

BizHawk 的简体中文翻译由外部文件提供，不修改模拟核心，也不需要重新编译即可调整翻译文本。

- 默认语言：简体中文（zh-CN）。
- 翻译文件：`Localization/zh-CN.tsv`。
- 格式：每行是“英文原文 + Tab + 中文译文”。
- 未收录的文字保持英文，不影响程序运行。
- 如需临时切换回英文，在启动 BizHawk 前设置环境变量 `BIZHAWK_LANGUAGE=en-US`；设置为 `zh-CN` 或其他以 `zh` 开头的值则启用中文。

翻译文件会随 EmuHawk 构建输出复制到 `Localization` 子目录。修改 TSV 后重新启动程序即可加载新文本。
