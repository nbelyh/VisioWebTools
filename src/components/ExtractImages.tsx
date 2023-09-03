import { useState } from 'react';
import { DropZone } from './DropZone';

export const ExtractImages = (props: {
}) => {

  const [vsdx, setVsdx] = useState<File>();
  const [error, setError] = useState('');
  const [processing, setProcessing] = useState(false);

  const onFileChange = (file?: File) => {
    setVsdx(file);
  }

  const onExtractImages = () => {

    // if (typeof window.appInsights !== 'undefined') {
    //   window.appInsights.trackEvent({ name: "Conversion" });
    // }

    if (vsdx) {

      var formData = new FormData();
      formData.append('vsdx', vsdx);

      setProcessing(true);
      setError('');
      // fetch('http://localhost:7071/api/ExtractImagesAzureFunction', {
      fetch('https://visiopdftip.azurewebsites.net/api/ExtractImagesAzureFunction', {
        method: 'POST',
        body: formData
      }).then(response => {
        return response.blob();
      }).then(blob => {
        var url = window.URL.createObjectURL(blob);
        var a = document.createElement('a');
        // a.download = "result.pdf"
        a.target = "_blank";
        a.href = url;
        a.download = `${vsdx.name.replace(/\.[^/.]+$/, "")}_images.zip`;
        a.click();
      }).catch(e => {
        setError(e.message);
      }).finally(() => {
        setProcessing(false);
      });
    } else {
      setError('Please select the VSDX file');
    }
  }

  return (
    <>
      {!!error && <div className="row">
        <div className="col-md-10 alert alert-danger mt-3">
          <strong>Ups! Something went wrong</strong>.
          Please make sure you have selected the VSDX (not VSD) file,
          or reload the page and try again: {error}. If it the problem persists, please report an issue to our
          <a href="https://github.com/nbelyh/visiopdftip-webapp/issues" target="_blank">GitHub</a>
        </div>
      </div>}

      <div className="row">
        <DropZone
          accept="application/vnd.ms-visio.drawing"
          sampleFileName="ImageSample.vsdx"
          label="Please drop the Visio VSDX file to extract media (images) from"
          onChange={onFileChange}
        />
      </div>

      <div className="row">
        <div className='col-md-10' style={{ textAlign: 'center' }}>
          {vsdx && <button className="btn btn-primary" disabled={processing} onClick={onExtractImages} >Extract Images</button>}
        </div>
      </div>
    </>
  );
}
