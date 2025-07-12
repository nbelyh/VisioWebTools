import { useState } from 'react';
import { DropZone } from './DropZone';
import { PrimaryButton } from './PrimaryButton';
import { ErrorNotification } from './ErrorNotification';
import { AzureFunctionBackend } from '../services/AzureFunctionBackend';
import { useDotNetFixedUrl } from '../services/useDotNetFixedUrl';
import { TextField, SelectField, ColorField } from './FormFields';
import { useFileProcessing } from '../services/useFileProcessing';

export const PdfTip = (props: {

}) => {

  const { processing, error, processFile, setError } = useFileProcessing();

  const [pdf, setPdf] = useState<File>();
  const [vsdx, setVsdx] = useState<File>();
  const [x, setX] = useState(0);
  const [y, setY] = useState(0);
  const [color, setColor] = useState('#ffffe0');
  const [icon, setIcon] = useState('Note');

  const { dotnet, loading, loadError } = useDotNetFixedUrl();

  const doProcessing = async (pdf: File, vsdx: File, color: string, icon: string, x: number, y: number) => {
    if (dotnet) {
      var pdfBytes = new Uint8Array(await pdf.arrayBuffer());
      var vsdxBytes = new Uint8Array(await vsdx.arrayBuffer());
      const output: Uint8Array = dotnet.FileProcessor.AddTooltips(pdfBytes, vsdxBytes, color, icon, x, y);
      return new Blob([output], { type: 'application/pdf' });
    } else {
      return await AzureFunctionBackend.invoke({ pdf, vsdx, color, icon, x, y }, 'AddTooltipsFunction');
    }
  }

  const icons = ["NoIcon", "Comment", "Help", "Insert", "Key", "NewParagraph", "Note", "Paragraph"];

  const uploadFiles = async () => {

    if (!pdf || !vsdx) {
      setError('Please select both PDF and VSDX files');
      return;
    }

    if (typeof window.appInsights !== 'undefined') {
      window.appInsights.trackEvent({ name: "PdfTipClicked" });
    }

    await processFile(() => doProcessing(pdf, vsdx, color, icon, x, y), {
      processingMessage: 'Adding tooltips...',
      fileName: `Tooltips_${pdf.name}`
    });
  }

  return (
    <>
      <ErrorNotification error={error || loadError} />

      <DropZone accept="application/pdf" sampleFileName="DropMe.pdf"
        label="Drop a PDF file (without tooltips) that you have exported from Visio here"
        onChange={setPdf}
      />

      <DropZone accept="application/vnd.ms-visio.drawing" sampleFileName="DropMe.vsdx"
        label="Drop the original Visio VSDX file to copy the tooltips from here"
        onChange={setVsdx}
      />

      <div className="grid md:grid-cols-6 gap-4">
        <TextField
          id="tooltip-x"
          label="Tooltip X location:"
          value={x.toString()}
          onChange={(value) => setX(Number.parseFloat(value) || 0)}
          type="number"
        />

        <TextField
          id="tooltip-y"
          label="Tooltip Y location:"
          value={y.toString()}
          onChange={(value) => setY(Number.parseFloat(value) || 0)}
          type="number"
        />

        <SelectField
          id="tooltip-icon"
          label="Tooltip Icon:"
          value={icon}
          options={icons}
          onChange={setIcon}
        />

        <ColorField
          id="tooltip-color"
          label="Tooltip color:"
          value={color}
          onChange={setColor}
        />
      </div>

      <div className="my-4 bg-slate-100 p-4 rounded w-5/6">
        <strong>Note:</strong> Some options (such as color and icon type) may not work in all PDF viewers. 
        Try the <a href="https://get.adobe.com/reader/" target="_blank" rel="noopener noreferrer">Adobe PDF viewer</a>.
      </div>
      
      <hr className="my-4" />

      <PrimaryButton onClick={uploadFiles} disabled={!pdf || !vsdx || !!processing || loading}>{processing || "Add comments to PDF"}</PrimaryButton>
    </>
  );
}
