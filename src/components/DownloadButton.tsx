import { useEffect, useRef } from 'react';

export const DownloadButton = (props: {
  downloadUrl: string;
  fileName?: string;
  children: React.ReactNode;
}) => {

  const downloadUrlRef = useRef<HTMLAnchorElement>(null);

  useEffect(() => {
    if (downloadUrlRef.current)
      downloadUrlRef.current.click();
  }, [props.downloadUrl]);
  return (
    <a ref={downloadUrlRef}
      href={props.downloadUrl}
      target='_blank'
      download={props.fileName}
      className="bg-green-800 cursor-pointer hover:bg-green-900 text-white font-bold py-2 px-4 rounded no-underline disabled:bg-green-300 inline-block">{props.children}</a>
  );
}