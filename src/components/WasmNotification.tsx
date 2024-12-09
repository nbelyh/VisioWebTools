export const WasmNotification = (props: {
  loading: boolean;
  wasm: boolean;
}) => {
  
  if (props.loading)
    return null;

  return props.wasm
    ? <div className="flex">
      <div className="my-3 bg-green-100 p-4 w-5/6">
        <div>Your files will not leave your browser, all the processing will be done <strong>locally</strong> on your machine in this browser using web assemtly.</div>
      </div>
    </div>
    : <div className="flex">
      <div className="my-3 bg-yellow-100 p-4 w-5/6">
        <strong>We are unable to use web assembly</strong>
        <div>Either your browser support it, or there is some other issue.</div>
      </div>
    </div>
}