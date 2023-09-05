import { useState } from 'react';
import { DropZone } from './DropZone';
import { PrimaryButton } from './PrimaryButton';

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

  const uploadFiles = async () => {

    setError('');

    if (!pdf || !vsdx) {
      setError('Please select both PDF and VSDX files');
      return;
    }

    if (typeof window.appInsights !== 'undefined') {
    	window.appInsights.trackEvent({ name: "PdfTipClicked"});
    }

    var formData = new FormData();
    formData.append('pdf', pdf);
    formData.append('vsdx', vsdx);
    formData.append('color', color);
    formData.append('icon', icon);
    formData.append('x', x.toString());
    formData.append('y', y.toString());

    setProcessing(true);

    try {
      const response = await fetch('https://visiowebtools.azurewebsites.net/api/AddTooltipsFunction', {
        method: 'POST',
        body: formData
      });

      if (!response.ok) {
        throw new Error(response.statusText);
      }

      const blob = await response.blob();
      var url = window.URL.createObjectURL(blob);
      var a = document.createElement('a');
      a.download = `Tooltips_${pdf.name}`
      a.target = "_blank";
      a.href = url;
      a.click();
    } catch (e: any) {
      setError(e?.message);
    } finally {
      setProcessing(false);
    }
  }

  const inputClass = "mt-1 block w-full rounded";

  return (
    <>
      {!!error && <div className="flex">
        <div className="my-3 bg-red-100 p-4 w-5/6">
          <strong>Ups! Something went wrong</strong>. Please make sure you have selected the exported PDF file and the original VSDX file,
          or reload the page and try again: {error}. If it the problem persists, please report an issue to our <a href="https://github.com/nbelyh/visiopdftip-webapp/issues" target="_blank">GitHub</a>
        </div>
      </div>}

      <DropZone accept="application/pdf" sampleFileName="Drawing1.pdf"
        label="Drop a PDF file (without tooltips) that you have exported from Visio here"
        onChange={setPdf}
      />

      <DropZone accept="application/vnd.ms-visio.drawing" sampleFileName="Drawing1.vsdx"
        label="Drop the original Visio VSDX file to copy the tooltips from here"
        onChange={setVsdx}
      />

      <div className="grid md:grid-cols-6 gap-4">
        <label className="block">
          <span className="text-neutral-700">Tooltip X location:</span>
          <input className={inputClass} type="number" value={x} onChange={e => setX(Number.parseInt(e.target.value))} />
        </label>

        <label className="block">
          <span className="text-neutral-700">Tooltip Y location:</span>
          <input className={inputClass} type="number" value={y} onChange={e => setY(Number.parseInt(e.target.value))} />
        </label>

        <label className="block">
          <span className="text-neutral-700">Tooltip Icon:</span>
          <select className={inputClass} value={icon} onChange={e => setIcon(e.target.value)}>
            {icons.map(icon => <option key={icon} value={icon}>{icon}</option>)}
          </select>
        </label>
        <label className="block">
          <span className="text-neutral-700">Tooltip color:</span>
          <input className={inputClass + " " + "form-input h-10"} type="color" id="color-picker" value={color} onChange={e => setColor(e.target.value)} />
        </label>
      </div>

      <div className="my-4 bg-slate-100 p-4 rounded">
        <strong>Note:</strong> Some options (such as tooltip color and icon type) may not work in all PDF
        viewers. Check in <a href="https://get.adobe.com/reader/" target="_blank" rel="noopener noreferrer">Adobe PDF viewer</a>.
      </div>

      <PrimaryButton onClick={uploadFiles} disabled={processing || !pdf || !vsdx}>Add comments to PDF</PrimaryButton>
    </>
  );
}
