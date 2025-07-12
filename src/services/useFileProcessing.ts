import { useState } from 'react';
import { stringifyError } from './parse';
import { downloadBlob } from './downloadUtils';

export const useFileProcessing = () => {
  const [processing, setProcessing] = useState('');
  const [error, setError] = useState('');

  const processFile = async (
    processingFunction: () => Promise<Blob>,
    options: { processingMessage: string; fileName: string; }
  ) => {
    setError('');
    
    try {
      setProcessing(options.processingMessage);
      const blob = await processingFunction();
      downloadBlob(blob, options.fileName);
    } catch (e: any) {
      setError(stringifyError(e));
    } finally {
      setProcessing('');
    }
  };

  return {
    processFile,
    processing,
    error,
    setError
  };
};
