import { useState } from 'react';
import { DropZone } from './DropZone';
import { PrimaryButton } from './PrimaryButton';
import { FileProcessor } from '../services/FileProcessor';
import { WasmNotification } from './WasmNotification';
import { useDotNetFixedUrl } from '../services/useDotNetFixedUrl';

export const SplitPages = (props: {
}) => {

  const [vsdx, setVsdx] = useState<File>();
  const [error, setError] = useState('');
  const [processing, setProcessing] = useState(false);

  const onFileChange = (file?: File) => {
    setVsdx(file);
  }

  const { dotnet, loading } = useDotNetFixedUrl();

  const onExtractImages = async () => {

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
      const blob = await FileProcessor.doProcessing(dotnet, vsdx, 'SplitPages');
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      // a.download = "result.pdf"
      a.target = "_blank";
      a.href = url;
      a.download = `${vsdx.name.replace(/\.[^/.]+$/, "")}_pages.zip`;
      a.click();
    } catch (e: any) {
      setError(e?.message);
    } finally {
      setProcessing(false);
    }
  }

  return (
    <>
      <WasmNotification loading={loading} wasm={dotnet} />
      {!!error && <div className="flex">
        <div className="my-3 bg-red-100 p-4 w-5/6">
          <strong>Ups! Something went wrong</strong>.
          Please make sure you have selected the VSDX (not VSD) file,
          or reload the page and try again: {error}. If it the problem persists, please report an issue to our <a href="https://github.com/nbelyh/visiopdftip-webapp/issues" target="_blank">GitHub</a>
        </div>
      </div>}

      <DropZone
        accept="application/vnd.ms-visio.drawing"
        sampleFileName="SplitPages.vsdx"
        label="Drop the Visio VSDX file to split pages here"
        onChange={onFileChange}
      />

      {vsdx && <PrimaryButton disabled={processing} onClick={onExtractImages}>{dotnet ? `Split Pages` : `Split Pages (using our server)`}</PrimaryButton>}
    </>
  );
}
