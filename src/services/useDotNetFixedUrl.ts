import { useDotNet } from './useDotNet';

export const useDotNetFixedUrl = () => {

  const url = import.meta.env.DEV
    ? '/visiowebtools-wasm/bin/Debug/net7.0/browser-wasm/AppBundle/dotnet.js'
    : '/AppBundle/_framework/dotnet.js';

  return useDotNet(url);
}