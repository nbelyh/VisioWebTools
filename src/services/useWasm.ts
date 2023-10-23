import { useEffect, useState } from 'react';

export const useWasm = () => {
  const [loading, setLoading] = useState(true);
  const [wasm, setWasmRef] = useState<any>(null);
  useEffect(() => {
    const visioWebToolsDotNet = (window as any)?.VisioWebToolsDotNet;
    if (visioWebToolsDotNet) {
      visioWebToolsDotNet.load().then((dotnet: any) => {
          setWasmRef(dotnet)
        }, (e: any) => {
          setWasmRef(null);
          console.error(e)
        }).finally(() => {
          setLoading(false);
        })
    }
  }, []);
  return { loading, wasm }
}