import { App } from "@modelcontextprotocol/ext-apps/app-with-deps";

/**
 * The upload box shown in the chat by the upload_file tool. The user drops a file here and the
 * page sends it straight to the server's upload link, so the file never passes through the
 * model's context. The model is then told the new file_id.
 */

/** What upload_file returns as structured content. */
interface Ticket {
  uploadUrl: string;
  maxFiles: number;
  maxBytes: number;
  expiresAt: string;
  /** How long files are kept, for people: "1 hour". */
  fileLifetime?: string;
}

/** What the upload link answers. */
interface Uploaded {
  fileId: string;
  name: string;
  size: number;
}

const $ = <T extends HTMLElement>(id: string) => document.getElementById(id) as T;
const drop = $<HTMLDivElement>("drop");
const hint = $<HTMLDivElement>("hint");
const list = $<HTMLUListElement>("files");

let ticket: Ticket | null = null;
let sent = 0;

const app = new App({ name: "VisioWebTools upload", version: "1.0.0" }, {});

// Before connect(): the host sends the tool result right after the handshake.
app.ontoolresult = (params) => {
  const t = params.structuredContent as Partial<Ticket> | undefined;
  if (t?.uploadUrl) {
    ticket = t as Ticket;
    const kept = ticket.fileLifetime ? `Files are kept for ${ticket.fileLifetime}, then deleted.` : "Files are kept for a limited time.";
    hint.textContent = `${kept} Up to ${ticket.maxFiles} files, ${Math.round(ticket.maxBytes / 1048576)} MB each.`;
  } else {
    hint.textContent = "This upload box has no upload link. Ask for a new one.";
  }
};

/** Tell the model what the user did. Quietly as context where the host takes it, else as a message. */
async function tellModel(text: string): Promise<void> {
  const content = [{ type: "text" as const, text }];
  const host = app.getHostCapabilities();
  try {
    if (host?.updateModelContext) await app.updateModelContext({ content });
    else if (host?.message) await app.sendMessage({ role: "user", content });
  } catch {
    // The file id is shown here either way; the user can pass it on.
  }
}

function row(text: string, cls: string): HTMLLIElement {
  const li = document.createElement("li");
  li.className = cls;
  li.textContent = text;
  list.append(li);
  return li;
}

async function upload(file: File): Promise<void> {
  if (!ticket) {
    row(`${file.name}: no upload link yet`, "err");
    return;
  }
  if (!/\.(vsdx|pdf)$/i.test(file.name)) {
    row(`${file.name}: only .vsdx and .pdf files`, "err");
    return;
  }
  if (file.size > ticket.maxBytes) {
    row(`${file.name}: over the ${Math.round(ticket.maxBytes / 1048576)} MB limit`, "err");
    return;
  }
  if (sent >= ticket.maxFiles) {
    row(`${file.name}: this box takes ${ticket.maxFiles} files; ask for a new one`, "err");
    return;
  }
  const li = row(`${file.name}: uploading`, "busy");
  try {
    const url = `${ticket.uploadUrl}&name=${encodeURIComponent(file.name)}`;
    const res = await fetch(url, { method: "PUT", body: file, headers: { "Content-Type": "application/octet-stream" } });
    if (!res.ok) throw new Error((await res.text()) || `HTTP ${res.status}`);
    const done = (await res.json()) as Uploaded;
    sent++;
    li.className = "ok";
    li.textContent = `${done.name} `;
    const code = document.createElement("code");
    code.textContent = `file_id ${done.fileId}`;
    li.append(code);
    await tellModel(
      `The user uploaded "${done.name}" (${done.size} bytes) in the upload box. Its file_id is ${done.fileId}; pass it to the VisioWebTools tools.`,
    );
  } catch (err) {
    li.className = "err";
    li.textContent = `${file.name}: ${err instanceof Error ? err.message : String(err)}`;
  }
}

async function take(files: FileList | null | undefined): Promise<void> {
  for (const file of Array.from(files ?? [])) await upload(file);
}

drop.addEventListener("dragover", (e) => { e.preventDefault(); drop.classList.add("over"); });
drop.addEventListener("dragleave", () => drop.classList.remove("over"));
drop.addEventListener("drop", (e) => { e.preventDefault(); drop.classList.remove("over"); void take(e.dataTransfer?.files); });
$<HTMLInputElement>("pick").addEventListener("change", (e) => void take((e.target as HTMLInputElement).files));

await app.connect();
