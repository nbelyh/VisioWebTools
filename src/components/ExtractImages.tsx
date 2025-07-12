import { useState } from 'react';
import { DropZone } from './DropZone';
import { PrimaryButton } from './PrimaryButton';
import { AzureFunctionBackend } from '../services/AzureFunctionBackend';
import { useDotNetFixedUrl } from '../services/useDotNetFixedUrl';
import { ErrorNotification } from './ErrorNotification';
import { useFileProcessing } from '../services/useFileProcessing';

export const ExtractImages = (props: {
}) => {

  const [vsdx, setVsdx] = useState<File>();
  const { processing, error, processFile, setError } = useFileProcessing();

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

    if (!vsdx) {
      setError('Please select the VSDX file');
      return;
    }

    await processFile(() => doProcessing(vsdx), {
      processingMessage: 'Extracting Images...',
      fileName: `${vsdx.name.replace(/\.[^/.]+$/, "")}_images.zip`
    });
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

      <PrimaryButton disabled={!vsdx || !!processing || loading} onClick={onExtractImages}>{processing || "Extract Images"}</PrimaryButton>
    </>
  );
}
