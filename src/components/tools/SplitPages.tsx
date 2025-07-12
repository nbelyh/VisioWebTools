import { useState } from 'react';
import { DropZone } from './common/DropZone';
import { PrimaryButton } from './common/PrimaryButton';
import { AzureFunctionBackend } from 'services/AzureFunctionBackend';
import { useDotNetFixedUrl } from 'services/useDotNetFixedUrl';
import { ErrorNotification } from './common/ErrorNotification';
import { useFileProcessing } from 'services/useFileProcessing';

export const SplitPages = (props: {
}) => {

  const [vsdx, setVsdx] = useState<File>();
  const { processing, error, processFile, setError } = useFileProcessing();

  const onFileChange = (file?: File) => {
    setVsdx(file);
  }

  const { dotnet, loading, loadError } = useDotNetFixedUrl();

  const doProcessing = async (vsdx: File) => {
    if (dotnet) {
      var ab = new Uint8Array(await vsdx.arrayBuffer());
      const output: Uint8Array = dotnet.FileProcessor.SplitPages(ab);
      return new Blob([output], { type: 'application/zip' });
    } else {
      return await AzureFunctionBackend.invoke({ vsdx }, 'SplitPagesAzureFunction');
    }
  }

  const onSplitPages = async () => {

    if (typeof window.appInsights !== 'undefined') {
      window.appInsights.trackEvent({ name: "SplitPagesClicked" });
    }

    if (!vsdx) {
      setError('Please select the VSDX file');
      return;
    }

    await processFile(() => doProcessing(vsdx), {
      processingMessage: 'Splitting...',
      fileName: `${vsdx.name.replace(/\.[^/.]+$/, "")}_pages.zip`
    });
  }

  return (
    <>
      <ErrorNotification error={error || loadError} />
      <DropZone
        accept="application/vnd.ms-visio.drawing"
        sampleFileName="SplitMe.vsdx"
        label="Drop the Visio VSDX file to split pages here"
        onChange={onFileChange}
      />

      <hr className="my-4" />

      <PrimaryButton disabled={!vsdx || !!processing || loading} onClick={onSplitPages}>{processing || "Split Pages"}</PrimaryButton>
    </>
  );
}
