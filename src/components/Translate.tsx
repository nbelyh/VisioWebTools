import { useState } from 'react';
import { DropZone } from './DropZone';
import { PrimaryButton } from './PrimaryButton';
import { AzureFunctionBackend } from '../services/AzureFunctionBackend';
import { useDotNetFixedUrl } from '../services/useDotNetFixedUrl';
import { ErrorNotification } from './ErrorNotification';
import { Languages } from './Languages';
import { stringifyError } from '../services/parse';
import { CheckboxField, SelectField, TextField } from './FormFields';
import { downloadBlob } from '../services/downloadUtils';

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

    try {
      setProcessing('Translating...');
      const blob = await doProcessing(vsdx);
      downloadBlob(blob, vsdx.name);
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

        <SelectField
          id="targetLanguage"
          label="Target Language"
          value={targetLanguage}
          options={Languages}
          onChange={setTargetLanguage}
        />

        <CheckboxField
          id="enableTranslateText"
          label="Translate Shape Text"
          checked={enableTranslateShapeText}
          onChange={setEnableTranslateShapeText}
        />

        <CheckboxField
          id="enableTranslatePropertyValues"
          label="Translate Text Properties"
          checked={enableTranslatePropertyValues}
          onChange={setEnableTranslatePropertyValues}
        />

        <CheckboxField
          id="enableTranslateShapeFields"
          label="Translate Text Fields"
          checked={enableTranslateShapeFields}
          onChange={setEnableTranslateShapeFields}
        />

        <CheckboxField
          id="enableTranslateUserRows"
          label="Translate Text User Rows"
          checked={enableTranslateUserRows}
          onChange={setEnableTranslateUserRows}
        />

        <CheckboxField
          id="enableTranslatePropertyLabels"
          label="Translate Property Labels"
          checked={enableTranslatePropertyLabels}
          onChange={setEnableTranslatePropertyLabels}
        />

        <CheckboxField
          id="enableTranslatePageNames"
          label="Translate Page Names"
          checked={enableTranslatePageNames}
          onChange={setEnableTranslatePageNames}
        />

        <TextField
          id="apiKey"
          label="Your own OpenAI API Key (optional)"
          value={apiKey}
          onChange={setApiKey}
        />

      </div>

      <hr className="my-4" />

      <PrimaryButton disabled={!vsdx || !!processing || loading} onClick={onTranslateFile}>{processing || "Translate"}</PrimaryButton>
    </>
  );
}
