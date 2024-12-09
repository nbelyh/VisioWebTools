import { useState } from 'react';
import { DropZone } from './DropZone';
import { PrimaryButton } from './PrimaryButton';
import { useDotNetFixedUrl } from '../services/useDotNetFixedUrl';
import { ErrorNotification } from './ErrorNotification';

export const SplitPages = (props: {
}) => {

  const [vsdx, setVsdx] = useState<File>();
  const [error, setError] = useState('');
  const [processing, setProcessing] = useState(false);

  const onFileChange = (file?: File) => {
    setVsdx(file);
  }

  const { dotnet, loading, loadError } = useDotNetFixedUrl();

  const doProcessing = async (vsdx: File) => {
    var ab = new Uint8Array(await vsdx.arrayBuffer());
    const output: Uint8Array = dotnet.FileProcessor.SplitPages(ab);
    return new Blob([output], { type: 'application/zip' });
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

    setProcessing(true);
    try {
      const blob = await doProcessing(vsdx);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      // a.download = "result.pdf"
      a.target = "_blank";
      a.href = url;
      a.download = `${vsdx.name.replace(/\.[^/.]+$/, "")}_pages.zip`;
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
        sampleFileName="SplitPages.vsdx"
        label="Drop the Visio VSDX file to split pages here"
        onChange={onFileChange}
      />

      <hr className="my-4" />

      <PrimaryButton disabled={!vsdx || processing || loading} onClick={onSplitPages}>Split Pages</PrimaryButton>
    </>
  );
}
