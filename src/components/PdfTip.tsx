import { useState } from 'react';
import { DropZone } from './DropZone';
import { PrimaryButton } from './PrimaryButton';
import { ErrorNotification } from './ErrorNotification';
import { WasmNotification } from './WasmNotification';
import { useDotNetFixedUrl } from '../services/useDotNetFixedUrl';

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

  const { dotnet, loading } = useDotNetFixedUrl();

  const doProcessing = async (pdf: File, vsdx: File, color: string, icon: string, x: number, y: number) => {
    var pdfBytes = new Uint8Array(await pdf.arrayBuffer());
    var vsdxBytes = new Uint8Array(await vsdx.arrayBuffer());
    const output: Uint8Array = dotnet.FileProcessor.AddTooltips(pdfBytes, vsdxBytes, color, icon, x, y);
    return new Blob([output], { type: 'application/pdf' });
  }

  const icons = ["NoIcon", "Comment", "Help", "Insert", "Key", "NewParagraph", "Note", "Paragraph"];

  const uploadFiles = async () => {

    setError('');

    if (!pdf || !vsdx) {
      setError('Please select both PDF and VSDX files');
      return;
    }

    if (typeof window.appInsights !== 'undefined') {
      window.appInsights.trackEvent({ name: "PdfTipClicked" });
    }

    setProcessing(true);

    try {

      const blob = await doProcessing(pdf, vsdx, color, icon, x, y);

      var url = window.URL.createObjectURL(blob);
      var a = document.createElement('a');
      a.download = `Tooltips_${pdf.name}`
      a.target = "_blank";
      a.href = url;
      a.click();
    } catch (e: any) {
      setError(`${e}`);
    } finally {
      setProcessing(false);
    }
  }

  const inputClass = "mt-1 block w-full rounded";

  return (
    <>
      <WasmNotification loading={loading} wasm={dotnet} />
      <ErrorNotification error={error} />

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

      <div className="my-4 bg-slate-100 p-4 rounded w-5/6">
        <strong>Note:</strong> Some of these options (such as color and icon type) may not work in all PDF viewers. 
        Check in the <a href="https://get.adobe.com/reader/" target="_blank" rel="noopener noreferrer">Adobe PDF viewer</a>.
      </div>

      {pdf && vsdx && <PrimaryButton onClick={uploadFiles} disabled={processing}>{dotnet ? `Add comments to PDF` : `Add comments to PDF (using our server)`}</PrimaryButton>}
    </>
  );
}
