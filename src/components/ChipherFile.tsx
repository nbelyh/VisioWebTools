import { useState } from 'react';
import { DropZone } from './DropZone';
import { PrimaryButton } from './PrimaryButton';
import { AzureFunctionBackend } from '../services/AzureFunctionBackend';
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

  const { dotnet, loading, loadError } = useDotNetFixedUrl();

  const doProcessing = async (vsdx: File) => {
    var options = {
      enableChipherShapeText,
      enableChipherShapeFields,
      enableChipherPageNames,
      enableChipherPropertyValues,
      enableChipherPropertyLabels
    };
    const optionsJson = JSON.stringify(options);
    if (dotnet) {
      var ab = new Uint8Array(await vsdx.arrayBuffer());
      const output: Uint8Array = dotnet.FileProcessor.ChipherFile(ab, optionsJson)
      return new Blob([output], { type: 'application/vnd.ms-visio.drawing' });
    } else {
      return await AzureFunctionBackend.invoke({ vsdx, optionsJson }, 'ChipherFileAzureFunction');
    }
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
  const [enableChipherShapeFields, setEnableChipherShapeFields] = useState(true);
  const [enableChipherPageNames, setEnableChipherPageNames] = useState(true);
  const [enableChipherPropertyValues, setEnableChipherPropertyValues] = useState(true);
  const [enableChipherPropertyLabels, setEnableChipherPropertyLabels] = useState(false);

  return (
    <>
      <ErrorNotification error={error || loadError} />
      <DropZone
        accept="application/vnd.ms-visio.drawing"
        sampleFileName="Chipher.vsdx"
        label="Drop the Visio VSDX file to split pages here"
        onChange={onFileChange}
      />

      <div className='mb-4'>
        <div className="flex items-center">
          <input type="checkbox" className="rounded-sm mr-2" id="enableChipherText" checked={enableChipherShapeText} onChange={(e) => setEnableChipherShapeText(e.target.checked)} />
          <label htmlFor="enableChipherText">Chipher Shape Text</label>
        </div>

        <div className="flex items-center">
          <input type="checkbox" className="rounded-sm mr-2" id="enableChipherShapeFields" checked={enableChipherShapeFields} onChange={(e) => setEnableChipherShapeFields(e.target.checked)} />
          <label htmlFor="enableChipherShapeFields">Chipher Shape Fields</label>
        </div>

        <div className="flex items-center">
          <input type="checkbox" className="rounded-sm mr-2" id="enableChipherPageNames" checked={enableChipherPageNames} onChange={(e) => setEnableChipherPageNames(e.target.checked)} />
          <label htmlFor="enableChipherPageNames">Chipher Page Names</label>
        </div>

        <div className="flex items-center">
          <input type="checkbox" className="rounded-sm mr-2" id="enableChipherPropertyValues" checked={enableChipherPropertyValues} onChange={(e) => setEnableChipherPropertyValues(e.target.checked)} />
          <label htmlFor="enableChipherPropertyValues">Chipher Property Values</label>
        </div>

        <div className="flex items-center">
          <input type="checkbox" className="rounded-sm mr-2" id="enableChipherPropertyLabels" checked={enableChipherPropertyLabels} onChange={(e) => setEnableChipherPropertyLabels(e.target.checked)} />
          <label htmlFor="enableChipherPropertyLabels">Chipher Property Labels</label>
        </div>
      </div>

      <hr className="my-4" />

      <PrimaryButton disabled={!vsdx || processing || loading} onClick={onChipherFile}>Chipher</PrimaryButton>
    </>
  );
}
