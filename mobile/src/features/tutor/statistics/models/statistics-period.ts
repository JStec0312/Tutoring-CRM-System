export const periods = ['Week', 'Month', 'Year'] as const;
export type StatisticsPeriod = (typeof periods)[number];

export function periodStart(period: StatisticsPeriod, now = new Date()) {
  const start = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));
  if (period === 'Week') start.setUTCDate(start.getUTCDate() - ((start.getUTCDay() + 6) % 7));
  if (period === 'Month') start.setUTCDate(1);
  if (period === 'Year') start.setUTCMonth(0, 1);
  return start;
}

export function durationLabel(minutes: number) {
  return `${new Intl.NumberFormat('en-GB', { maximumFractionDigits: 1 }).format(minutes / 60)} h`;
}

export function attendanceLabel(completed: number, missed: number) {
  return completed + missed ? `${Math.round(completed / (completed + missed) * 100)}%` : '—';
}
