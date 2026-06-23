export function formatDueDate(iso?: string): {
  label: string;
  status: "overdue" | "due-soon" | "upcoming" | "none";
} {
  if (!iso) return { label: "No due date", status: "none" };

  const due = new Date(iso);
  const now = new Date();
  const diffMs = due.getTime() - now.getTime();
  const diffHours = diffMs / (1000 * 60 * 60);
  const diffDays = diffMs / (1000 * 60 * 60 * 24);

  const label = due.toLocaleString(undefined, {
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  });

  if (diffMs < 0) return { label, status: "overdue" };
  if (diffHours < 24) return { label, status: "due-soon" };
  if (diffDays < 3) return { label, status: "upcoming" };
  return { label, status: "upcoming" };
}

export function timeAgo(iso: string): string {
  const date = new Date(iso);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / 60000);
  if (diffMins < 1) return "just now";
  if (diffMins < 60) return `${diffMins}m ago`;
  const diffHours = Math.floor(diffMins / 60);
  if (diffHours < 24) return `${diffHours}h ago`;
  const diffDays = Math.floor(diffHours / 24);
  return `${diffDays}d ago`;
}