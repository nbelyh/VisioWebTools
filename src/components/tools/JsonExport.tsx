import { useState } from 'react';
import { DropZone } from './common/DropZone';
import { PrimaryButton } from './common/PrimaryButton';
import { AzureFunctionBackend } from 'services/AzureFunctionBackend';
import { useDotNetFixedUrl } from 'services/useDotNetFixedUrl';
import { ErrorNotification } from './common/ErrorNotification';
import { CheckboxField } from './common/FormFields';
import { useFileProcessing } from 'services/useFileProcessing';
import { useLocalStorage } from 'services/useLocalStorage';

export const JsonExport = (props: {
}) => {

  const [vsdx, setVsdx] = useState<File>();
  const { processing, error, processFile, setError } = useFileProcessing();

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
      includeEmptyShapes,
      includeConnectors,
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

    if (!vsdx) {
      setError('Please select the VSDX file');
      return;
    }

    await processFile(() => doProcessing(vsdx), {
      processingMessage: 'Extracting JSON...',
      fileName: vsdx.name.replace('.vsdx', '.json')
    });
  }

  const [includeShapeText, setincludeShapeText] = useLocalStorage('includeShapeText', true);
  const [includeShapeFields, setincludeShapeFields] = useLocalStorage('includeShapeFields', false);
  const [includePropertyRows, setIncludePropertyRows] = useLocalStorage('includePropertyRows', false);
  const [includeUserRows, setIncludeUserRows] = useLocalStorage('includeUserRows', false);
  const [includeMasters, setIncludeMasters] = useLocalStorage('includeMasters', false);
  const [includeDocumentProperties, setIncludeDocumentProperties] = useLocalStorage('includeDocumentProperties', false);
  const [includeEmptyShapes, setIncludeEmptyShapes] = useLocalStorage('includeEmptyShapes', false);
  const [includeConnectors, setIncludeConnectors] = useLocalStorage('includeConnectors', true);

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

        <CheckboxField
          id="includeText"
          label="Include Shape Text"
          checked={includeShapeText}
          onChange={setincludeShapeText}
        />

        <CheckboxField
          id="includePropertyRows"
          label="Include Shape Properties"
          checked={includePropertyRows}
          onChange={setIncludePropertyRows}
        />

        <CheckboxField
          id="includeShapeFields"
          label="Include Shape Fields"
          checked={includeShapeFields}
          onChange={setincludeShapeFields}
        />

        <CheckboxField
          id="includeUserRows"
          label="Include User Rows"
          checked={includeUserRows}
          onChange={setIncludeUserRows}
        />

        <CheckboxField
          id="includeDocumentProperties"
          label="Include Document Properties"
          checked={includeDocumentProperties}
          onChange={setIncludeDocumentProperties}
        />

        <CheckboxField
          id="includeMasters"
          label="Include Masters"
          checked={includeMasters}
          onChange={setIncludeMasters}
        />

        <CheckboxField
          id="includeEmptyShapes"
          label="Include Shapes with no data"
          checked={includeEmptyShapes}
          onChange={setIncludeEmptyShapes}
        />

        <CheckboxField
          id="includeConnectors"
          label="Include Connectors"
          checked={includeConnectors}
          onChange={setIncludeConnectors}
        />

      </div>

      <hr className="my-4" />

      <PrimaryButton disabled={!vsdx || !!processing || loading} onClick={onJsonExport}>{processing || "Extract JSON"}</PrimaryButton>
    </>
  );
}
