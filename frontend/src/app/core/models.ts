/**
 * TypeScript interfaces mirroring the API's DTOs. An `interface` is just a
 * shape/contract for an object — it has no runtime code, it only helps the
 * compiler check that we use the right fields.
 */

// ---- Auth ----
export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  deviceIdentifier?: string; // '?' = optional
}

export interface AuthResponse {
  userId: string;
  email: string;
  fullName: string;
  role: string;
  message: string;
}

export interface LoginResponse extends AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

// ---- Documents ----
export interface DocumentDto {
  id: string;
  fileName: string;
  fileType: string;
  fileSizeBytes: number;
  uploadedAt: string; // ISO date string
}

export interface DocumentChunkDto {
  id: string;
  pageNumber: number;
  chunkIndex: number;
  content: string;
  hasEmbedding: boolean;
}

// ---- Chat ----
export interface AskRequest {
  question: string;
}

export interface SourceDto {
  document: string;
  page: number;
  relevance: number;
}

export interface ChatResponse {
  answer: string;
  sources: SourceDto[];
}
