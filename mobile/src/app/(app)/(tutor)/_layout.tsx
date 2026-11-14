import { Slot } from 'expo-router';
import { TutorWorkspace } from '@/features/tutor/workspace/components/tutor-workspace';

export default function TutorLayout() {
  return <TutorWorkspace><Slot /></TutorWorkspace>;
}
