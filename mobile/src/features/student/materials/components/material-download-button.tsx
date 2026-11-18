import { Text, View } from 'react-native';
import { useApi } from '@/shared/api/api-context';
import { WorkspaceButton, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { useWorkspaceMutation } from '@/shared/models/use-workspace-mutation';
import { downloadMaterial, type Material } from '@/features/tutor/materials/api/materials';
import { saveMaterialDownload } from '@/features/tutor/materials/models/save-material-download';

export function MaterialDownloadButton({ material }: { material: Material }) {
  const api = useApi();
  const mutation = useWorkspaceMutation();
  async function download() {
    const bytes = await downloadMaterial(api, material.materialId);
    await saveMaterialDownload(material, bytes);
  }
  return <View style={ui.stack}>
    <WorkspaceButton textOnly label="Download →" accessibilityLabel={`Download ${material.fileName}`} disabled={mutation.busy} onPress={() => void mutation.run(download)} />
    {!!mutation.error && <Text role="alert" style={ui.error}>{mutation.error}</Text>}
  </View>;
}
