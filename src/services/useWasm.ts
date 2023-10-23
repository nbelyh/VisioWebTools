import { useEffect, useState } from 'react';

export const useWasm = () => {

  const url = import.meta.env.DEV 
    ? '/visiowebtools-wasm/bin/Debug/net7.0/browser-wasm/AppBundle/dotnet.js'
    : '/AppBundle/dotnet.js';

  const [loading, setLoading] = useState(true);
  const [wasm, setWasm] = useState<any>(null);

  const loadWasm = async (): Promise<any> => {
    const module = await import(url);
    const { getAssemblyExports, getConfig } = await module
      .dotnet
      .withDiagnosticTracing(false)
      .create();

    const config = getConfig();
    const exports = await getAssemblyExports(config.mainAssemblyName);
    return exports;
  }

  useEffect(() => {
    loadWasm()
      .then(exports => setWasm(exports))
      .finally(() => setLoading(false))
  }, []);
  return { loading, wasm }
}