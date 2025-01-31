import { useState } from 'react';
import { DropZone } from './DropZone';
import { PrimaryButton } from './PrimaryButton';
import { AzureFunctionBackend } from '../services/AzureFunctionBackend';
import { useDotNetFixedUrl } from '../services/useDotNetFixedUrl';
import { ErrorNotification } from './ErrorNotification';

export const JsonExport = (props: {
}) => {

  const [vsdx, setVsdx] = useState<File>();
  const [error, setError] = useState('');
  const [processing, setProcessing] = useState('');

  const onFileChange = (file?: File) => {
    setVsdx(file);
  }

  const { dotnet, loading, loadError } = useDotNetFixedUrl();

  const doProcessing = async (vsdx: File) => {
    var options = {
      includeShapeText,
      includeShapeFields,
      includePropertyRows,
      includeUserRows,
      includeDocumentProperties,
      includeMasters,
      translatableOnly
    };
    const optionsJson = JSON.stringify(options);
    if (dotnet) {
      var ab = new Uint8Array(await vsdx.arrayBuffer());
      const result = dotnet.FileProcessor.ExtractJson(ab, optionsJson);
      return new Blob([result], { type: 'text/json' });
    } else {
      return await AzureFunctionBackend.invoke({ vsdx, optionsJson }, 'TranslateFileAzureFunction');
    }
  }

  const onJsonExport = async () => {

    if (typeof window.appInsights !== 'undefined') {
      window.appInsights.trackEvent({ name: "SplitPagesClicked" });
    }

    setError('');

    if (!vsdx) {
      setError('Please select the VSDX file');
      return;
    }

    setProcessing('Extracting JSON...');
    try {
      const blob = await doProcessing(vsdx);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.target = "_blank";
      a.href = url;
      a.download = vsdx.name.replace('.vsdx', '.json');
      a.click();
    } catch (e: any) {
      setError(`${e}`);
    } finally {
      setProcessing('');
    }
  }

  const [includeShapeText, setincludeShapeText] = useState(true);
  const [includeShapeFields, setincludeShapeFields] = useState(true);
  const [includePropertyRows, setIncludePropertyRows] = useState(true);
  const [includeUserRows, setIncludeUserRows] = useState(true);
  const [includeMasters, setIncludeMasters] = useState(true);
  const [includeDocumentProperties, setIncludeDocumentProperties] = useState(true);
  const [translatableOnly, setTranslatableOnly] = useState(true);

  return (
    <>
      <ErrorNotification error={error || loadError} />
      <DropZone
        accept="application/vnd.ms-visio.drawing"
        sampleFileName="ExtractFromMe.vsdx"
        label="Drop the Visio VSDX file here"
        onChange={onFileChange}
      />

      <div className='mb-4'>

        <div className="flex items-center">
          <input type="checkbox" className="rounded-sm mr-2" id="includeText" checked={includeShapeText} onChange={(e) => setincludeShapeText(e.target.checked)} />
          <label htmlFor="includeText">Include Shape Text</label>
        </div>

        <div className="flex items-center">
          <input type="checkbox" className="rounded-sm mr-2" id="includePropertyRows" checked={includePropertyRows} onChange={(e) => setIncludePropertyRows(e.target.checked)} />
          <label htmlFor="includePropertyRows">Include Shape Properties</label>
        </div>

        <div className="flex items-center">
          <input type="checkbox" className="rounded-sm mr-2" id="includeShapeFields" checked={includeShapeFields} onChange={(e) => setincludeShapeFields(e.target.checked)} />
          <label htmlFor="includeShapeFields">Include Shape Fields</label>
        </div>

        <div className="flex items-center">
          <input type="checkbox" className="rounded-sm mr-2" id="includeUserRows" checked={includeUserRows} onChange={(e) => setIncludeUserRows(e.target.checked)} />
          <label htmlFor="includeUserRows">Include User Rows</label>
        </div>

        <div className="flex items-center">
          <input type="checkbox" className="rounded-sm mr-2" id="includeDocumentProperties" checked={includeDocumentProperties} onChange={(e) => setIncludeDocumentProperties(e.target.checked)} />
          <label htmlFor="includeDocumentProperties">Include Document Properties</label>
        </div>

        <div className="flex items-center">
          <input type="checkbox" className="rounded-sm mr-2" id="includeMasters" checked={includeMasters} onChange={(e) => setIncludeMasters(e.target.checked)} />
          <label htmlFor="includeMasters">Include Masters</label>
        </div>

        <div className="flex items-center">
          <input type="checkbox" className="rounded-sm mr-2" id="includeTextOnly" checked={translatableOnly} onChange={(e) => setTranslatableOnly(e.target.checked)} />
          <label htmlFor="includeTextOnly">Translatable Text Only</label>
        </div>

      </div>

      <hr className="my-4" />

      <PrimaryButton disabled={!vsdx || !!processing || loading} onClick={onJsonExport}>{processing || "Extract JSON"}</PrimaryButton>
    </>
  );
}
