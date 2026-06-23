'use client';
import { AssignmentDto } from '@/types';

interface Props {
  assignments: AssignmentDto[];
  loading: boolean;
  error?: string;
}

export function AssignmentList({ assignments, loading, error }: Props) {
  if (loading) return (
    <div className="space-y-3">
      {[1, 2, 3].map(i => (
        <div key={i} className="h-16 rounded-xl bg-white/[0.03] animate-pulse" />
      ))}
    </div>
  );

  if (error) return (
    <div className="rounded-xl bg-rose-500/10 border border-rose-500/20 px-4 py-3">
      <p className="text-sm text-rose-400">{error}</p>
    </div>
  );

  if (assignments.length === 0) return (
    <div className="text-center py-10">
      <p className="text-sm text-zinc-500">No assignments found in this course.</p>
    </div>
  );

  return (
    <div className="space-y-2">
      {assignments.map(a => (
        <AssignmentRow key={a.assignmentId} assignment={a} />
      ))}
    </div>
  );
}

function AssignmentRow({ assignment: a }: { assignment: AssignmentDto }) {
  const due = a.dueDate ? new Date(a.dueDate) : null;
  const now = new Date();
  const overdue = due && due < now;
  const soon = due && !overdue && (due.getTime() - now.getTime()) < 24 * 60 * 60 * 1000;

  return (
    <div className="
      flex items-start gap-4 bg-white/[0.03] border border-white/[0.07]
      rounded-xl px-4 py-3.5 hover:bg-white/[0.05] transition-colors
    ">
      {/* Work type icon */}
      <div className="mt-0.5 w-7 h-7 rounded-lg bg-indigo-500/10 flex items-center justify-center flex-shrink-0">
        <svg className="w-3.5 h-3.5 text-indigo-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
          {a.workType === 'ASSIGNMENT'
            ? <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            : <path strokeLinecap="round" strokeLinejoin="round" d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          }
        </svg>
      </div>

      {/* Info */}
      <div className="flex-1 min-w-0">
        <p className="text-sm font-medium text-white truncate">{a.title}</p>
        {a.description && (
          <p className="text-xs text-zinc-500 mt-0.5 line-clamp-1">{a.description}</p>
        )}
        {/* Materials count */}
        {a.materials.length > 0 && (
          <p className="text-xs text-zinc-600 mt-1">
            {a.materials.length} attachment{a.materials.length !== 1 ? 's' : ''}
          </p>
        )}
      </div>

      {/* Due date */}
      <div className="flex flex-col items-end gap-1.5 flex-shrink-0">
        {a.maxPoints != null && (
          <span className="text-xs text-zinc-500">{a.maxPoints} pts</span>
        )}
        {due ? (
          <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${
            overdue ? 'bg-rose-500/10 text-rose-400' :
            soon    ? 'bg-amber-500/10 text-amber-400' :
                      'bg-zinc-800 text-zinc-400'
          }`}>
            {overdue ? 'Past due' :
             soon    ? 'Due soon' :
             due.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
          </span>
        ) : (
          <span className="text-xs text-zinc-600">No due date</span>
        )}
      </div>
    </div>
  );
}