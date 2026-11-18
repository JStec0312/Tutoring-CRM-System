import { Pressable, StyleSheet, Text, TextInput, View, type TextInputProps } from 'react-native';
import type { PropsWithChildren } from 'react';

export function WorkspaceButton({ label, onPress, primary = false, disabled = false, accessibilityLabel, textOnly = false }: {
  label: string;
  accessibilityLabel?: string;
  textOnly?: boolean;
  onPress: () => void;
  primary?: boolean;
  disabled?: boolean;
}) {
  return <Pressable
    accessibilityRole="button"
    accessibilityLabel={accessibilityLabel}
    accessibilityState={{ disabled }}
    disabled={disabled}
    onPress={onPress}
    style={({ pressed }) => [styles.button, primary && styles.primary, textOnly && styles.textOnly, (pressed || disabled) && styles.dimmed]}
  >
    <Text style={[styles.body, primary && styles.primaryText]}>{label}</Text>
  </Pressable>;
}

export function WorkspaceField({ label, ...inputProps }: TextInputProps & { label: string }) {
  return <View style={styles.field}>
    <Text style={styles.body}>{label}</Text>
    <TextInput
      accessibilityLabel={label}
      placeholderTextColor="#666"
      style={[styles.input, inputProps.multiline && styles.textArea]}
      {...inputProps}
    />
  </View>;
}

export function WorkspaceCard({ title, detail, onPress }: { title: string; detail?: string; onPress?: () => void }) {
  const content = <>
    <Text style={styles.cardTitle}>{title}</Text>
    {detail ? <Text style={styles.body}>{detail}</Text> : null}
  </>;
  return onPress
    ? <Pressable accessibilityRole="button" onPress={onPress} style={({ pressed }) => [styles.card, pressed && styles.dimmed]}>{content}</Pressable>
    : <View style={styles.card}>{content}</View>;
}

export function WorkspaceColumns({ children, compact }: PropsWithChildren<{ compact: boolean }>) {
  return <View style={compact ? styles.stacked : styles.columns}>{children}</View>;
}

export const workspaceStyles = StyleSheet.create({
  heading: { color: '#292929', fontSize: 28, lineHeight: 39, fontWeight: '600' },
  sectionHeading: { color: '#292929', fontSize: 18, lineHeight: 25, fontWeight: '600' },
  body: { color: '#292929', fontSize: 14, lineHeight: 20 },
  main: { flex: 499, minWidth: 0, gap: 16 },
  aside: { flex: 377, minWidth: 0, gap: 16 },
  stack: { gap: 16, minWidth: 0 },
  row: { flexDirection: 'row', alignItems: 'center', flexWrap: 'wrap', gap: 12 },
  error: { color: '#9d2525', fontSize: 14, lineHeight: 20 },
});

const styles = StyleSheet.create({
  body: workspaceStyles.body,
  button: {
    minHeight: 44, paddingVertical: 10, paddingHorizontal: 12, alignItems: 'center', justifyContent: 'center',
    borderColor: '#898989', borderWidth: 1, borderRadius: 8, backgroundColor: '#e3e3e3',
  },
  textOnly: { backgroundColor: 'transparent', borderWidth: 0, paddingHorizontal: 0, alignItems: 'flex-start' },
  primary: { backgroundColor: '#292929', borderColor: '#292929' },
  primaryText: { color: '#fff' },
  dimmed: { opacity: 0.6 },
  field: { gap: 6 },
  input: {
    color: '#292929', fontSize: 16, minHeight: 41, paddingHorizontal: 15, paddingVertical: 8,
    backgroundColor: '#fff', borderColor: '#d1d1d1', borderWidth: 1, borderRadius: 8,
  },
  textArea: { minHeight: 85, textAlignVertical: 'top' },
  card: { backgroundColor: '#f7f7f7', borderColor: '#ccc', borderWidth: 1, padding: 12, gap: 6, minWidth: 0 },
  cardTitle: { color: '#292929', fontSize: 16, lineHeight: 22, fontWeight: '600' },
  columns: { flexDirection: 'row', alignItems: 'flex-start', gap: 24 },
  stacked: { gap: 24 },
});
