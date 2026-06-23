"use client";

import { useEffect, useState } from "react";
import { signIn, useSession } from "next-auth/react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { classroomApi } from "@/lib/api";


export default function LoginPage() {
  const { data: session, status } = useSession();
  const { login, token } = useAuth();
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Already have our JWT → go to dashboard
  useEffect(() => {
    if (token) router.replace("/dashboard");
  }, [token, router]);

  // NextAuth session arrived → exchange with our backend
  useEffect(() => {
    if (status !== "authenticated" || !session) return;

    const exchange = async () => {
      setIsLoading(true);
      setError(null);
      try {
        const res = await classroomApi.googleLogin({
          idToken: session.idToken,
          accessToken: session.accessToken,
          refreshToken: session.refreshToken,
        });
        login(res.token, res.user);
        router.replace("/dashboard");
      } catch (err) {
        setError(
          err instanceof Error ? err.message : "Authentication failed. Please try again."
        );
        setIsLoading(false);
      }
    };

    exchange();
  }, [session, status, login, router]);

  const handleGoogleSignIn = async () => {
    setIsLoading(true);
    setError(null);
    await signIn("google");
  };

  return (
    <main className="min-h-screen flex flex-col items-center justify-center px-4 relative overflow-hidden">
      {/* Background grid */}
      <div
        className="pointer-events-none absolute inset-0 opacity-[0.03]"
        style={{
          backgroundImage:
            "linear-gradient(var(--border) 1px, transparent 1px), linear-gradient(90deg, var(--border) 1px, transparent 1px)",
          backgroundSize: "40px 40px",
        }}
      />
      {/* Glow orb */}
      <div
        className="pointer-events-none absolute top-1/3 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[600px] h-[600px] rounded-full opacity-10"
        style={{
          background:
            "radial-gradient(circle, var(--accent) 0%, transparent 70%)",
        }}
      />

      <div className="relative z-10 w-full max-w-sm animate-fade-up">
        {/* Logo mark */}
        <div className="flex justify-center mb-8">
          <div className="relative">
            <div
              className="w-14 h-14 rounded-2xl flex items-center justify-center text-2xl font-bold"
              style={{
                background:
                  "linear-gradient(135deg, var(--accent) 0%, var(--accent-dim) 100%)",
                boxShadow: "0 0 40px var(--accent-glow)",
              }}
            >
              C
            </div>
          </div>
        </div>

        {/* Heading */}
        <div className="text-center mb-10">
          <h1
            className="text-3xl font-bold tracking-tight mb-2"
            style={{ fontFamily: "var(--font-display)" }}
          >
            ClassMate AI
          </h1>
          <p className="text-sm" style={{ color: "var(--text-secondary)" }}>
            Your assignments, handled intelligently.
          </p>
        </div>

        {/* Card */}
        <div
          className="rounded-2xl p-8 border"
          style={{
            background: "var(--bg-card)",
            borderColor: "var(--border)",
          }}
        >
          <p
            className="text-sm text-center mb-6"
            style={{ color: "var(--text-secondary)" }}
          >
            Sign in with your Google account to connect your Classroom courses.
          </p>

          {error && (
            <div
              className="mb-4 px-4 py-3 rounded-xl text-sm"
              style={{
                background: "var(--red-dim)",
                color: "var(--red)",
                border: "1px solid rgba(240,84,106,0.25)",
              }}
            >
              {error}
            </div>
          )}

          <button
            onClick={handleGoogleSignIn}
            disabled={isLoading}
            className="w-full flex items-center justify-center gap-3 px-4 py-3 rounded-xl font-medium text-sm transition-all duration-200 disabled:opacity-60 disabled:cursor-not-allowed"
            style={{
              background: "var(--bg-elevated)",
              border: "1px solid var(--border-bright)",
              color: "var(--text-primary)",
            }}
            onMouseEnter={(e) =>
              ((e.currentTarget as HTMLButtonElement).style.borderColor =
                "var(--accent)")
            }
            onMouseLeave={(e) =>
              ((e.currentTarget as HTMLButtonElement).style.borderColor =
                "var(--border-bright)")
            }
          >
            {isLoading ? (
              <span className="spinner" />
            ) : (
              <GoogleIcon />
            )}
            <span>
              {isLoading ? "Connecting…" : "Continue with Google"}
            </span>
          </button>

          <p
            className="mt-5 text-xs text-center leading-relaxed"
            style={{ color: "var(--text-muted)" }}
          >
            We request Classroom read and Drive write access to generate and
            upload assignment drafts on your behalf.
          </p>
        </div>

        {/* Disclaimer */}
        <p
          className="mt-6 text-xs text-center leading-relaxed px-2"
          style={{ color: "var(--text-muted)" }}
        >
          This tool assists with drafts — you are responsible for reviewing and
          submitting your own work.
        </p>
      </div>
    </main>
  );
}

function GoogleIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 18 18" fill="none">
      <path
        d="M17.64 9.2c0-.637-.057-1.251-.164-1.84H9v3.481h4.844c-.209 1.125-.843 2.078-1.796 2.717v2.258h2.908c1.702-1.567 2.684-3.875 2.684-6.615z"
        fill="#4285F4"
      />
      <path
        d="M9 18c2.43 0 4.467-.806 5.956-2.18l-2.908-2.259c-.806.54-1.837.86-3.048.86-2.344 0-4.328-1.584-5.036-3.711H.957v2.332A8.997 8.997 0 0 0 9 18z"
        fill="#34A853"
      />
      <path
        d="M3.964 10.71A5.41 5.41 0 0 1 3.682 9c0-.593.102-1.17.282-1.71V4.958H.957A8.996 8.996 0 0 0 0 9c0 1.452.348 2.827.957 4.042l3.007-2.332z"
        fill="#FBBC05"
      />
      <path
        d="M9 3.58c1.321 0 2.508.454 3.44 1.345l2.582-2.58C13.463.891 11.426 0 9 0A8.997 8.997 0 0 0 .957 4.958L3.964 7.29C4.672 5.163 6.656 3.58 9 3.58z"
        fill="#EA4335"
      />
    </svg>
  );
}