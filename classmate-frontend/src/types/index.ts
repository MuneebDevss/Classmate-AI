// ── Auth ──────────────────────────────────────────────────────────────────────

export interface GoogleAuthRequest {
  idToken: string;
  accessToken: string;
  refreshToken: string;
}

export interface AuthResponse {
  token: string;
  user: UserDto;
}

// ── User ──────────────────────────────────────────────────────────────────────

export interface UserDto {
  id: number;
  email: string;
  displayName: string;
  avatarUrl?: string;
  freeUsagesRemaining: number;
  hasOpenAiKey: boolean;
  hasGeminiKey: boolean;
  notificationEmail: string;
}

// ── Classroom ─────────────────────────────────────────────────────────────────

export interface CourseDto {
  courseId: string;
  name: string;
  section?: string;
  description?: string;
  teacherName?: string;
  courseState: string;
  enrollmentCode?: string;
  autoSolve: boolean;
  delayMinutes: number;
}

export interface AssignmentDto {
  assignmentId: string;
  courseId: string;
  title: string;
  description?: string;
  dueDate?: string;
  maxPoints?: number;
  workType: string;
  state: string;
  materials: MaterialDto[];
}

export interface MaterialDto {
  type: string;
  title: string;
  url?: string;
  driveFileId?: string;
  thumbnailUrl?: string;
}

// ── Settings ──────────────────────────────────────────────────────────────────

export interface UpsertClassroomSettingRequest {
  courseId: string;
  courseName: string;
  autoSolve: boolean;
  delayMinutes: number;
}

// ── UI Helpers ────────────────────────────────────────────────────────────────

export interface DelayOption {
  label: string;
  value: number;
}

export const DELAY_OPTIONS: DelayOption[] = [
  { label: "Immediately", value: 0 },
  { label: "30 minutes", value: 30 },
  { label: "1 hour", value: 60 },
  { label: "6 hours", value: 360 },
  { label: "1 day", value: 1440 },
  { label: "1h before due", value: -60 },
];