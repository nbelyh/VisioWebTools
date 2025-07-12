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
      className="btn-success">{props.children}</a>
  );
}