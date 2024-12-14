import { parseResponseError } from './parse';

export class AzureFunctionBackend {

  static async invoke(input: any, fn: string): Promise<Blob> {
    var formData = new FormData();
    for (const key in input) {
      formData.append(key, input[key]);
    }
    const response = await fetch(`https://visiowebtools.azurewebsites.net/api/${fn}`, {
      // const response = await fetch(`http://localhost:7071/api/${fn}`, {
      method: 'POST',
      body: formData
    });

    if (!response.ok) {
      const error = await parseResponseError(response);
      throw error;
    }

    return await response.blob();
  }

}