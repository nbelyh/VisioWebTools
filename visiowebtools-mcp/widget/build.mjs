// Bundles the upload widget into one self-contained HTML page (dist/upload.html), which the MCP
// server embeds and serves as a ui:// resource. Hosts load it into a sandboxed iframe that may
// fetch nothing undeclared, so the script and styles go inline.
import { build } from "esbuild";
import { mkdir, readFile, writeFile } from "node:fs/promises";

const out = await build({
  entryPoints: ["src/upload.ts"],
  bundle: true,
  minify: true,
  format: "esm",
  target: "es2022",
  write: false,
});
const js = out.outputFiles[0].text.replace(/<\/script/gi, "<\\/script");
const template = await readFile("src/upload.html", "utf8");
await mkdir("dist", { recursive: true });
await writeFile("dist/upload.html", template.replace("/*SCRIPT*/", () => js));
console.log(`dist/upload.html: ${(js.length / 1024).toFixed(0)} KB of script`);
