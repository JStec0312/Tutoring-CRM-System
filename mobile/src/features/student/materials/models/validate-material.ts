import type { DocumentPickerAsset } from 'expo-document-picker';

export function validateMaterial(asset: Pick<DocumentPickerAsset, 'name' | 'size'>) {
  if (!/\.(pdf|jpg|png)$/i.test(asset.name)) return 'Choose a PDF, JPG or PNG file.';
  if (asset.size === 0) return 'Choose a file that is not empty.';
  if (asset.size != null && asset.size > 20_000_000) return 'Choose a file up to 20 MB.';
  return '';
}
