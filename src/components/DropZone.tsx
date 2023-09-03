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

  const dropZone: CSSProperties = {
    height: '150px',
    border: '2px dashed #6c757d',
    // background: '#f8f9fa',
    marginBottom: '1.5rem',
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    flexDirection: 'column',
    textAlign: 'center',
    transition: 'background-color 0.3s',
    // backgroundColor: dragging ? '#e8e8e8' : undefined
  }

  const dropzoneLink: CSSProperties = {
    padding: '10px',
    // background: '#f8f9fa',
    border: '1px solid #6c757d',
    borderRadius: '3px',
    boxShadow: '10px 10px 5px grey',
    cursor: 'move',
    textDecoration: 'none',
  }

  const dropzoneDiv: CSSProperties = {
    minHeight: '5em'
  }

  return (
    <div className="row">
      <div className="col-md-10">
        <div style={dropZone} className="bg-light" onDragOver={onDragOver} onDrop={onDrop} onDragLeave={onDragLeave}>
          {file
            ? <p><i style={{ color: 'green' }}>&#10004;</i> You have selected: <strong>{file.name}</strong></p>
            : <>
              <p>{props.label}</p>
              <button className="btn btn-secondary mt-2" onClick={onUploadButtonClick}>Or click here pick a file</button>
            </>
          }
        </div>
        <input ref={fileButtonRef} type="file" accept={props.accept} style={{ display: 'none' }} onChange={onFileChange} />
      </div>
      <div className="col-md-2 mt-2" style={dropzoneDiv}>
        <a href={props.sampleFileName} onDragStart={onDragStart} draggable="true" style={dropzoneLink}>&#128196;&nbsp;{props.sampleFileName}</a>
      </div>
    </div>
  );
}