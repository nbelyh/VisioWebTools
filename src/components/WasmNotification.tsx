export const WasmNotification = (props: {
  loading: boolean;
  wasm: boolean;
}) => {
  
  if (props.loading)
    return null;

  return props.wasm
    ? <div className="flex">
      <div className="my-3 bg-green-100 p-4 w-5/6">
        <strong>We will be using web assembly to process files, your browser supports it!</strong>
        <div>Your files will not leave your browser, all the processing will be done locally on your machine in this browser.</div>
      </div>
    </div>
    : <div className="flex">
      <div className="my-3 bg-yellow-100 p-4 w-5/6">
        <strong>We are unable to use web assembly, your browser does not seem to support it..</strong>
        <div>We will not be able to process your files locally in the browser, but we are still able to process them on our server. Your files will not be saved anywhere.</div>
      </div>
    </div>
}