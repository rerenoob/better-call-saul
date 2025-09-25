import React from 'react';
import { useDropzone } from 'react-dropzone';
import { MAX_FILE_SIZE } from '../../types/upload';

interface DragDropZoneProps {
  onFilesSelected: (files: File[]) => void;
  disabled?: boolean;
  maxFiles?: number;
}

export const DragDropZone: React.FC<DragDropZoneProps> = ({
  onFilesSelected,
  disabled = false,
  maxFiles = 10,
}) => {
  const { getRootProps, getInputProps, isDragActive, isDragReject, fileRejections } = useDropzone({
    accept: {
      'application/pdf': ['.pdf'],
      'application/msword': ['.doc'],
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document': ['.docx'],
      'text/plain': ['.txt'],
    },
    maxSize: MAX_FILE_SIZE,
    maxFiles,
    disabled,
    onDrop: acceptedFiles => {
      if (acceptedFiles.length > 0) {
        onFilesSelected(acceptedFiles);
      }
    },
  });

  const getBorderColor = () => {
    if (isDragReject) return 'border-red-400';
    if (isDragActive) return 'border-blue-500';
    return 'border-gray-300';
  };

  const getBackgroundColor = () => {
    if (isDragReject) return 'bg-red-50';
    if (isDragActive) return 'bg-blue-50';
    return 'bg-gray-50';
  };

  return (
    <div className="w-full">
      <div
        {...getRootProps()}
        className={`
          relative border-2 border-dashed rounded-lg p-8 text-center cursor-pointer transition-all duration-200
          ${getBorderColor()} ${getBackgroundColor()}
          ${disabled ? 'opacity-50 cursor-not-allowed' : 'hover:bg-gray-100'}
          min-h-[200px] flex flex-col items-center justify-center
        `}
      >
        <input {...getInputProps()} />

        {/* Upload Icon */}
        <div className="mb-4">
          <svg
            className={`mx-auto h-12 w-12 ${isDragActive ? 'text-blue-500' : 'text-gray-400'}`}
            stroke="currentColor"
            fill="none"
            viewBox="0 0 48 48"
          >
            <path
              d="M28 8H12a4 4 0 00-4 4v20m32-12v8m0 0v8a4 4 0 01-4 4H12a4 4 0 01-4-4v-4m32-4l-3.172-3.172a4 4 0 00-5.656 0L28 28M8 32l9.172-9.172a4 4 0 015.656 0L28 28m0 0l4 4m4-24h8m-4-4v8m-12 4h.02"
              strokeWidth={2}
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        </div>

        {/* Main Text */}
        <div className="space-y-2">
          <p className={`text-lg font-medium ${isDragActive ? 'text-blue-600' : 'text-gray-700'}`}>
            {isDragActive ? 'Drop files here' : 'Drag & drop files here, or'}
          </p>
          <button
            type="button"
            className="text-blue-600 hover:text-blue-700 underline font-medium"
            disabled={disabled}
          >
            click to select files
          </button>
        </div>

        {/* File Requirements */}
        <div className="mt-4 text-sm text-gray-500 space-y-1">
          <p>PDF, DOCX, TXT supported</p>
          <p>Maximum file size: 50MB</p>
          <p>Maximum {maxFiles} files</p>
        </div>
      </div>

      {/* Error Messages */}
      {fileRejections.length > 0 && (
        <div className="mt-4 p-3 bg-red-50 border border-red-200 rounded-md">
          <h4 className="text-sm font-medium text-red-800 mb-1">Some files were rejected:</h4>
          <ul className="text-sm text-red-700 space-y-1">
            {fileRejections.map(({ file, errors }) => (
              <li key={file.name} className="flex items-start">
                <span className="font-medium mr-2">{file.name}:</span>
                <div>
                  {errors.map(error => (
                    <div key={error.code}>
                      {error.code === 'file-too-large' && 'File is too large (max 50MB)'}
                      {error.code === 'file-invalid-type' &&
                        'Invalid file type (PDF, DOC, DOCX, TXT only)'}
                      {error.code === 'too-many-files' && `Too many files (max ${maxFiles})`}
                    </div>
                  ))}
                </div>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
};
