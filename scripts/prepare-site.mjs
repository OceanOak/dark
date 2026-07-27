import { copyFile, mkdir, readdir, rename, writeFile } from "node:fs/promises";

await mkdir("dist/client", { recursive: true });

for (const entry of await readdir("dist", { withFileTypes: true })) {
  if (entry.name === "client") continue;
  await rename(`dist/${entry.name}`, `dist/client/${entry.name}`);
}

await mkdir("dist/server", { recursive: true });
await mkdir("dist/.openai", { recursive: true });

await copyFile(".openai/hosting.json", "dist/.openai/hosting.json");

await writeFile(
  "dist/server/index.js",
  `export default {
  async fetch(request, env) {
    return env.ASSETS.fetch(request);
  },
};
`,
  "utf8"
);
