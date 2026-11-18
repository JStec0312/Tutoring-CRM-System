import type { ReactNode } from 'react';
import { ScrollView, StyleSheet, Text, useWindowDimensions, View } from 'react-native';

type TableRow = { key: string; cells: ReactNode[] };

function renderCell(cell: ReactNode) {
  if (typeof cell === 'string') return <Text style={styles.text}>{cell}</Text>;
  return cell;
}

export function StudentTable({ label, headings, rows }: {
  label: string;
  headings: string[];
  rows: TableRow[];
}) {
  const compact = useWindowDimensions().width < 760;
  if (compact) {
    return <View role="list" accessibilityLabel={label} style={styles.cards}>
      {rows.map(row => <View key={row.key} role="listitem" style={styles.card}>
        {row.cells.map((cell, index) => <View key={headings[index]} style={styles.cardCell}>
          <Text style={styles.headerText}>{headings[index]}</Text>
          {renderCell(cell)}
        </View>)}
      </View>)}
    </View>;
  }
  return <ScrollView horizontal contentContainerStyle={styles.scroll}>
    <View role="table" accessibilityLabel={label} style={styles.table}>
      <View role="row" style={[styles.row, styles.header]}>
        {headings.map(heading => <Text key={heading} role="columnheader" style={[styles.cell, styles.headerText]}>{heading}</Text>)}
      </View>
      {rows.map(row => <View key={row.key} role="row" style={styles.row}>
        {row.cells.map((cell, index) => <View key={headings[index]} role="cell" style={styles.cell}>
          {renderCell(cell)}
        </View>)}
      </View>)}
    </View>
  </ScrollView>;
}

const styles = StyleSheet.create({
  scroll: { flexGrow: 1 },
  table: { flex: 1, minWidth: 590 },
  row: { flexDirection: 'row', borderWidth: 0.5, borderColor: '#d1d1d1', paddingHorizontal: 16, paddingVertical: 4, gap: 12, minHeight: 53 },
  header: { backgroundColor: '#d9d9d9', paddingVertical: 16 },
  cell: { flex: 1, minWidth: 0, justifyContent: 'center' },
  headerText: { color: '#292929', fontSize: 14, lineHeight: 20, fontWeight: '600' },
  text: { color: '#292929', fontSize: 14, lineHeight: 20 },
  cards: { gap: 12 },
  card: { borderWidth: 1, borderColor: '#d1d1d1', padding: 16, gap: 12 },
  cardCell: { gap: 4 },
});
