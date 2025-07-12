export const PrimaryButton = (props: { 
  disabled: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) => (
  <button 
    onClick={props.onClick} 
    disabled={props.disabled} 
    className="btn-primary">{props.children}</button>
);