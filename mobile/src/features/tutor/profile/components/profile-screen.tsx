import { getProfile, ProfileForm, PasswordForm } from '@/shared/components/account-profile';
import { useTutorQuery } from '../../workspace/models/use-tutor-query';
import { QueryFeedback, TutorPage } from '../../workspace/components/tutor-workspace';

export function TutorProfileScreen() {
  const query = useTutorQuery(getProfile);
  return <TutorPage title="Profile & security">
    <QueryFeedback {...query} />
    {query.data && <ProfileForm profile={query.data} />}
    <PasswordForm />
  </TutorPage>;
}
