import { useState } from 'react';
import { DropZone } from './DropZone';
import { PrimaryButton } from './PrimaryButton';
import { AzureFunctionBackend } from '../services/AzureFunctionBackend';
import { useDotNetFixedUrl } from '../services/useDotNetFixedUrl';
import { ErrorNotification } from './ErrorNotification';
import { Languages } from './Languages';

export const TranslateFile = (props: {
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
      enableTranslateShapeText,
      enableTranslateShapeFields,
      enableTranslatePageNames,
      enableTranslatePropertyValues,
      enableTranslatePropertyLabels
    };
    const optionsJson = JSON.stringify(options);
    if (dotnet) {
      var ab = new Uint8Array(await vsdx.arrayBuffer());
      const original = dotnet.FileProcessor.GetTranslationJson(ab, optionsJson);
      const translated = await dotnet.FileProcessor.Translate(original, targetLanguage);
      console.log(translated);
      const result = dotnet.FileProcessor.ApplyTranslationJson(ab, optionsJson, translated);
      return new Blob([result], { type: 'application/vnd.ms-visio.drawing' });
    } else {
      return await AzureFunctionBackend.invoke({ vsdx, optionsJson }, 'TranslateFileAzureFunction');
    }
  }

  const onTranslateFile = async () => {

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

  const [enableTranslateShapeText, setEnableTranslateShapeText] = useState(true);
  const [enableTranslateShapeFields, setEnableTranslateShapeFields] = useState(false);
  const [enableTranslatePageNames, setEnableTranslatePageNames] = useState(false);
  const [enableTranslatePropertyValues, setEnableTranslatePropertyValues] = useState(false);
  const [enableTranslatePropertyLabels, setEnableTranslatePropertyLabels] = useState(false);

  const [targetLanguage, setTargetLanguage] = useState('German');

  return (
    <>
      <ErrorNotification error={error || loadError} />
      <DropZone
        accept="application/vnd.ms-visio.drawing"
        sampleFileName="TranslateMe.vsdx"
        label="Drop the Visio VSDX file to translate here"
        onChange={onFileChange}
      />

      <div className='mb-4'>

        <div className="flex items-center mb-2">
          <label htmlFor="targetLanguage">Target Language</label>
          <select id="targetLanguage" className="ml-2" value={targetLanguage} onChange={(e) => setTargetLanguage(e.target.value)}>
            {Languages.map(l => <option key={l}>{l}</option>)}
          </select>
        </div>

        <div className="flex items-center">
          <input type="checkbox" className="rounded-sm mr-2" id="enableTranslateText" checked={enableTranslateShapeText} onChange={(e) => setEnableTranslateShapeText(e.target.checked)} />
          <label htmlFor="enableTranslateText">Translate Shape Text</label>
        </div>

        <div className="flex items-center">
          <input type="checkbox" className="rounded-sm mr-2" id="enableTranslateShapeFields" checked={enableTranslateShapeFields} onChange={(e) => setEnableTranslateShapeFields(e.target.checked)} />
          <label htmlFor="enableTranslateShapeFields">Translate Shape Fields</label>
        </div>

        <div className="flex items-center">
          <input type="checkbox" className="rounded-sm mr-2" id="enableTranslatePageNames" checked={enableTranslatePageNames} onChange={(e) => setEnableTranslatePageNames(e.target.checked)} />
          <label htmlFor="enableTranslatePageNames">Translate Page Names</label>
        </div>

        <div className="flex items-center">
          <input type="checkbox" className="rounded-sm mr-2" id="enableTranslatePropertyValues" checked={enableTranslatePropertyValues} onChange={(e) => setEnableTranslatePropertyValues(e.target.checked)} />
          <label htmlFor="enableTranslatePropertyValues">Translate Property Values</label>
        </div>

        <div className="flex items-center">
          <input type="checkbox" className="rounded-sm mr-2" id="enableTranslatePropertyLabels" checked={enableTranslatePropertyLabels} onChange={(e) => setEnableTranslatePropertyLabels(e.target.checked)} />
          <label htmlFor="enableTranslatePropertyLabels">Translate Property Labels</label>
        </div>
      </div>

      <hr className="my-4" />

      <PrimaryButton disabled={!vsdx || processing || loading} onClick={onTranslateFile}>Translate</PrimaryButton>
    </>
  );
}
