# Status Neko — agent notes

## Build & publish

```powershell
dotnet build
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o dist
```

Output: `dist/StatusNeko.exe` (framework-dependent, needs .NET 10 runtime).

## Architecture

Single WinForms app, no DI, no tests. Four runtime components wired in `MainForm`:

| Component | File | Mechanism |
|-----------|------|-----------|
| SMTC listener | `SMTCMonitor.cs` | `Windows.Media.Control` WinRT events → `BeginInvoke` to UI thread |
| Steam poller | `SteamWatcher.cs` | `System.Threading.Timer` (60s) + `HttpClient` → events |
| GitHub poller | `GitHubWatcher.cs` | `System.Threading.Timer` (60s) + `HttpClient` → two-step API (events → commit) |
| Tray + window | `MainForm.cs` | `NotifyIcon` + `ContextMenuStrip` + `Form` |

Config file `config.json` is read from `AppDomain.CurrentDomain.BaseDirectory` at startup, gitignored.  
Supports optional `api_push_url` — on every state change, POSTs a JSON payload to that URL.

## Constraints

- **Windows-only.** Requires Windows 10 1803+ for SMTC WinRT API.
- **No tests.** Hand-verify UI behavior.
- `python_backup/` contains the original Python source — not part of the C# project.
- GitHub unauthenticated API: 60 req/h. Set `github_token` in config for 5000 req/h.
