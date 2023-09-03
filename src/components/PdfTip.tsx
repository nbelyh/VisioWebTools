import { useState } from 'react';
import { DropZone } from './DropZone';

export const PdfTip = (props: {

}) => {

  const [error, setError] = useState('');
  const [processing, setProcessing] = useState(false);

  const [pdf, setPdf] = useState<File>();
  const [vsdx, setVsdx] = useState<File>();
  const [x, setX] = useState(0);
  const [y, setY] = useState(0);
  const [color, setColor] = useState('#ffffe0');
  const [icon, setIcon] = useState('Note');

  const icons = ["NoIcon", "Comment", "Help", "Insert", "Key", "NewParagraph", "Note", "Paragraph"];

  const uploadFiles = () => {

    if (!pdf || !vsdx) {
      setError('Please select both PDF and VSDX files');
      return;
    }

    // if (window.appInsights) {
    // 	window.appInsights.trackEvent({ name: "Conversion"});
    // }

    var formData = new FormData();
    formData.append('pdf', pdf);
    formData.append('vsdx', vsdx);
    formData.append('color', color);
    formData.append('icon', icon);
    formData.append('x', x.toString());
    formData.append('y', y.toString());

    setProcessing(true);
    setError('');
    fetch('https://visiopdftip.azurewebsites.net/api/AddTooltipsFunction', {
      method: 'POST',
      body: formData
    }).then(response => {
      return response.blob();
    }).then(blob => {
      var url = window.URL.createObjectURL(blob);
      var a = document.createElement('a');
      a.download = `Tooltips_${pdf.name}`
      a.target = "_blank";
      a.href = url;
      a.click();
    }).catch(e => {
      setError(e.message);
    }).finally(() => {
      setProcessing(false);
    });
  }

  return (
    <>
      {!!error &&
        <div className="row">
          <div className="col-md-10 alert alert-danger mt-3">
            <strong>Ups! Something went wrong</strong>. Please make sure you have selected the exported PDF file and the original VSDX file,
            or reload the page and try again: {error}. If it the problem persists, please report an issue to our
            <a href="https://github.com/nbelyh/visiopdftip-webapp/issues" target="_blank">GitHub</a>
          </div>
        </div>}

      <div className="row">
        <DropZone accept="application/pdf" sampleFileName="Drawing1.pdf"
          label="Please drop a PDF file (without tooltips) you have exported from Visio"
          onChange={setPdf}
        />

        <DropZone accept="application/vnd.ms-visio.drawing" sampleFileName="Drawing1.vsdx"
          label="Please drop the original Visio VSDX file to copy the tooltips from"
          onChange={setVsdx}
        />
      </div>

      <div className="row">
        <div className="form-group col-lg-2 col-md-6">
          <label htmlFor="tooltip-x">Tooltip X location:</label>
          <input type="number" id="tooltip-x" value={x} className="form-control"
            onChange={e => setX(Number.parseInt(e.target.value))} />
        </div>

        <div className="form-group col-lg-2 col-md-6">
          <label htmlFor="tooltip-y">Tooltip Y location:</label>
          <input type="number" id="tooltip-y" value={y} className="form-control"
            onChange={e => setY(Number.parseInt(e.target.value))} />
        </div>

        <div className="form-group col-lg-4 col-md-6">
          <label htmlFor="color-picker">Tooltip color:</label>
          <input type="color" id="color-picker" value={color} className="form-control"
            onChange={e => setColor(e.target.value)}
          />
        </div>

        <div className="form-group col-lg-4 col-md-6">
          <label htmlFor="icon-picker">Tooltip Icon:</label>
          <select id="icon-picker" className="form-control" value={icon} onChange={e => setIcon(e.target.value)}>
            {icons.map(icon => <option value={icon}>{icon}</option>)}
          </select>
        </div>
      </div>

      <div className="row">
        <div className="alert mt-3">
          <strong>Note:</strong> Some options (such as tooltip color and icon type) may not work in all PDF
          viewers. Check in <a href="https://get.adobe.com/reader/" target="_blank" rel="noopener noreferrer">Adobe PDF viewer</a>.
        </div>
      </div>

      <button onClick={uploadFiles} className="btn btn-primary" disabled={processing || !pdf || !vsdx}>Generate PDF with tooltips</button>
    </>);
}
