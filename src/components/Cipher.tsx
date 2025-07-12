import { useState } from 'react';
import { DropZone } from './DropZone';
import { PrimaryButton } from './PrimaryButton';
import { AzureFunctionBackend } from '../services/AzureFunctionBackend';
import { useDotNetFixedUrl } from '../services/useDotNetFixedUrl';
import { ErrorNotification } from './ErrorNotification';
import { useFileProcessing } from '../services/useFileProcessing';
import { CheckboxField } from './FormFields';

export const Cipher = (props: {
}) => {

  const [vsdx, setVsdx] = useState<File>();
  const { processing, error, processFile, setError } = useFileProcessing();

  const onFileChange = (file?: File) => {
    setVsdx(file);
  }

  const { dotnet, loading, loadError } = useDotNetFixedUrl();

  const doProcessing = async (vsdx: File) => {
    var options = {
      enableCipherShapeText,
      enableCipherShapeFields,
      enableCipherPageNames,
      enableCipherPropertyValues,
      enableCipherPropertyLabels,
      enableCipherMasters,
      enableCipherUserRows,
      enableCipherDocumentProperties
    };
    const optionsJson = JSON.stringify(options);
    if (dotnet) {
      var ab = new Uint8Array(await vsdx.arrayBuffer());
      const output: Uint8Array = dotnet.FileProcessor.CipherFile(ab, optionsJson)
      return new Blob([output], { type: 'application/vnd.ms-visio.drawing' });
    } else {
      return await AzureFunctionBackend.invoke({ vsdx, optionsJson }, 'CipherFileAzureFunction');
    }
  }

  const onCipherFile = async () => {

    if (typeof window.appInsights !== 'undefined') {
      window.appInsights.trackEvent({ name: "SplitPagesClicked" });
    }

    if (!vsdx) {
      setError('Please select the VSDX file');
      return;
    }

    await processFile(() => doProcessing(vsdx), {
      processingMessage: 'Ciphering...',
      fileName: vsdx.name
    });
  }

  const [enableCipherShapeText, setEnableCipherShapeText] = useState(true);
  const [enableCipherShapeFields, setEnableCipherShapeFields] = useState(true);
  const [enableCipherPageNames, setEnableCipherPageNames] = useState(true);
  const [enableCipherPropertyValues, setEnableCipherPropertyValues] = useState(true);
  const [enableCipherPropertyLabels, setEnableCipherPropertyLabels] = useState(false);
  const [enableCipherMasters, setEnableCipherMasters] = useState(false);
  const [enableCipherUserRows, setEnableCipherUserRows] = useState(false);
  const [enableCipherDocumentProperties, setEnableCipherDocumentProperties] = useState(false);

  return (
    <>
      <ErrorNotification error={error || loadError} />
      <DropZone
        accept="application/vnd.ms-visio.drawing"
        sampleFileName="CipherMe.vsdx"
        label="Drop the Visio VSDX file to cipher here"
        onChange={onFileChange}
      />

      <div className='mb-4 flex'>
        <div className='w-1/2'>
          <CheckboxField
            id="enableCipherText"
            label="Cipher Shape Text"
            checked={enableCipherShapeText}
            onChange={setEnableCipherShapeText}
          />

          <CheckboxField
            id="enableCipherShapeFields"
            label="Cipher Shape Text Fields"
            checked={enableCipherShapeFields}
            onChange={setEnableCipherShapeFields}
          />

          <CheckboxField
            id="enableCipherPageNames"
            label="Cipher Page Names"
            checked={enableCipherPageNames}
            onChange={setEnableCipherPageNames}
          />

          <CheckboxField
            id="enableCipherPropertyValues"
            label="Cipher Properties"
            checked={enableCipherPropertyValues}
            onChange={setEnableCipherPropertyValues}
          />

        </div>
        <div className='w-1/2'>
          <CheckboxField
            id="enableCipherUserRows"
            label="Cipher User Rows"
            checked={enableCipherUserRows}
            onChange={setEnableCipherUserRows}
          />

          <CheckboxField
            id="enableCipherDocumentProperties"
            label="Cipher Document Properties"
            checked={enableCipherDocumentProperties}
            onChange={setEnableCipherDocumentProperties}
          />

          <CheckboxField
            id="enableCipherPropertyLabels"
            label="Cipher Property Labels"
            checked={enableCipherPropertyLabels}
            onChange={setEnableCipherPropertyLabels}
          />

          <CheckboxField
            id="enableCipherMasters"
            label="Cipher Masters"
            checked={enableCipherMasters}
            onChange={setEnableCipherMasters}
          />
        </div>
      </div>

      <hr className="my-4" />
      <PrimaryButton disabled={!vsdx || !!processing || loading} onClick={onCipherFile}>{processing || "Cipher"}</PrimaryButton>
    </>
  );
}
