
export class FileProcessor {

  static async doProcessing(wasm: any, vsdx: Blob, action: string): Promise<Blob> {
    if (wasm) {
      var ab = new Uint8Array(await vsdx.arrayBuffer());
      const data: Uint8Array = wasm.FileProcessor[`${action}`](ab);
      const blob = new Blob([data], { type: 'application/zip' });
      return blob;
    } else {
      var formData = new FormData();
      formData.append('vsdx', vsdx);

      const response = await fetch(`https://visiowebtools.azurewebsites.net/api/${action}AzureFunction`, {
        method: 'POST',
        body: formData
      });

      if (!response.ok) {
        throw new Error(response.statusText);
      }

      return await response.blob();
    }
  }

}