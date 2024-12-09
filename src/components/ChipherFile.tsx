import { useState } from 'react';
import { DropZone } from './DropZone';
import { PrimaryButton } from './PrimaryButton';
import { WasmNotification } from './WasmNotification';
import { useDotNetFixedUrl } from '../services/useDotNetFixedUrl';
import { ErrorNotification } from './ErrorNotification';

export const ChipherFile = (props: {
}) => {

  const [vsdx, setVsdx] = useState<File>();
  const [error, setError] = useState('');
  const [processing, setProcessing] = useState(false);

  const onFileChange = (file?: File) => {
    setVsdx(file);
  }

  const { dotnet, loading } = useDotNetFixedUrl();

  const doProcessing = async (vsdx: File) => {
    var ab = new Uint8Array(await vsdx.arrayBuffer());
    const output: Uint8Array = dotnet.FileProcessor.ChipherFile(ab, 
      enableChipherShapeText, 
      enableChipherPageNames, 
      enableChipherShapeData);

    return new Blob([output], { type: 'application/vnd.ms-visio.drawing' });
  }

  const onChipherFile = async () => {

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
      a.target = "_blank";
      a.href = url;
      a.download = vsdx.name;
      a.click();
    } catch (e: any) {
      setError(`${e}`);
    } finally {
      setProcessing(false);
    }
  }

  const [enableChipherShapeText, setEnableChipherShapeText] = useState(true);
  const [enableChipherPageNames, setEnableChipherPageNames] = useState(true);
  const [enableChipherShapeData, setEnableChipherShapeData] = useState(true);

  return (
    <>
      <WasmNotification loading={loading} wasm={dotnet} />
      <ErrorNotification error={error} />
      <DropZone
        accept="application/vnd.ms-visio.drawing"
        sampleFileName="Chipher.vsdx"
        label="Drop the Visio VSDX file to split pages here"
        onChange={onFileChange}
      />

      <div className="flex items-center">
        <input type="checkbox" className="rounded-sm mr-2" id="enableChipherText" checked={enableChipherShapeText} onChange={(e) => setEnableChipherShapeText(e.target.checked)} />
        <label htmlFor="enableChipherText">Chipher Shape Text</label>
      </div>

      <div className="flex items-center">
        <input type="checkbox" className="rounded-sm mr-2" id="enableChipherPageNames" checked={enableChipherPageNames} onChange={(e) => setEnableChipherPageNames(e.target.checked)} />
        <label htmlFor="enableChipherPageNames">Chipher Page Names</label>
      </div>

      <div className="flex items-center">
        <input type="checkbox" className="rounded-sm mr-2" id="enableChipherShapeData" checked={enableChipherShapeData} onChange={(e) => setEnableChipherShapeData(e.target.checked)} />
        <label htmlFor="enableChipherShapeData">Chipher Shape Data</label>
      </div>

      {vsdx && <PrimaryButton disabled={processing} onClick={onChipherFile}>{dotnet ? `Chipher` : `Chipher (using our server)`}</PrimaryButton>}
    </>
  );
}