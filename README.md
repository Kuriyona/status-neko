# Status Neko

系统托盘监视器 — 同时显示 SMTC 媒体信息、Steam 游戏状态和 GitHub 最近提交。

## 功能

-   **SMTC 监听** — 实时显示当前播放的媒体标题、艺术家、来源应用
-   **Steam 状态** — 显示 Steam 在线状态和当前游戏（每 60 秒轮询）
-   **GitHub 活跃** — 显示最近一次提交信息和时间（每 60 秒轮询）
-   **API 推送** — 数据变化时自动（或手动）推送 JSON 到自定义 URL
-   系统托盘图标，hover 显示摘要信息

## 构建

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o dist
```

输出在 `dist/StatusNeko.exe`，单文件，需要 [.NET 10 运行时](https://dotnet.microsoft.com/download/dotnet/10.0)。

## 使用

1. 首次运行，在设置中填入 Steam API Key / Steam ID / GitHub 用户名
2. （可选）填入 GitHub Token 避免速率限制
3. （可选）填入 API 推送 URL 开启状态推送
4. 右键托盘图标 → 显示窗口
5. 媒体、Steam、GitHub 状态自动更新

## 配置

`config.json`（与 exe 同目录）：

```json
{
  "steam_api_key": "你的Steam API Key",
  "steam_steam_id": "你的Steam ID",
  "github_username": "你的GitHub用户名",
  "github_token": "你的GitHub Token（可选）",
  "api_push_url": "http://example.com/webhook（可选）"
}
```

## API 推送结构

配置 `api_push_url` 后，每次数据变化时自动 POST JSON 到该地址（也可点击窗口的"推送"按钮手动触发）：

```json
{
  "media": {
    "title": "歌曲名",
    "artist": "艺术家",
    "album": "专辑名",
    "source_app": "来源应用.exe"
  },
  "steam": {
    "persona_name": "用户名",
    "state": 1,
    "game": "游戏名"
  },
  "github": {
    "repo": "owner/repo",
    "message": "提交信息",
    "timestamp": "2026-07-16T12:00:00Z"
  }
}
```

各字段在无数据时为 `null`。

## 技术栈

-   .NET 10 + Windows Forms
-   Windows.Media.Control (WinRT SMTC)
-   Steam Web API
-   GitHub REST API
