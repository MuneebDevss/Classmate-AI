// hooks/useAssignments.ts
import { useState, useCallback } from 'react';
import { AssignmentDto } from '@/types';
import { classroomApi } from '@/lib/api';

export function useAssignments(jwt: string | undefined) {
  const [assignments, setAssignments] = useState<AssignmentDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | undefined>(undefined);

  const load = useCallback(async (courseId: string) => {
    if (!jwt) return;
    setLoading(true);
    setError(undefined);
    try {
      const data = await classroomApi.getAssignments(jwt, courseId);
      setAssignments(data);
    } catch (err: any) {
      setError(err.message || 'Failed to load assignments');
    } finally {
      setLoading(false);
    }
  }, [jwt]);

  return { assignments, loading, error, load };
}