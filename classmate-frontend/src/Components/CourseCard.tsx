"use client";

import { useState } from "react";
import { CourseDto, DELAY_OPTIONS } from "@/types";
import { classroomApi } from "@/lib/api";
import { useAuth } from "@/lib/auth-context";

interface CourseCardProps {
  course: CourseDto;
  onSelect: (courseId: string) => void;
  isSelected: boolean;
}

export default function CourseCard({
  course,
  onSelect,
  isSelected,
}: CourseCardProps) {
  const [autoSolve, setAutoSolve] = useState(course.autoSolve);
  const [delayMinutes, setDelayMinutes] = useState(course.delayMinutes);
  const [saving, setSaving] = useState(false);
  const { token } = useAuth();

  const persist = async (nextAutoSolve: boolean, nextDelay: number) => {
    setSaving(true);
    try {
      await classroomApi.updateSettings(
        token??'',
        course.courseId,
        nextAutoSolve,
        course.name,
        nextDelay
      );
    } catch (e) {
      console.error("Failed to save settings", e);
    } finally {
      setSaving(false);
    }
  };

  const handleToggle = async () => {
    const next = !autoSolve;
    setAutoSolve(next);
    await persist(next, delayMinutes);
  };

  const handleDelay = async (e: React.ChangeEvent<HTMLSelectElement>) => {
    const next = Number(e.target.value);
    setDelayMinutes(next);
    await persist(autoSolve, next);
  };

  // Generate a stable color from course name
  const hue = course.name
    .split("")
    .reduce((acc, c) => acc + c.charCodeAt(0), 0) % 360;

  return (
    <div
      className="rounded-2xl border overflow-hidden transition-all duration-200 cursor-pointer group"
      style={{
        background: isSelected ? "var(--bg-elevated)" : "var(--bg-card)",
        borderColor: isSelected ? "var(--accent)" : "var(--border)",
        boxShadow: isSelected ? "0 0 0 1px var(--accent)" : "none",
      }}
      onClick={() => onSelect(course.courseId)}
    >
      {/* Color bar */}
      <div
        className="h-1 w-full"
        style={{
          background: `linear-gradient(90deg, hsl(${hue}, 65%, 55%), hsl(${
            hue + 40
          }, 65%, 55%))`,
        }}
      />

      <div className="p-5">
        {/* Header */}
        <div className="flex items-start justify-between gap-3 mb-3">
          <div className="flex-1 min-w-0">
            <h3
              className="font-semibold text-sm leading-snug truncate"
              style={{ color: "var(--text-primary)" }}
              title={course.name}
            >
              {course.name}
            </h3>
            {course.section && (
              <p
                className="text-xs mt-0.5 truncate"
                style={{ color: "var(--text-muted)" }}
              >
                {course.section}
              </p>
            )}
            {course.teacherName && (
              <p
                className="text-xs mt-0.5 truncate"
                style={{ color: "var(--text-muted)" }}
              >
                {course.teacherName}
              </p>
            )}
          </div>

          {/* Auto-solve toggle */}
          <div className="flex flex-col items-end gap-1">
            <button
              onClick={(e) => {
                e.stopPropagation();
                handleToggle();
              }}
              disabled={saving}
              className="relative w-10 h-5 rounded-full transition-all duration-200 flex-shrink-0 disabled:opacity-60"
              style={{
                background: autoSolve
                  ? "var(--accent)"
                  : "var(--bg-hover)",
                border: `1.5px solid ${
                  autoSolve ? "var(--accent)" : "var(--border-bright)"
                }`,
              }}
              aria-label={autoSolve ? "Disable auto-solve" : "Enable auto-solve"}
            >
              <span
                className="absolute top-0.5 w-3.5 h-3.5 rounded-full transition-all duration-200"
                style={{
                  background: autoSolve ? "#fff" : "var(--text-muted)",
                  left: autoSolve ? "calc(100% - 16px)" : "1px",
                }}
              />
            </button>
            <span
              className="text-[10px] font-medium"
              style={{
                color: autoSolve ? "var(--accent)" : "var(--text-muted)",
              }}
            >
              {autoSolve ? "Auto" : "Off"}
            </span>
          </div>
        </div>

        {/* Delay selector — only shown when auto-solve is on */}
        <div
          className="overflow-hidden transition-all duration-300"
          style={{ maxHeight: autoSolve ? "60px" : "0px", opacity: autoSolve ? 1 : 0 }}
        >
          <div
            className="flex items-center gap-2 pt-2 border-t"
            style={{ borderColor: "var(--border)" }}
            onClick={(e) => e.stopPropagation()}
          >
            <span
              className="text-xs shrink-0"
              style={{ color: "var(--text-muted)" }}
            >
              Solve after
            </span>
            <select
              value={delayMinutes}
              onChange={handleDelay}
              disabled={saving}
              className="flex-1 text-xs rounded-lg px-2 py-1.5 font-medium appearance-none cursor-pointer transition-colors duration-150"
              style={{
                background: "var(--bg-base)",
                border: "1px solid var(--border-bright)",
                color: "var(--text-primary)",
                outline: "none",
              }}
            >
              {DELAY_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
            {saving && <span className="spinner shrink-0" style={{ width: 14, height: 14 }} />}
          </div>
        </div>

        {/* Footer: view assignments link */}
        <div className="mt-3 flex items-center justify-between">
          <span
            className="text-[10px] px-2 py-0.5 rounded-full font-medium"
            style={{
              background: "var(--green-dim)",
              color: "var(--green)",
            }}
          >
            Active
          </span>
          <span
            className="text-xs transition-colors duration-150"
            style={{
              color: isSelected ? "var(--accent)" : "var(--text-muted)",
            }}
          >
            {isSelected ? "Viewing assignments →" : "Click to view →"}
          </span>
        </div>
      </div>
    </div>
  );
}