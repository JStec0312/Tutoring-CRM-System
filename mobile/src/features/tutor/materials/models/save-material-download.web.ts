import type { Material } from '../api/materials';

export async function saveMaterialDownload(material: Material, bytes: ArrayBuffer) {
  const url = URL.createObjectURL(new Blob([bytes], { type: material.contentType }));
  const link = document.createElement('a');
  link.href = url;
  link.download = material.fileName;
  document.body.append(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
