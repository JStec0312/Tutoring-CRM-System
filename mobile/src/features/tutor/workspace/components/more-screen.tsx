import { useRouter, type Href } from 'expo-router';
import { WorkspaceButton } from '@/shared/components/ui/workspace-controls';
import { useAuth } from '@/shared/session/AuthProvider';
import { TutorPage } from './tutor-workspace';

const links: { label: string; to: Href }[] = [
  { label: 'Payments', to: '/tutor/payments' },
  { label: 'Statistics', to: '/tutor/statistics' },
  { label: 'Notifications', to: '/tutor/notifications' },
  { label: 'Profile & security', to: '/tutor/profile' },
];

export function MoreScreen() {
  const router = useRouter();
  const { signOut } = useAuth();
  return <TutorPage title="More">
    {links.map(link => <WorkspaceButton key={link.label} label={link.label} onPress={() => router.push(link.to)} />)}
    <WorkspaceButton label="Sign out" onPress={() => void signOut().catch(() => console.warn('Could not sign out on the server.'))} />
  </TutorPage>;
}
