import { useEffect, useRef, useState } from 'react';

export const useDotNet = (url: string) => {

  const dotnetUrl = useRef('');
  const [dotnet, setDotNet] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  const load = async (currentUrl: string): Promise<any> => {

    // throw new Error('Not implemented');

    const module = await import(/* @vite-ignore */ currentUrl);

    const { getAssemblyExports, getConfig } = await module
      .dotnet
      .withDiagnosticTracing(false)
      .create();

    const config = getConfig();
    const exports = await getAssemblyExports(config.mainAssemblyName);
    return exports;
  }

  useEffect(() => {
    if (dotnetUrl.current !== url) { // safeguard to prevent double-loading
      setLoading(true);
      dotnetUrl.current = url;
      load(url)
        .then(exports => setDotNet(exports))
        .finally(() => setLoading(false))
    }
  }, [url]);
  return { dotnet, loading };
}
