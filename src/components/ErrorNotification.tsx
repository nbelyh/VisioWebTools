import { stringifyError } from '../services/parse';

export const ErrorNotification = (props: {
  error?: string;
}) => {

  if (!props.error)
    return null;

  return (
    <div className="flex">
      <div className="my-3 bg-red-100 p-4 w-5/6">
      <strong>Ups! Something went wrong</strong>.
        Please make sure you have selected the VSDX (not VSD) file,or reload the page and try again.
        If it the problem persists, please report an issue to our <a href="https://github.com/nbelyh/visiopdftip-webapp/issues" target="_blank">GitHub</a>
        <div><small>{stringifyError(props.error)}</small></div>
      </div>
    </div>
  );
}