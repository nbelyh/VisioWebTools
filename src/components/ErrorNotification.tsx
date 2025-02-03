import { stringifyError } from '../services/parse';

export const ErrorNotification = (props: {
  error?: string;
}) => {

  if (!props.error)
    return null;

  return (
    <div className="flex">
      <div className="my-3 bg-red-100 p-4 w-5/6">
        <strong>Упс! Что-то пошло не так.</strong>
        <div>
        Пожалуйста, убедитесь, что вы выбрали файл VSDX (не VSD), или перезагрузите страницу и попробуйте снова.
        </div>
        <div><small>{stringifyError(props.error)}</small></div>
      </div>
    </div>
  );
}