import { apiClient } from './apiClient';
import { FileUploadResponse, CaseCreationResponse, UploadFile } from '../types/upload';

export const fileUploadService = {
  uploadFile: async (
    file: File, 
    onProgress?: (progress: number) => void,
    caseId?: string,
    uploadSessionId?: string
  ): Promise<FileUploadResponse> => {
    const formData = new FormData();
    formData.append('file', file);
    
    // Use default values if not provided
    if (caseId) {
      formData.append('caseId', caseId);
    } else {
      // Use a temporary GUID for standalone uploads
      formData.append('caseId', '00000000-0000-0000-0000-000000000000');
    }
    
    if (uploadSessionId) {
      formData.append('uploadSessionId', uploadSessionId);
    } else {
      // Generate a unique session ID
      formData.append('uploadSessionId', `upload-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`);
    }

    try {
      const response = await apiClient.post<FileUploadResponse>('/api/fileupload/upload', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
        onUploadProgress: (progressEvent) => {
          if (progressEvent.total && onProgress) {
            const percentCompleted = Math.round((progressEvent.loaded * 100) / progressEvent.total);
            onProgress(percentCompleted);
          }
        },
      });

      return response.data;
    } catch (error: unknown) {
      return {
        success: false,
        fileId: '',
        fileName: file.name,
        error: error instanceof Error && 'response' in error && error.response && typeof error.response === 'object' && 'data' in error.response && error.response.data && typeof error.response.data === 'object' && 'message' in error.response.data ? (error.response.data as {message: string}).message : 'Failed to upload file',
      };
    }
  },

  uploadMultipleFiles: async (
    files: UploadFile[],
    onProgress?: (fileId: string, progress: number) => void
  ): Promise<FileUploadResponse[]> => {
    const uploadPromises = files.map(uploadFile => 
      fileUploadService.uploadFile(
        uploadFile.file,
        (progress) => onProgress?.(uploadFile.id, progress)
      )
    );

    return Promise.all(uploadPromises);
  },

  createCaseWithFiles: async (
    caseName: string,
    clientName: string,
    fileIds: string[]
  ): Promise<CaseCreationResponse> => {
    try {
      const response = await apiClient.post<CaseCreationResponse>('/api/case/create-with-files', {
        title: caseName,
        description: clientName ? `Client: ${clientName}` : undefined,
        fileIds,
      });

      return response.data;
    } catch (error: unknown) {
      return {
        success: false,
        caseId: '',
        error: error instanceof Error && 'response' in error && error.response && typeof error.response === 'object' && 'data' in error.response && error.response.data && typeof error.response.data === 'object' && 'message' in error.response.data ? (error.response.data as {message: string}).message : 'Failed to create case',
      };
    }
  },

  deleteFile: async (fileId: string): Promise<{ success: boolean; error?: string }> => {
    try {
      await apiClient.delete(`/api/documents/${fileId}`);
      return { success: true };
    } catch (error: unknown) {
      return {
        success: false,
        error: error instanceof Error && 'response' in error && error.response && typeof error.response === 'object' && 'data' in error.response && error.response.data && typeof error.response.data === 'object' && 'message' in error.response.data ? (error.response.data as {message: string}).message : 'Failed to delete file',
      };
    }
  },

  validateFile: (file: File): { isValid: boolean; error?: string } => {
    const allowedTypes = [
      'application/pdf',
      'application/msword',
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      'text/plain'
    ];

    const maxSize = 50 * 1024 * 1024; // 50MB

    if (!allowedTypes.includes(file.type)) {
      return {
        isValid: false,
        error: 'Only PDF, DOC, DOCX, and TXT files are allowed'
      };
    }

    if (file.size > maxSize) {
      return {
        isValid: false,
        error: 'File size must be less than 50MB'
      };
    }

    return { isValid: true };
  }
};