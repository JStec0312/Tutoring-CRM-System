import { Slot } from 'expo-router';
import { StudentWorkspace } from '@/features/student/workspace/components/student-workspace';

export default function StudentLayout() {
  return <StudentWorkspace><Slot /></StudentWorkspace>;
}
