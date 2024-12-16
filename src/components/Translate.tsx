import { useState } from 'react';
import { DropZone } from './DropZone';
import { PrimaryButton } from './PrimaryButton';
import { AzureFunctionBackend } from '../services/AzureFunctionBackend';
import { useDotNetFixedUrl } from '../services/useDotNetFixedUrl';
import { ErrorNotification } from './ErrorNotification';
import { Languages } from './Languages';
import { stringifyError } from '../services/parse';

export const Translate = (props: {
}) => {

  const [vsdx, setVsdx] = useState<File>();
  const [apiKey, setApiKey] = useState('');
  const [error, setError] = useState('');
  const [processing, setProcessing] = useState('');

  const onFileChange = (file?: File) => {
    setVsdx(file);
  }

  const { dotnet, loading, loadError } = useDotNetFixedUrl();

  const getApiUrl = (key: string) => {
    return key
      ? "https://api.openai.com/v1/chat/completions"
      : location.hostname === 'localhost' 
        ? "http://localhost:7071/api/TranslateAzureFunction" 
        : "https://visiowebtools.azurewebsites.net/api/TranslateAzureFunction";
  }

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
      
      var apiUrl = getApiUrl(apiKey);
      const translated = await dotnet.FileProcessor.Translate(apiUrl, apiKey, original, targetLanguage);

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

    setProcessing('Translating...');
    try {
      const blob = await doProcessing(vsdx);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.target = "_blank";
      a.href = url;
      a.download = vsdx.name;
      a.click();
    } catch (e: any) {
      setError(stringifyError(e));
    } finally {
      setProcessing('');
    }
  }

  const [enableTranslateShapeText, setEnableTranslateShapeText] = useState(true);
  const [enableTranslateShapeFields, setEnableTranslateShapeFields] = useState(false);
  const [enableTranslatePageNames, setEnableTranslatePageNames] = useState(false);
  const [enableTranslatePropertyValues, setEnableTranslatePropertyValues] = useState(false);
  const [enableTranslatePropertyLabels, setEnableTranslatePropertyLabels] = useState(false);
  const [enableTranslateUserRows, setEnableTranslateUserRows] = useState(false);

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
          <select id="targetLanguage" className="ml-2 rounded-sm" value={targetLanguage} onChange={(e) => setTargetLanguage(e.target.value)}>
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
          <input type="checkbox" className="rounded-sm mr-2" id="enableTranslatePropertyValues" checked={enableTranslatePropertyValues} onChange={(e) => setEnableTranslatePropertyValues(e.target.checked)} />
          <label htmlFor="enableTranslatePropertyValues">Translate Text Properties</label>
        </div>

        <div className="flex items-center">
          <input type="checkbox" className="rounded-sm mr-2" id="enableTranslateUserRows" checked={enableTranslateUserRows} onChange={(e) => setEnableTranslateUserRows(e.target.checked)} />
          <label htmlFor="enableTranslateUserRows">Translate User Rows</label>
        </div>

        <div className="flex items-center">
          <input type="checkbox" className="rounded-sm mr-2" id="enableTranslatePropertyLabels" checked={enableTranslatePropertyLabels} onChange={(e) => setEnableTranslatePropertyLabels(e.target.checked)} />
          <label htmlFor="enableTranslatePropertyLabels">Translate Property Labels</label>
        </div>

        <div className="flex items-center">
          <input type="checkbox" className="rounded-sm mr-2" id="enableTranslatePageNames" checked={enableTranslatePageNames} onChange={(e) => setEnableTranslatePageNames(e.target.checked)} />
          <label htmlFor="enableTranslatePageNames">Translate Page Names</label>
        </div>

        <div className="flex items-center">
          <label htmlFor="apiKey" className="mr-2">Your own OpenAI API Key (optional)</label>
          <input type="text" id="apiKey" className="rounded-sm grow mt-2" value={apiKey} onChange={(e) => setApiKey(e.target.value)} />
        </div>

      </div>

      <hr className="my-4" />

      <PrimaryButton disabled={!vsdx || !!processing || loading} onClick={onTranslateFile}>{processing || "Translate"}</PrimaryButton>
    </>
  );
}
