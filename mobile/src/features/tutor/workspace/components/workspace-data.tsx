import type { ReactNode } from 'react';
import { Pressable, StyleSheet, Text, useWindowDimensions, View } from 'react-native';
import { workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';

export function WorkspaceMetrics({ items }: { items: { label: string; value: string }[] }) {
  const compact = useWindowDimensions().width < 760;
  return <View style={compact ? ui.stack : ui.row}>
    {items.map(({ label, value }) => <View key={label} style={[styles.metric, !compact && styles.metricWide]}>
      <Text style={styles.label}>{label}</Text>
      <Text style={styles.value}>{value}</Text>
    </View>)}
  </View>;
}

export function WorkspaceTable({ headers, rows }: {
  headers: string[];
  rows: { key: string; cells: ReactNode[]; onPress?: () => void }[];
}) {
  const compact = useWindowDimensions().width < 900;
  if (!rows.length) return null;
  return <View style={styles.table}>
    {!compact && <View style={[styles.line, styles.header]}>
      {headers.map(label => <Text key={label} style={[styles.cell, styles.label]}>{label}</Text>)}
    </View>}
    {rows.map(({ key, cells, onPress }) => {
      const contents = cells.map((cell, index) => <View key={headers[index]} style={compact ? styles.mobileCell : styles.cell}>
        {compact && index > 0 && <Text style={styles.label}>{headers[index]}</Text>}
        {typeof cell === 'string' ? <Text style={index === 0 && compact ? styles.label : ui.body}>{cell}</Text> : cell}
      </View>);
      const style = compact ? styles.mobileRow : styles.line;
      return onPress
        ? <Pressable key={key} accessibilityRole="link" style={style} onPress={onPress}>{contents}</Pressable>
        : <View key={key} style={style}>{contents}</View>;
    })}
  </View>;
}

const styles = StyleSheet.create({
  metric: { padding: 20, gap: 8, borderWidth: 1, borderColor: '#ccc', borderRadius: 4 },
  metricWide: { flex: 1, minWidth: 120 },
  label: { ...ui.body, fontWeight: '600' },
  value: { ...ui.heading, fontSize: 24, lineHeight: 32 },
  table: { borderWidth: 1, borderColor: '#ddd' },
  header: { backgroundColor: '#d9d9d9' },
  line: { flexDirection: 'row', alignItems: 'center', padding: 16, gap: 12, borderBottomWidth: 1, borderColor: '#e4e4e4' },
  cell: { flex: 1, minWidth: 0 },
  mobileRow: { backgroundColor: '#f7f7f7', gap: 8, padding: 12, borderBottomWidth: 1, borderColor: '#ddd' },
  mobileCell: { gap: 3 },
});
