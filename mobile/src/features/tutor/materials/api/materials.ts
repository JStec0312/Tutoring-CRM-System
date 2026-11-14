import type { DocumentPickerAsset } from 'expo-document-picker';
import type { ApiClient } from '@/shared/api/api-client';

const endpoint = '/api/learning-materials';

export type Material = {
  materialId: string;
  lessonId: string | null;
  title: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedByTutor: boolean;
};

export function getMaterials(api: ApiClient, agreementId: string, signal: AbortSignal) {
  const params = new URLSearchParams({ tutoringAgreementId: agreementId });
  return api.get<Material[]>(`${endpoint}?${params}`, { signal });
}

export function uploadMaterial(api: ApiClient, agreementId: string, title: string, asset: DocumentPickerAsset, lessonId?: string) {
  const form = new FormData();
  form.append('Title', title);
  form.append('TutoringAgreementId', agreementId);
  if (lessonId) form.append('LessonId', lessonId);
  const file = asset.file ?? { uri: asset.uri, name: asset.name, type: asset.mimeType || 'application/octet-stream' };
  form.append('File', file as Blob);
  return api.post(endpoint, form);
}

export function attachMaterial(api: ApiClient, materialId: string, lessonId: string) {
  return api.put(`${endpoint}/${encodeURIComponent(materialId)}/lesson`, { lessonId });
}

export function deleteMaterial(api: ApiClient, materialId: string) {
  return api.delete(`${endpoint}/${encodeURIComponent(materialId)}`);
}

export function downloadMaterial(api: ApiClient, materialId: string) {
  return api.download(`${endpoint}/${encodeURIComponent(materialId)}/download`);
}
