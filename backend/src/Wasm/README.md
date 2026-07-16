# Darklang browser REPL (WASM)

The parser + runtime compiled to WebAssembly. Dark evaluates entirely in
the browser tab, no server. The publish output is a static site; host it anywhere.

## Build & run (from the repo root)

```
# 1. Generate the package snapshot (re-run when packages change;
#    also refreshes the published copy, no republish needed)
python3 backend/src/Wasm/generate-snapshot.py

# 2. Publish (re-run when F# or index.html changes)
dotnet publish backend/src/Wasm/Wasm.fsproj -c Release -o rundir/wasm-repl

# 3. Serve locally (9090 because the devcontainer only forwards 9090-9099
#    to the host; any port works for a browser inside the container)
./scripts/run-cli serve Darklang.WasmReplServer.router --port 9090
```

Open http://localhost:9090 → `1 + 2` → `3`. To host publicly, upload
`rundir/wasm-repl/wwwroot` to any static host (GitHub Pages, Cloudflare Pages, etc.)

## Notes

- Supports single expressions only (no declarations yet). Builtins come from
  `Builtins.Pure`, `Stdlib.*` from the snapshot. Output matches `dark eval`.
- Keep `<WasmBuildNative>false</WasmBuildNative>`. SQLite comes in through
  `LibParser`/`LibDB` and crashes the native-relink step. The browser never
  uses SQLite, so skipping it is safe.
- Always publish to `rundir/`. Publishing into the project tree makes the next
  publish copy the previous build into itself.

## Next steps

- Hosted package manager to replace the snapshot seam (marked in `Repl.fs`).
- Break `LibParser`'s dependency on `LibDB`. That dependency is what pulls
  SQLite into the browser bundle (and why the `WasmBuildNative` workaround
  exists) — the browser never uses it.
- Make the first load smaller (~10 MB gzipped today). Trim unused code and drop
  the accidental `Expecto` (test framework) reference.
