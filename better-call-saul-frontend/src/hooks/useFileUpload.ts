import { useState, useCallback } from 'react';
import { UploadFile, UploadStatus } from '../types/upload';
import { fileUploadService } from '../services/fileUploadService';

export const useFileUpload = () => {
  const [files, setFiles] = useState<UploadFile[]>([]);
  const [isUploading, setIsUploading] = useState(false);

  const generateFileId = () => Math.random().toString(36).substr(2, 9);

  const addFiles = useCallback((newFiles: File[]) => {
    const validatedFiles: UploadFile[] = [];
    
    newFiles.forEach(file => {
      const validation = fileUploadService.validateFile(file);
      
      if (validation.isValid) {
        validatedFiles.push({
          id: generateFileId(),
          file,
          name: file.name,
          size: file.size,
          type: file.type,
          uploadProgress: 0,
          status: UploadStatus.PENDING
        });
      }
    });

    setFiles(prev => [...prev, ...validatedFiles]);
    return validatedFiles.length;
  }, []);

  const removeFile = useCallback((fileId: string) => {
    setFiles(prev => prev.filter(f => f.id !== fileId));
  }, []);

  const updateFileProgress = useCallback((fileId: string, progress: number, status: UploadStatus) => {
    setFiles(prev => prev.map(file => 
      file.id === fileId 
        ? { ...file, uploadProgress: progress, status }
        : file
    ));
  }, []);

  const uploadFiles = useCallback(async () => {
    if (files.length === 0) return { success: true, fileIds: [] };

    setIsUploading(true);
    const uploadResults: string[] = [];
    
    try {
      for (const file of files) {
        if (file.status === UploadStatus.COMPLETED) {
          continue;
        }

        updateFileProgress(file.id, 0, UploadStatus.UPLOADING);

        const result = await fileUploadService.uploadFile(
          file.file,
          (progress) => updateFileProgress(file.id, progress, UploadStatus.UPLOADING)
        );

        if (result.success && result.fileId) {
          updateFileProgress(file.id, 100, UploadStatus.COMPLETED);
          uploadResults.push(result.fileId);
        } else {
          updateFileProgress(file.id, 0, UploadStatus.ERROR);
          setFiles(prev => prev.map(f => 
            f.id === file.id 
              ? { ...f, error: result.error, status: UploadStatus.ERROR }
              : f
          ));
        }
      }

      return { success: true, fileIds: uploadResults };
    } catch (error) {
      return { success: false, error: 'Failed to upload files', fileIds: [] };
    } finally {
      setIsUploading(false);
    }
  }, [files, updateFileProgress]);

  const retryFailedUploads = useCallback(async () => {
    const failedFiles = files.filter(f => f.status === UploadStatus.ERROR);
    
    for (const file of failedFiles) {
      updateFileProgress(file.id, 0, UploadStatus.UPLOADING);
      
      const result = await fileUploadService.uploadFile(
        file.file,
        (progress) => updateFileProgress(file.id, progress, UploadStatus.UPLOADING)
      );

      if (result.success) {
        updateFileProgress(file.id, 100, UploadStatus.COMPLETED);
      } else {
        updateFileProgress(file.id, 0, UploadStatus.ERROR);
        setFiles(prev => prev.map(f => 
          f.id === file.id 
            ? { ...f, error: result.error, status: UploadStatus.ERROR }
            : f
        ));
      }
    }
  }, [files, updateFileProgress]);

  const clearFiles = useCallback(() => {
    setFiles([]);
  }, []);

  const getUploadStats = useCallback(() => {
    const total = files.length;
    const completed = files.filter(f => f.status === UploadStatus.COMPLETED).length;
    const failed = files.filter(f => f.status === UploadStatus.ERROR).length;
    const pending = files.filter(f => f.status === UploadStatus.PENDING).length;
    const uploading = files.filter(f => f.status === UploadStatus.UPLOADING).length;

    return { total, completed, failed, pending, uploading };
  }, [files]);

  return {
    files,
    isUploading,
    addFiles,
    removeFile,
    uploadFiles,
    retryFailedUploads,
    clearFiles,
    getUploadStats
  };
};