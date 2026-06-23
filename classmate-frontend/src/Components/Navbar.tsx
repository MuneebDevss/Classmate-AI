"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { signOut } from "next-auth/react";
import { useAuth } from "@/lib/auth-context";

export default function Navbar() {
  const { user, logout } = useAuth();
  const router = useRouter();
  const pathname = usePathname();

  const handleLogout = async () => {
    logout();
    await signOut({ redirect: false });
    router.push("/login");
  };

  return (
    <header
      className="sticky top-0 z-50 flex items-center justify-between px-6 h-14 border-b"
      style={{
        background: "rgba(10,10,15,0.85)",
        backdropFilter: "blur(12px)",
        borderColor: "var(--border)",
      }}
    >
      {/* Brand */}
      <Link
        href="/dashboard"
        className="flex items-center gap-2.5 font-bold text-sm tracking-wide"
        style={{ fontFamily: "var(--font-display)" }}
      >
        <span
          className="w-7 h-7 rounded-lg flex items-center justify-center text-xs font-bold"
          style={{
            background:
              "linear-gradient(135deg, var(--accent) 0%, var(--accent-dim) 100%)",
          }}
        >
          C
        </span>
        ClassMate AI
      </Link>

      {/* Nav links */}
      <nav className="flex items-center gap-1">
        <NavLink href="/dashboard" active={pathname === "/dashboard"}>
          Dashboard
        </NavLink>
        <NavLink href="/settings" active={pathname === "/settings"}>
          Settings
        </NavLink>
      </nav>

      {/* User menu */}
      <div className="flex items-center gap-3">
        {user?.freeUsagesRemaining !== undefined && (
          <span
            className="hidden sm:flex items-center gap-1.5 text-xs px-2.5 py-1 rounded-full font-medium"
            style={{
              background:
                user.freeUsagesRemaining > 0
                  ? "var(--green-dim)"
                  : "var(--amber-dim)",
              color:
                user.freeUsagesRemaining > 0
                  ? "var(--green)"
                  : "var(--amber)",
            }}
          >
            <span
              className="w-1.5 h-1.5 rounded-full"
              style={{
                background:
                  user.freeUsagesRemaining > 0
                    ? "var(--green)"
                    : "var(--amber)",
              }}
            />
            {user.freeUsagesRemaining > 0
              ? `${user.freeUsagesRemaining} free left`
              : "API key required"}
          </span>
        )}
        {user?.avatarUrl ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={user.avatarUrl}
            alt={user.displayName}
            className="w-7 h-7 rounded-full object-cover"
            style={{ border: "1.5px solid var(--border-bright)" }}
          />
        ) : (
          <div
            className="w-7 h-7 rounded-full flex items-center justify-center text-xs font-semibold uppercase"
            style={{
              background: "var(--bg-elevated)",
              border: "1.5px solid var(--border-bright)",
              color: "var(--text-secondary)",
            }}
          >
            {user?.displayName?.[0] ?? "?"}
          </div>
        )}
        <button
          onClick={handleLogout}
          className="text-xs px-3 py-1.5 rounded-lg transition-all duration-150"
          style={{
            color: "var(--text-secondary)",
            border: "1px solid transparent",
          }}
          onMouseEnter={(e) => {
            (e.currentTarget as HTMLButtonElement).style.borderColor =
              "var(--border)";
            (e.currentTarget as HTMLButtonElement).style.color =
              "var(--text-primary)";
          }}
          onMouseLeave={(e) => {
            (e.currentTarget as HTMLButtonElement).style.borderColor =
              "transparent";
            (e.currentTarget as HTMLButtonElement).style.color =
              "var(--text-secondary)";
          }}
        >
          Sign out
        </button>
      </div>
    </header>
  );
}

function NavLink({
  href,
  active,
  children,
}: {
  href: string;
  active: boolean;
  children: React.ReactNode;
}) {
  return (
    <Link
      href={href}
      className="text-xs px-3 py-1.5 rounded-lg font-medium transition-all duration-150"
      style={{
        color: active ? "var(--text-primary)" : "var(--text-secondary)",
        background: active ? "var(--bg-elevated)" : "transparent",
      }}
    >
      {children}
    </Link>
  );
}