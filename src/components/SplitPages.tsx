import { useState } from 'react';
import { DropZone } from './DropZone';
import { PrimaryButton } from './PrimaryButton';
import { AzureFunctionBackend } from '../services/AzureFunctionBackend';
import { useDotNetFixedUrl } from '../services/useDotNetFixedUrl';
import { ErrorNotification } from './ErrorNotification';
import { stringifyError } from '../services/parse';
import { downloadBlob } from '../services/downloadUtils';

export const SplitPages = (props: {
}) => {

  const [vsdx, setVsdx] = useState<File>();
  const [error, setError] = useState('');
  const [processing, setProcessing] = useState('');

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

    setError('');

    if (!vsdx) {
      setError('Please select the VSDX file');
      return;
    }

    try {
      setProcessing('Splitting...');
      const blob = await doProcessing(vsdx);
      downloadBlob(blob, `${vsdx.name.replace(/\.[^/.]+$/, "")}_pages.zip`);
    } catch (e: any) {
      setError(stringifyError(e));
    } finally {
      setProcessing('');
    }
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
