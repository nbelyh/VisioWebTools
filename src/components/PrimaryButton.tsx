export const PrimaryButton = (props: { 
  disabled: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) => (
  <button 
    onClick={props.onClick} 
    disabled={props.disabled} 
    className="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded no-underline disabled:bg-blue-300">{props.children}</button>
);