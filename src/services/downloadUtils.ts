/**
 * Triggers a file download using a blob
 */
export const downloadBlob = (blob: Blob, filename: string): void => {
  const url = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.download = filename;
  a.target = "_blank";
  a.href = url;
  a.click();
  
  // Clean up the object URL after download
  setTimeout(() => {
    window.URL.revokeObjectURL(url);
  }, 100);
};
