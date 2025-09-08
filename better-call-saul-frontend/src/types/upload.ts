export interface UploadFile {
  id: string;
  file: File;
  name: string;
  size: number;
  type: string;
  uploadProgress: number;
  status: UploadStatus;
  error?: string;
}

export enum UploadStatus {
  PENDING = 'pending',
  UPLOADING = 'uploading',
  COMPLETED = 'completed',
  ERROR = 'error',
  CANCELLED = 'cancelled'
}

export interface FileUploadProgress {
  fileId: string;
  progress: number;
  status: UploadStatus;
}

export interface CaseUploadData {
  caseName: string;
  clientName: string;
  files: UploadFile[];
}

export interface FileUploadResponse {
  success: boolean;
  fileId: string;
  fileName: string;
  url?: string;
  error?: string;
}

export interface CaseCreationResponse {
  success: boolean;
  caseId: string;
  message?: string;
  error?: string;
}

export const ALLOWED_FILE_TYPES = [
  'application/pdf',
  'application/msword',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  'text/plain'
];

export const MAX_FILE_SIZE = 50 * 1024 * 1024; // 50MB
export const MAX_FILES = 10;