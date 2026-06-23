"use client";

import { AssignmentDto } from "@/types";
import { formatDueDate, timeAgo } from "@/lib/utils";

interface AssignmentRowProps {
  assignment: AssignmentDto;
}

const WORK_TYPE_LABEL: Record<string, string> = {
  ASSIGNMENT: "Assignment",
  SHORT_ANSWER_QUESTION: "Short Answer",
  MULTIPLE_CHOICE_QUESTION: "Multiple Choice",
};

export default function AssignmentRow({ assignment }: AssignmentRowProps) {
  const { label: dueLabel, status: dueStatus } = formatDueDate(
    assignment.dueDate
  );

  const dueColor =
    dueStatus === "overdue"
      ? "var(--red)"
      : dueStatus === "due-soon"
      ? "var(--amber)"
      : "var(--text-muted)";

  const dueBg =
    dueStatus === "overdue"
      ? "var(--red-dim)"
      : dueStatus === "due-soon"
      ? "var(--amber-dim)"
      : "transparent";

  return (
    <div
      className="group flex flex-col gap-2 p-4 rounded-xl border transition-all duration-150"
      style={{
        background: "var(--bg-card)",
        borderColor: "var(--border)",
      }}
      onMouseEnter={(e) =>
        ((e.currentTarget as HTMLDivElement).style.borderColor =
          "var(--border-bright)")
      }
      onMouseLeave={(e) =>
        ((e.currentTarget as HTMLDivElement).style.borderColor =
          "var(--border)")
      }
    >
      {/* Top row */}
      <div className="flex items-start gap-3">
        {/* Type badge */}
        <span
          className="mt-0.5 shrink-0 text-[10px] font-medium px-2 py-0.5 rounded-md"
          style={{
            background: "var(--bg-elevated)",
            color: "var(--text-muted)",
            border: "1px solid var(--border)",
          }}
        >
          {WORK_TYPE_LABEL[assignment.workType] ?? assignment.workType}
        </span>

        {/* Title & description */}
        <div className="flex-1 min-w-0">
          <p
            className="text-sm font-semibold leading-snug"
            style={{ color: "var(--text-primary)" }}
          >
            {assignment.title}
          </p>
          {assignment.description && (
            <p
              className="text-xs mt-1 leading-relaxed line-clamp-2"
              style={{ color: "var(--text-secondary)" }}
            >
              {assignment.description}
            </p>
          )}
        </div>

        {/* Points */}
        {assignment.maxPoints != null && (
          <span
            className="shrink-0 text-xs font-mono font-medium"
            style={{ color: "var(--text-muted)" }}
          >
            {assignment.maxPoints} pts
          </span>
        )}
      </div>

      {/* Bottom row */}
      <div className="flex items-center gap-2 flex-wrap">
        {/* Due date */}
        <span
          className="text-[11px] font-medium px-2 py-0.5 rounded-md"
          style={{
            color: dueColor,
            background: dueBg,
            border: dueStatus !== "none" ? `1px solid ${dueColor}30` : "none",
          }}
        >
          {dueStatus === "overdue" ? "⚠ Overdue · " : "Due · "}
          {dueLabel}
        </span>

        {/* Updated time */}
        {/* <span
          className="text-[11px]"
          style={{ color: "var(--text-muted)" }}
        >
          Updated {timeAgo(assignment.updateTime ?? new Date().toISOString())}
        </span> */}

        {/* Materials */}
        {assignment.materials.length > 0 && (
          <span
            className="ml-auto flex items-center gap-1 text-[11px]"
            style={{ color: "var(--text-muted)" }}
          >
            <PaperclipIcon />
            {assignment.materials.length} attachment
            {assignment.materials.length !== 1 ? "s" : ""}
          </span>
        )}
      </div>

      {/* Material pills */}
      {assignment.materials.length > 0 && (
        <div className="flex flex-wrap gap-1.5 pt-1">
          {assignment.materials.slice(0, 4).map((m, i) => (
            <a
              key={i}
              href={m.url ?? "#"}
              target="_blank"
              rel="noopener noreferrer"
              onClick={(e) => !m.url && e.preventDefault()}
              className="flex items-center gap-1.5 text-[11px] px-2.5 py-1 rounded-lg transition-colors duration-150"
              style={{
                background: "var(--bg-elevated)",
                color: "var(--text-secondary)",
                border: "1px solid var(--border)",
                cursor: m.url ? "pointer" : "default",
              }}
              onMouseEnter={(e) => {
                if (m.url)
                  (e.currentTarget as HTMLAnchorElement).style.color =
                    "var(--accent)";
              }}
              onMouseLeave={(e) =>
                ((e.currentTarget as HTMLAnchorElement).style.color =
                  "var(--text-secondary)")
              }
            >
              <MaterialIcon type={m.type} />
              <span className="max-w-[120px] truncate">{m.title}</span>
            </a>
          ))}
          {assignment.materials.length > 4 && (
            <span
              className="text-[11px] px-2.5 py-1 rounded-lg"
              style={{
                background: "var(--bg-elevated)",
                color: "var(--text-muted)",
                border: "1px solid var(--border)",
              }}
            >
              +{assignment.materials.length - 4} more
            </span>
          )}
        </div>
      )}
    </div>
  );
}

function PaperclipIcon() {
  return (
    <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="m21.44 11.05-9.19 9.19a6 6 0 0 1-8.49-8.49l8.57-8.57A4 4 0 1 1 18 8.84l-8.59 8.57a2 2 0 0 1-2.83-2.83l8.49-8.48" />
    </svg>
  );
}

function MaterialIcon({ type }: { type: string }) {
  if (type === "driveFile")
    return (
      <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
        <path d="M14.5 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7.5L14.5 2z" />
        <polyline points="14 2 14 8 20 8" />
      </svg>
    );
  if (type === "youtubeVideo")
    return (
      <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
        <polygon points="5 3 19 12 5 21 5 3" />
      </svg>
    );
  return (
    <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71" />
      <path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71" />
    </svg>
  );
}