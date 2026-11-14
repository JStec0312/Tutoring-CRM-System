import { Directory, File, Paths } from 'expo-file-system';
import { isAvailableAsync, shareAsync } from 'expo-sharing';
import type { Material } from '../api/materials';

export async function saveMaterialDownload(material: Material, bytes: ArrayBuffer) {
  if (!await isAvailableAsync()) throw new Error('File sharing is unavailable on this device.');
  const folder = new Directory(Paths.cache, 'tutoring-materials', material.materialId);
  folder.create({ intermediates: true, idempotent: true });
  const name = material.fileName.split(/[\\/]/).pop();
  const safeName = name && name !== '.' && name !== '..' ? name : 'material';
  const file = new File(folder, safeName);
  try {
    await file.write(bytes);
    await shareAsync(file.uri, { mimeType: material.contentType, dialogTitle: 'Save learning material' });
  } finally {
    if (file.exists) file.delete();
  }
}
