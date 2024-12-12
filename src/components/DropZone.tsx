import { type CSSProperties, useState, useRef, type ChangeEvent, type DragEvent } from 'react';

export const DropZone = (props: {
  accept: string;
  label: string;
  sampleFileName: string;
  onChange: (file?: File) => void;
}) => {

  const fileButtonRef = useRef<HTMLInputElement | null>(null);
  const [file, _setFile] = useState<File>();
  const setFile = (file?: File) => {
    _setFile(file);
    props.onChange(file);
  }
  const [dragging, setDragging] = useState(false);

  const onUploadButtonClick = () => {
    if (fileButtonRef.current) {
      fileButtonRef.current.click();
    }
  }

  const onDrop = async (event: DragEvent) => {
    event.preventDefault();
    event.stopPropagation();

    var data = event.dataTransfer.getData("text");
    if (data === 'sample') {
      const response = await fetch(`/samples/${props.sampleFileName}`);
      const blob = await response.blob();
      const newFile = new File([blob], props.sampleFileName, { type: props.accept });
      setFile(newFile);
    } else {
      const newFile = event.dataTransfer.files[0];
      setFile(newFile);
    }
  }

  const onFileChange = (event: ChangeEvent) => {
    const target = event.target as HTMLInputElement;
    if (target?.files) {
      const newFile = target.files[0];
      setFile(newFile);
    }
  }

  const onDragStart = (event: DragEvent) => {
    event.dataTransfer.setData("text", 'sample');
  }

  const onDragOver = (event: DragEvent) => {
    event.preventDefault();
    event.stopPropagation();
    const target = event.target as HTMLElement;
    if (target) {
      setDragging(true);
    }
  }

  const onDragLeave = (event: DragEvent) => {
    event.preventDefault();
    event.stopPropagation();
    const target = event.target as HTMLElement;
    if (target) {
      setDragging(false);
    }
  }

  const className = `h-48 flex items-center justify-center border-dashed ${dragging ? 'bg-neutral-200' : 'bg-neutral-100'} border-neutral-300 border-2  text-center`;

  return (
    <div className="flex mb-4">
      <div className="md:w-5/6">
        <div className={className} onDragOver={onDragOver} onDrop={onDrop} onDragLeave={onDragLeave}>
          {file
            ? <div>
                <p>&#10004; You have selected: <strong>{file.name}</strong></p>
                <input type="button" value="Reset" onClick={() => setFile()} className="inline-flex items-center bg-neutral-300 hover:bg-neutral-400 text-neutral-900 px-4 py-1 rounded focus:outline-none" />
              </div>
            : <div>
              <p>{props.label}</p>
              <button className="inline-flex items-center bg-neutral-300 hover:bg-neutral-400 text-neutral-900 px-4 py-1 rounded focus:outline-none" onClick={onUploadButtonClick}>Or click here pick a file...</button>
            </div>
          }
        </div>
        <input ref={fileButtonRef} type="file" accept={props.accept} style={{ display: 'none' }} onChange={onFileChange} />
      </div>
      <div className="md:w-1/6 p-4">
        <a href={`samples/${props.sampleFileName}`} onDragStart={onDragStart} draggable="true" className="p-3 bg-neutral-100 border-2 border-neutral-300 no-underline shadow-xl rounded cursor-move">
          &#128196;&nbsp;<span className='underline cursor-pointer' title='Click to download, drag to the area on the left to use'>{props.sampleFileName}</span>
        </a>
      </div>
    </div>
  );
}