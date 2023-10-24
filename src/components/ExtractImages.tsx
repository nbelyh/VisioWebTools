import { useState } from 'react';
import { DropZone } from './DropZone';
import { PrimaryButton } from './PrimaryButton';
import { FileProcessor } from '../services/FileProcessor';
import { WasmNotification } from './WasmNotification';
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
  const { dotnet, loading } = useDotNetFixedUrl();

  const onExtractImages = async () => {

    if (typeof window.appInsights !== 'undefined') {
      window.appInsights.trackEvent({ name: "ExtractImagesClicked" });
    }

    setError('');

    if (!vsdx) {
      setError('Please select the VSDX file');
      return;
    }

    var formData = new FormData();
    formData.append('vsdx', vsdx);

    setProcessing(true);
    try {
      const out = await FileProcessor.doProcessing(dotnet, vsdx, 'ExtractImages');
      const url = window.URL.createObjectURL(out);
      const a = document.createElement('a');
      // a.download = "result.pdf"
      a.target = "_blank";
      a.href = url;
      a.download = `${vsdx.name.replace(/\.[^/.]+$/, "")}_images.zip`;
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
      <ErrorNotification error={error} />
      <DropZone
        accept="application/vnd.ms-visio.drawing"
        sampleFileName="ImageSample.vsdx"
        label="Drop the Visio VSDX file to extract media (images) from here"
        onChange={onFileChange}
      />

      {vsdx && <PrimaryButton disabled={processing} onClick={onExtractImages}>{dotnet ? `Extract Images` : `Extract Image (using our server)`}</PrimaryButton>}
    </>
  );
}
