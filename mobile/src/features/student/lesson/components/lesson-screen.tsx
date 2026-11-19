import { useCallback } from 'react';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { Text, View } from 'react-native';
import type { ApiClient } from '@/shared/api/api-client';
import { WorkspaceButton, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { useWorkspaceQuery } from '@/shared/models/use-workspace-query';
import { getMaterials } from '@/features/tutor/materials/api/materials';
import { lessonMinutes, money, shortDate, timeLabel } from '@/features/tutor/workspace/models/workspace-format';
import { StudentPage, StudentQueryFeedback, studentStyles } from '../../workspace/components/student-workspace';
import { StudentTable } from '../../workspace/components/student-table';
import { tutorName } from '../../tutors/api/agreements';
import { MaterialDownloadButton } from '../../materials/components/material-download-button';
import { getStudentLesson } from '../api/lessons';

export function StudentLessonScreen() {
  const { lessonId } = useLocalSearchParams<{ lessonId: string }>();
  const router = useRouter();
  const load = useCallback(async (api: ApiClient, signal: AbortSignal) => {
    const lesson = await getStudentLesson(api, lessonId, signal);
    const materials = await getMaterials(api, lesson.tutoringAgreementId, signal);
    return { lesson, materials: materials.filter(material => material.lessonId === lessonId || !material.lessonId) };
  }, [lessonId]);
  const query = useWorkspaceQuery(load);
  const lesson = query.data?.lesson;
  let estimate = '';
  if (lesson?.estimatedAmount != null && lesson.currency) estimate = ` · ${money(lesson.estimatedAmount, lesson.currency)} estimated`;
  return <StudentPage code="S03" title="Lesson details & materials">
    <StudentQueryFeedback {...query} />
    {lesson && <>
      <Text style={ui.body}>{lesson.subject} with {tutorName(lesson)} / {shortDate(lesson.startsAtUtc)} · {timeLabel(lesson.startsAtUtc)}–{timeLabel(lesson.endsAtUtc)}</Text>
      {lesson.plannedTopic && <Text style={ui.body}>{lesson.plannedTopic}</Text>}
      <View style={studentStyles.notice}><Text style={studentStyles.noticeText}>{lesson.status} · {lessonMinutes(lesson)} minutes{estimate}</Text></View>
      {lesson.cancellationReason && <Text style={ui.body}>Cancellation reason: {lesson.cancellationReason}</Text>}
      <View style={ui.row}>
        {lesson.status === 'Scheduled' && <WorkspaceButton primary label="Cancel lesson"
          onPress={() => router.push({ pathname: '/student/lessons/[lessonId]/cancel', params: { lessonId } })} />}
        <WorkspaceButton label="Upload material" onPress={() => router.push({ pathname: '/student/materials/upload',
          params: { agreementId: lesson.tutoringAgreementId, lessonId } })} />
      </View>
      <StudentTable label="Shared learning materials" headings={['Shared material', 'Uploaded by', 'Action']}
        rows={(query.data?.materials ?? []).map(material => {
          let uploadedBy = 'You';
          if (material.uploadedByTutor) uploadedBy = tutorName(lesson);
          return { key: material.materialId, cells: [material.title, uploadedBy,
            <View key={material.materialId} style={ui.stack}>
              <MaterialDownloadButton material={material} />
              {!material.lessonId && <WorkspaceButton textOnly label="Attach to lesson →" onPress={() => router.push({
                pathname: '/student/materials/[materialId]/attach', params: { materialId: material.materialId, agreementId: lesson.tutoringAgreementId, lessonId },
              })} />}
            </View>] };
        })} />
      {query.data?.materials.length === 0 && <Text style={ui.body}>No shared materials for this lesson.</Text>}
      <View style={studentStyles.notice}><Text style={studentStyles.noticeText}>Cancelling requires a reason and confirmation. Materials are limited to this tutoring agreement.</Text></View>
    </>}
  </StudentPage>;
}
