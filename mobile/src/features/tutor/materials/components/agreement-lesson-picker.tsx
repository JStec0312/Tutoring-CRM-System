import { useState } from 'react';
import { Text, View } from 'react-native';
import { WorkspaceButton, WorkspaceField, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import type { CalendarLesson } from '../../calendar/api/get-calendar';
import { lessonTime, shortDate } from '../../workspace/models/workspace-format';

export function AgreementLessonPicker({ lessons, selected, onSelect, optional = false, disabled = false }: {
  lessons: CalendarLesson[];
  selected?: string;
  onSelect: (lessonId?: string) => void;
  optional?: boolean;
  disabled?: boolean;
}) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState('');
  const selectedLesson = lessons.find(lesson => lesson.lessonId === selected);
  const label = (lesson: CalendarLesson) => `${shortDate(lesson.startsAtUtc)} · ${lessonTime(lesson)} · ${lesson.status}`;
  const matches = [...lessons].reverse().filter(lesson => label(lesson).toLowerCase().includes(search.trim().toLowerCase()));
  function select(id?: string) {
    onSelect(id);
    setOpen(false);
    setSearch('');
  }

  return <View style={ui.stack}>
    <Text style={ui.body}>{optional ? 'Lesson (optional)' : 'Lesson in this agreement'}</Text>
    <WorkspaceButton label={selectedLesson ? label(selectedLesson) : 'Choose a lesson'} disabled={disabled || !lessons.length}
      onPress={() => setOpen(!open)} />
    {optional && !!selected && <WorkspaceButton label="Clear lesson" disabled={disabled} onPress={() => select()} />}
    {!lessons.length && <Text style={ui.body}>There are no lessons in this agreement.</Text>}
    {open && <>
      <WorkspaceField label="Search lessons by date" value={search} onChangeText={setSearch} />
      {matches.slice(0, 12).map(lesson => <WorkspaceButton key={lesson.lessonId} label={label(lesson)}
        primary={lesson.lessonId === selected} disabled={disabled} onPress={() => select(lesson.lessonId)} />)}
      {matches.length > 12 && <Text style={ui.body}>Search to find more lessons.</Text>}
      {!matches.length && <Text style={ui.body}>No matching lessons.</Text>}
    </>}
  </View>;
}
