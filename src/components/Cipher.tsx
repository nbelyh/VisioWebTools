import { useState } from 'react';
import { DropZone } from './DropZone';
import { PrimaryButton } from './PrimaryButton';
import { AzureFunctionBackend } from '../services/AzureFunctionBackend';
import { useDotNetFixedUrl } from '../services/useDotNetFixedUrl';
import { ErrorNotification } from './ErrorNotification';
import { stringifyError } from '../services/parse';
import { DownloadButton } from './DownloadButton';

export const Cipher = (props: {
}) => {

  const [vsdx, setVsdx] = useState<File>();
  const [error, setError] = useState('');
  const [processing, setProcessing] = useState('');

  const onFileChange = (file?: File) => {
    setDownloadUrl(undefined);
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

    setError('');

    if (!vsdx) {
      setError('Пожалуйста, выберите файл VSDX');
      return;
    }

    setProcessing('Шифрование...');
    try {
      const blob = await doProcessing(vsdx);
      const url = window.URL.createObjectURL(blob);
      setDownloadUrl(url);
    } catch (e: any) {
      setError(stringifyError(e));
    } finally {
      setProcessing('');
    }
  }

  const [enableCipherShapeText, setEnableCipherShapeText] = useState(true);
  const [enableCipherShapeFields, setEnableCipherShapeFields] = useState(true);
  const [enableCipherPageNames, setEnableCipherPageNames] = useState(true);
  const [enableCipherPropertyValues, setEnableCipherPropertyValues] = useState(true);
  const [enableCipherPropertyLabels, setEnableCipherPropertyLabels] = useState(false);
  const [enableCipherMasters, setEnableCipherMasters] = useState(false);
  const [enableCipherUserRows, setEnableCipherUserRows] = useState(false);
  const [enableCipherDocumentProperties, setEnableCipherDocumentProperties] = useState(false);

  const [downloadUrl, setDownloadUrl] = useState<string>();

  return (
    <>
      <ErrorNotification error={error || loadError} />
      <DropZone
        accept="application/vnd.ms-visio.drawing"
        sampleFileName="Пример.vsdx"
        label="Перетащите сюда файл Visio VSDX для шифрования"
        onChange={onFileChange}
      />

      <div className='mb-4 flex'>
        <div className='w-1/2'>
          <div className="flex items-center">
            <input type="checkbox" className="rounded-sm mr-2" id="enableCipherText" checked={enableCipherShapeText} onChange={(e) => setEnableCipherShapeText(e.target.checked)} />
            <label htmlFor="enableCipherText">Шифровать текст фигур</label>
          </div>

          <div className="flex items-center">
            <input type="checkbox" className="rounded-sm mr-2" id="enableCipherShapeFields" checked={enableCipherShapeFields} onChange={(e) => setEnableCipherShapeFields(e.target.checked)} />
            <label htmlFor="enableCipherShapeFields">Шифровать текстовые поля фигур</label>
          </div>

          <div className="flex items-center">
            <input type="checkbox" className="rounded-sm mr-2" id="enableCipherPageNames" checked={enableCipherPageNames} onChange={(e) => setEnableCipherPageNames(e.target.checked)} />
            <label htmlFor="enableCipherPageNames">Шифровать названия страниц</label>
          </div>

          <div className="flex items-center">
            <input type="checkbox" className="rounded-sm mr-2" id="enableCipherPropertyValues" checked={enableCipherPropertyValues} onChange={(e) => setEnableCipherPropertyValues(e.target.checked)} />
            <label htmlFor="enableCipherPropertyValues">Шифровать свойства</label>
          </div>
        </div>
        <div className='w-1/2'>
          <div className="flex items-center">
            <input type="checkbox" className="rounded-sm mr-2" id="enableCipherUserRows" checked={enableCipherUserRows} onChange={(e) => setEnableCipherUserRows(e.target.checked)} />
            <label htmlFor="enableCipherUserRows">Шифровать пользовательские строки</label>
          </div>

          <div className="flex items-center">
            <input type="checkbox" className="rounded-sm mr-2" id="enableCipherDocumentProperties" checked={enableCipherDocumentProperties} onChange={(e) => setEnableCipherDocumentProperties(e.target.checked)} />
            <label htmlFor="enableCipherDocumentProperties">Шифровать свойства документа</label>
          </div>

          <div className="flex items-center">
            <input type="checkbox" className="rounded-sm mr-2" id="enableCipherPropertyLabels" checked={enableCipherPropertyLabels} onChange={(e) => setEnableCipherPropertyLabels(e.target.checked)} />
            <label htmlFor="enableCipherPropertyLabels">Шифровать метки свойств</label>
          </div>

          <div className="flex items-center">
            <input type="checkbox" className="rounded-sm mr-2" id="enableCipherMasters" checked={enableCipherMasters} onChange={(e) => setEnableCipherMasters(e.target.checked)} />
            <label htmlFor="enableCipherMasters">Шифровать мастера</label>
          </div>
        </div>

      </div>

      <hr className="my-4" />
      {downloadUrl
        ? <DownloadButton downloadUrl={downloadUrl} fileName={vsdx?.name}>Скачать результат</DownloadButton>
        : <PrimaryButton disabled={!vsdx || !!processing || loading} onClick={onCipherFile}>{processing || "Шифровать"}</PrimaryButton>
      }
    </>
  );
}
