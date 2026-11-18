import { useState } from 'react';
import { Text, View } from 'react-native';
import { WorkspaceButton, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { agreementLabel, type StudentAgreement } from '../../tutors/api/agreements';

export function AgreementPicker({ agreements, selected, onSelect }: {
  agreements: StudentAgreement[];
  selected?: string;
  onSelect: (id: string) => void;
}) {
  const [open, setOpen] = useState(false);
  const agreement = agreements.find(item => item.agreementId === selected);
  let label = 'Choose a tutoring agreement';
  if (agreement) label = agreementLabel(agreement);
  return <View style={ui.stack}>
    <Text style={ui.body}>Tutoring agreement</Text>
    <WorkspaceButton label={label} onPress={() => setOpen(!open)} disabled={agreements.length === 0} />
    {open && agreements.map(item => <WorkspaceButton key={item.agreementId} label={agreementLabel(item)}
      onPress={() => { onSelect(item.agreementId); setOpen(false); }} />)}
    {agreements.length === 0 && <Text style={ui.body}>You have no tutoring agreements yet. Ask your tutor for an invitation.</Text>}
  </View>;
}
