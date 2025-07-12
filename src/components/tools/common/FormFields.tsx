export const CheckboxField = ({ id, label, checked, onChange }: {
  id: string;
  label: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
}) => (
  <div className="flex-items-center">
    <input 
      type="checkbox" 
      className="form-checkbox" 
      id={id} 
      checked={checked} 
      onChange={(e) => onChange(e.target.checked)} 
    />
    <label htmlFor={id}>{label}</label>
  </div>
);

export const SelectField = ({ id, label, value, options, onChange }: {
  id: string;
  label: string;
  value: string;
  options: string[];
  onChange: (value: string) => void;
}) => (
  <label className="block">
    <span className="text-neutral-700">{label}</span>
    <select id={id} className="form-select mt-1 block w-full" value={value} onChange={(e) => onChange(e.target.value)}>
      {options.map(option => <option key={option}>{option}</option>)}
    </select>
  </label>
);

export const TextField = ({ id, label, value, onChange, type = 'text', placeholder }: {
  id: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: 'text' | 'password' | 'email' | 'number';
  placeholder?: string;
}) => (
  <label className="block">
    <span className="text-neutral-700">{label}</span>
    <input 
      type={type} 
      id={id} 
      className="form-input mt-1 block w-full"
      value={value} 
      onChange={(e) => onChange(e.target.value)}
      placeholder={placeholder}
    />
  </label>
);

export const ColorField = ({ id, label, value, onChange }: {
  id: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
}) => (
  <label className="block">
    <span className="text-neutral-700">{label}</span>
    <input 
      type="color" 
      id={id} 
      className="form-input mt-1 block w-full h-10"
      value={value} 
      onChange={(e) => onChange(e.target.value)}
    />
  </label>
);
