import { useState } from 'react';
import { DropZone } from './DropZone';
import { PrimaryButton } from './PrimaryButton';

export const SplitPages = (props: {
}) => {

  const [vsdx, setVsdx] = useState<File>();
  const [error, setError] = useState('');
  const [processing, setProcessing] = useState(false);

  const onFileChange = (file?: File) => {
    setVsdx(file);
  }

  const onExtractImages = async () => {

    if (typeof window.appInsights !== 'undefined') {
      window.appInsights.trackEvent({ name: "SplitPagesClicked" });
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
      // const response = await fetch('https://visiowebtools.azurewebsites.net/api/SplitPagesAzureFunction', {
      const response = await fetch('http://localhost:7071/api/SplitPagesAzureFunction', {
        method: 'POST',
        body: formData
      });

      if (!response.ok) {
        throw new Error(response.statusText);
      }
      const blob = await response.blob();
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
      {!!error && <div className="flex">
        <div className="my-3 bg-red-100 p-4 w-5/6">
          <strong>Ups! Something went wrong</strong>.
          Please make sure you have selected the VSDX (not VSD) file,
          or reload the page and try again: {error}. If it the problem persists, please report an issue to our <a href="https://github.com/nbelyh/visiopdftip-webapp/issues" target="_blank">GitHub</a>
        </div>
      </div>}

      <DropZone
        accept="application/vnd.ms-visio.drawing"
        sampleFileName="SplitMe.vsdx"
        label="Drop the Visio VSDX file to split pages here"
        onChange={onFileChange}
      />

      {vsdx && <PrimaryButton disabled={processing} onClick={onExtractImages}>Split Pages</PrimaryButton>}
    </>
  );
}
