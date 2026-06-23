"use client";

import { useEffect, useState, useCallback } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { classroomApi } from "@/lib/api";
import { CourseDto, AssignmentDto } from "@/types";
import Navbar from "@/Components/Navbar";
import CourseCard from "@/Components/CourseCard";
import AssignmentRow from "@/Components/AssignmentRow";

export default function DashboardPage() {
  const { token, isLoading: authLoading, user } = useAuth();
  const router = useRouter();

  const [courses, setCourses] = useState<CourseDto[]>([]);
  const [coursesLoading, setCoursesLoading] = useState(true);
  const [coursesError, setCoursesError] = useState<string | null>(null);

  const [selectedCourseId, setSelectedCourseId] = useState<string | null>(null);
  const [assignments, setAssignments] = useState<AssignmentDto[]>([]);
  const [assignmentsLoading, setAssignmentsLoading] = useState(false);
  const [assignmentsError, setAssignmentsError] = useState<string | null>(null);

  // Auth guard
  useEffect(() => {
    if (!authLoading && !token) router.replace("/login");
  }, [authLoading, token, router]);

  const fetchCourses = useCallback(async () => {
    setCoursesLoading(true);
    setCoursesError(null);
    try {
      const data = await classroomApi.getCourses(token?? "");
      setCourses(data);
    } catch (e) {
      setCoursesError(
        e instanceof Error ? e.message : "Failed to load courses."
      );
    } finally {
      setCoursesLoading(false);
    }
  }, []);

  useEffect(() => {
    if (token) fetchCourses();
  }, [token, fetchCourses]);

  const handleSelectCourse = async (courseId: string) => {
    if (selectedCourseId === courseId) {
      setSelectedCourseId(null);
      setAssignments([]);
      return;
    }
    setSelectedCourseId(courseId);
    setAssignmentsLoading(true);
    setAssignmentsError(null);
    try {
      const data = await classroomApi.getAssignments(token?? "", courseId);
      setAssignments(data);
    } catch (e) {
      setAssignmentsError(
        e instanceof Error ? e.message : "Failed to load assignments."
      );
    } finally {
      setAssignmentsLoading(false);
    }
  };

  const selectedCourse = courses.find((c) => c.courseId === selectedCourseId);

  if (authLoading) return <FullPageSpinner />;

  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />

      <main className="flex-1 px-4 sm:px-6 py-6 max-w-6xl mx-auto w-full">
        {/* Free tier banner */}
        {user && user.freeUsagesRemaining === 0 && !user.hasOpenAiKey && !user.hasGeminiKey && (
          <div
            className="mb-6 flex items-center justify-between gap-4 px-4 py-3 rounded-xl border animate-fade-up"
            style={{
              background: "var(--amber-dim)",
              borderColor: "rgba(245,166,35,0.25)",
            }}
          >
            <div className="flex items-center gap-2.5 text-sm" style={{ color: "var(--amber)" }}>
              <span>⚡</span>
              <span>
                Free tier used up. Add an API key to continue auto-solving.
              </span>
            </div>
            <a
              href="/settings"
              className="text-xs font-semibold px-3 py-1.5 rounded-lg shrink-0 transition-opacity hover:opacity-80"
              style={{
                background: "var(--amber)",
                color: "#0a0a0f",
              }}
            >
              Add Key →
            </a>
          </div>
        )}

        {/* Page header */}
        <div className="flex items-center justify-between mb-6 animate-fade-up">
          <div>
            <h1
              className="text-xl font-bold"
              style={{ fontFamily: "var(--font-display)" }}
            >
              Your Courses
            </h1>
            <p className="text-xs mt-0.5" style={{ color: "var(--text-muted)" }}>
              {courses.length > 0
                ? `${courses.length} active course${courses.length !== 1 ? "s" : ""} from Google Classroom`
                : "Loading your courses…"}
            </p>
          </div>
          <button
            onClick={fetchCourses}
            disabled={coursesLoading}
            className="flex items-center gap-2 text-xs px-3 py-2 rounded-lg transition-all duration-150 disabled:opacity-50"
            style={{
              background: "var(--bg-elevated)",
              border: "1px solid var(--border)",
              color: "var(--text-secondary)",
            }}
          >
            {coursesLoading ? (
              <span className="spinner" style={{ width: 12, height: 12 }} />
            ) : (
              <RefreshIcon />
            )}
            Refresh
          </button>
        </div>

        {/* Courses grid */}
        {coursesLoading ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 mb-8">
            {[...Array(6)].map((_, i) => (
              <div key={i} className="h-40 skeleton rounded-2xl" />
            ))}
          </div>
        ) : coursesError ? (
          <ErrorState message={coursesError} onRetry={fetchCourses} />
        ) : courses.length === 0 ? (
          <EmptyCoursesState />
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 mb-8 stagger">
            {courses.map((course) => (
              <div key={course.courseId} className="animate-fade-up">
                <CourseCard
                  course={course}
                  onSelect={handleSelectCourse}
                  isSelected={selectedCourseId === course.courseId}
                />
              </div>
            ))}
          </div>
        )}

        {/* Assignments panel */}
        {selectedCourseId && (
          <div className="animate-fade-up">
            <div
              className="rounded-2xl border overflow-hidden"
              style={{
                background: "var(--bg-card)",
                borderColor: "var(--border)",
              }}
            >
              {/* Panel header */}
              <div
                className="flex items-center justify-between px-5 py-4 border-b"
                style={{ borderColor: "var(--border)" }}
              >
                <div>
                  <h2
                    className="font-semibold text-sm"
                    style={{ fontFamily: "var(--font-display)" }}
                  >
                    {selectedCourse?.name}
                  </h2>
                  <p className="text-xs mt-0.5" style={{ color: "var(--text-muted)" }}>
                    Assignments · sorted by most recent
                  </p>
                </div>
                <button
                  onClick={() => {
                    setSelectedCourseId(null);
                    setAssignments([]);
                  }}
                  className="text-xs px-2.5 py-1 rounded-lg transition-colors duration-150"
                  style={{
                    color: "var(--text-muted)",
                    border: "1px solid var(--border)",
                  }}
                >
                  Close ✕
                </button>
              </div>

              {/* Panel body */}
              <div className="p-4">
                {assignmentsLoading ? (
                  <div className="space-y-3">
                    {[...Array(4)].map((_, i) => (
                      <div key={i} className="h-20 skeleton rounded-xl" />
                    ))}
                  </div>
                ) : assignmentsError ? (
                  <ErrorState
                    message={assignmentsError}
                    onRetry={() => handleSelectCourse(selectedCourseId)}
                  />
                ) : assignments.length === 0 ? (
                  <div className="py-10 text-center">
                    <p
                      className="text-sm"
                      style={{ color: "var(--text-muted)" }}
                    >
                      No published assignments found for this course.
                    </p>
                  </div>
                ) : (
                  <div className="space-y-2 stagger">
                    {assignments.map((a) => (
                      <div key={a.assignmentId} className="animate-fade-up">
                        <AssignmentRow assignment={a} />
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </div>
        )}
      </main>
    </div>
  );
}

function FullPageSpinner() {
  return (
    <div className="min-h-screen flex items-center justify-center">
      <span className="spinner" style={{ width: 28, height: 28 }} />
    </div>
  );
}

function EmptyCoursesState() {
  return (
    <div
      className="rounded-2xl border py-16 text-center"
      style={{
        background: "var(--bg-card)",
        borderColor: "var(--border)",
      }}
    >
      <div className="text-3xl mb-3">📚</div>
      <p
        className="text-sm font-medium mb-1"
        style={{ color: "var(--text-primary)" }}
      >
        No active courses found
      </p>
      <p className="text-xs" style={{ color: "var(--text-muted)" }}>
        Make sure you are enrolled as a student in at least one Google Classroom
        course.
      </p>
    </div>
  );
}

function ErrorState({
  message,
  onRetry,
}: {
  message: string;
  onRetry: () => void;
}) {
  return (
    <div
      className="rounded-2xl border py-10 text-center px-4"
      style={{
        background: "var(--red-dim)",
        borderColor: "rgba(240,84,106,0.2)",
      }}
    >
      <p className="text-sm mb-3" style={{ color: "var(--red)" }}>
        {message}
      </p>
      <button
        onClick={onRetry}
        className="text-xs px-3 py-1.5 rounded-lg"
        style={{
          background: "var(--bg-elevated)",
          border: "1px solid var(--border)",
          color: "var(--text-primary)",
        }}
      >
        Try again
      </button>
    </div>
  );
}

function RefreshIcon() {
  return (
    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3 12a9 9 0 0 1 9-9 9.75 9.75 0 0 1 6.74 2.74L21 8" />
      <path d="M21 3v5h-5" />
      <path d="M21 12a9 9 0 0 1-9 9 9.75 9.75 0 0 1-6.74-2.74L3 16" />
      <path d="M8 16H3v5" />
    </svg>
  );
}