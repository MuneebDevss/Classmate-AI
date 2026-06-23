import { CourseDto, AssignmentDto, UserDto, AuthResponse } from '@/types';

const BASE = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000';

// ── Base fetch with auth header ───────────────────────────────────────────────

async function apiFetch<T>(
  path: string,
  jwt: string,
  options: RequestInit = {}
): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      Authorization:  `Bearer ${jwt}`,
      ...(options.headers ?? {}),
    },
  });

  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new ApiError(res.status, (body).error ?? 'Request failed');
  }

  // 204 No Content
  if (res.status === 204) return undefined as T;
  const text = await res.text();
  return text ? JSON.parse(text) : (undefined as T);
}

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
  }
}

// ── Classroom endpoints ───────────────────────────────────────────────────────

export const classroomApi = {

  /** Exchange Google ID token + access token for our own JWT */
  googleLogin: (tokens: { idToken: string; accessToken: string; refreshToken: string }) =>
    apiFetch<AuthResponse>('/api/auth/google', '', {
      method: 'POST',
      body: JSON.stringify(tokens),
    }),
  /** Fetch all active courses, merged with user auto-solve settings */
  getCourses: (jwt: string) =>
    apiFetch<CourseDto[]>('/api/classroom/courses', jwt),

  /** Fetch all published assignments for a course */
  getAssignments: (jwt: string, courseId: string) =>
    apiFetch<AssignmentDto[]>(`/api/classroom/courses/${courseId}/assignments`, jwt),

  /** Save auto-solve toggle + delay for a course */
  updateSettings: (
    jwt: string,
    courseId: string,
    autoSolve: boolean,
    courseName: string,
    delayMinutes: number
  ) =>
    apiFetch<void>(`/api/classroom/courses/settings`, jwt, {
      method: 'PUT',
      body: JSON.stringify({ courseId, autoSolve, delayMinutes, courseName }),
    }),
};