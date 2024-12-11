import { useState } from 'react';
import { DropZone } from './DropZone';
import { PrimaryButton } from './PrimaryButton';
import { AzureFunctionBackend } from '../services/AzureFunctionBackend';
import { useDotNetFixedUrl } from '../services/useDotNetFixedUrl';
import { ErrorNotification } from './ErrorNotification';

export const ExtractImages = (props: {
}) => {

  const [vsdx, setVsdx] = useState<File>();
  const [error, setError] = useState('');
  const [processing, setProcessing] = useState(false);

  const onFileChange = (file?: File) => {
    setVsdx(file);
  }
  const { dotnet, loadError, loading } = useDotNetFixedUrl();

  const doProcessing = async (input: File) => {
    if (dotnet) {
      var ab = new Uint8Array(await input.arrayBuffer());
      const data: Uint8Array = dotnet.FileProcessor.ExtractImages(ab);
      const blob = new Blob([data], { type: 'application/zip' });
      return blob;
    } else {
      return await AzureFunctionBackend.invoke({ vsdx: input }, 'ExtractImagesAzureFunction');
    }
  }

  const onExtractImages = async () => {

    if (typeof window.appInsights !== 'undefined') {
      window.appInsights.trackEvent({ name: "ExtractImagesClicked" });
    }

    setError('');

    if (!vsdx) {
      setError('Please select the VSDX file');
      return;
    }

    setProcessing(true);
    try {
      const out = await doProcessing(vsdx);
      const url = window.URL.createObjectURL(out);
      const a = document.createElement('a');
      // a.download = "result.pdf"
      a.target = "_blank";
      a.href = url;
      a.download = `${vsdx.name.replace(/\.[^/.]+$/, "")}_images.zip`;
      a.click();
    } catch (e: any) {
      setError(`${e}`);
    } finally {
      setProcessing(false);
    }
  }

  return (
    <>
      <ErrorNotification error={error || loadError} />
      <DropZone
        accept="application/vnd.ms-visio.drawing"
        sampleFileName="ImageSample.vsdx"
        label="Drop the Visio VSDX file to extract media (images) from here"
        onChange={onFileChange}
      />

      <hr className="my-4" />

      <PrimaryButton disabled={!vsdx || processing || loading} onClick={onExtractImages}>Extract Images</PrimaryButton>
    </>
  );
}
