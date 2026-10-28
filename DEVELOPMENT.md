# 灯种守夜 v0.1

这个原型以离线 Canvas 游戏为主体，Windows 原生启动器嵌入整个游戏文件。
设计范围是一张可完整通关的小地图；不用外部图像、字体或音频链接。

## 结构

- `src/engine.js`：与浏览器无关的模拟。寻路、波次、塔、投射物、清障、技能、经济、胜负、检查点。
- `src/game.js`：实际 Canvas 场景与动画、HTML 交互、音频合成、存档和标题/结算界面。
- `src/style.css` / `src/index.html`：响应式缩放的 1280×800 游戏界面。
- `src/launcher.c`：Windows Unicode 启动器。提取 RCDATA 到当前用户目录，使用安装的 Edge 启动应用窗口，保留独立浏览器配置；找不到 Edge 时使用默认浏览器。
- `src/icon.ico`：原创图标。
- `build.py`：把三份源内容组装成一个 HTML，按需生成资源并编译 EXE。

## 构建

只生成 HTML：

```sh
python build.py
```

在有 MinGW-w64 的 Linux 环境中编译 Windows x64 程序：

```sh
MINGW_CC=x86_64-w64-mingw32-gcc \
MINGW_WINDRES=x86_64-w64-mingw32-windres \
python build.py
```

可执行程序仅依赖标准 Windows 系统 DLL；浏览器渲染依赖已有 Edge/Chrome。
程序资源是 HTML UTF-8 字节，不依赖 EXE 所在目录是否有其他文件。

## 测试

```sh
node tests/engine.test.js
node tests/balance.js
```

开发时还可安装 `happy-dom` 和 `@napi-rs/canvas`，运行界面与绘制测试。
`tests/dom.test.cjs` 默认从工程依赖查找模块；可用 `GAME_TEST_DEPS` 指定依赖目录。

```sh
npm install --no-audit --no-fund
node tests/dom.test.cjs
```

`tests/ui.test.js` 是可选的真实浏览器测试，需 Playwright 与 Chromium。
本次执行环境不允许浏览器所需的本地 socket，因此本次未完成该项。
可设置 `GAME_BROWSER` 指定本地 Chrome/Chromium 路径，在支持的环境中执行。

## 添加内容

塔 / 敌人数值：`TOWERS`、`TYPES`。新敌人还需在 `enemy()` 中绘制。
新塔还需同步 `tower()` 和 `towerSvg()`。
地图的寻路与画面道路共用 `PATH`，避免视觉与移动路径不一致。
新地图需要一起定义路径控制点、塔位、障碍、背景构图与波次。
模拟使用 60Hz 固定步长，界面以 requestAnimationFrame 绘制，倍速只改变模拟时间。

建议下一版先引入地图配置对象，扩展第二地图，再做塔分支与兵营阻挡，
保持每版都有一个完整可玩并可通关的内容单位。
