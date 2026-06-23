// "use client";

// import { useEffect, useState } from "react";
// import { useRouter } from "next/navigation";
// import { useAuth } from "@/lib/auth-context";
// import { getMe } from "@/lib/api";
// import Navbar from "@/Components/Navbar";

// const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

// function getToken() {
//   if (typeof window === "undefined") return null;
//   return localStorage.getItem("classmate_token");
// }

// async function apiPost(path: string, body: object) {
//   const token = getToken();
//   const res = await fetch(`${BASE_URL}${path}`, {
//     method: "POST",
//     headers: {
//       "Content-Type": "application/json",
//       Authorization: `Bearer ${token}`,
//     },
//     body: JSON.stringify(body),
//   });
//   if (!res.ok) {
//     const text = await res.text();
//     throw new Error(text || `Request failed: ${res.status}`);
//   }
//   if (res.status === 204) return;
//   return res.json();
// }

// async function apiDelete(path: string) {
//   const token = getToken();
//   const res = await fetch(`${BASE_URL}${path}`, {
//     method: "DELETE",
//     headers: { Authorization: `Bearer ${token}` },
//   });
//   if (!res.ok) throw new Error(`Request failed: ${res.status}`);
// }

// export default function SettingsPage() {
//   const { token, isLoading: authLoading, user, refreshUser } = useAuth();
//   const router = useRouter();

//   const [openAiKey, setOpenAiKey] = useState("");
//   const [geminiKey, setGeminiKey] = useState("");
//   const [notificationEmail, setNotificationEmail] = useState(
//     user?.notificationEmail ?? ""
//   );
//   const [saving, setSaving] = useState<string | null>(null);
//   const [success, setSuccess] = useState<string | null>(null);
//   const [error, setError] = useState<string | null>(null);

//   useEffect(() => {
//     if (!authLoading && !token) router.replace("/login");
//   }, [authLoading, token, router]);

//   useEffect(() => {
//     if (user) setNotificationEmail(user.notificationEmail);
//   }, [user]);

//   const showSuccess = (msg: string) => {
//     setSuccess(msg);
//     setError(null);
//     setTimeout(() => setSuccess(null), 3000);
//   };

//   const showError = (msg: string) => {
//     setError(msg);
//     setSuccess(null);
//   };

//   const handleSaveOpenAi = async () => {
//     if (!openAiKey.trim()) return showError("Please enter a valid OpenAI key.");
//     setSaving("openai");
//     try {
//       await apiPost("/api/user/settings/openai-key", { key: openAiKey.trim() });
//       setOpenAiKey("");
//       const updated = await getMe();
//       refreshUser(updated);
//       showSuccess("OpenAI key saved successfully.");
//     } catch (e) {
//       showError(e instanceof Error ? e.message : "Failed to save key.");
//     } finally {
//       setSaving(null);
//     }
//   };

//   const handleSaveGemini = async () => {
//     if (!geminiKey.trim()) return showError("Please enter a valid Gemini key.");
//     setSaving("gemini");
//     try {
//       await apiPost("/api/user/settings/gemini-key", { key: geminiKey.trim() });
//       setGeminiKey("");
//       const updated = await getMe();
//       refreshUser(updated);
//       showSuccess("Gemini key saved successfully.");
//     } catch (e) {
//       showError(e instanceof Error ? e.message : "Failed to save key.");
//     } finally {
//       setSaving(null);
//     }
//   };

//   const handleDeleteKey = async (type: "openai" | "gemini") => {
//     setSaving(`delete-${type}`);
//     try {
//       await apiDelete(`/api/user/settings/${type}-key`);
//       const updated = await getMe();
//       refreshUser(updated);
//       showSuccess(`${type === "openai" ? "OpenAI" : "Gemini"} key removed.`);
//     } catch (e) {
//       showError(e instanceof Error ? e.message : "Failed to remove key.");
//     } finally {
//       setSaving(null);
//     }
//   };

//   const handleSaveEmail = async () => {
//     if (!notificationEmail.trim()) return showError("Email cannot be empty.");
//     setSaving("email");
//     try {
//       await apiPost("/api/user/settings/notification-email", {
//         email: notificationEmail.trim(),
//       });
//       const updated = await getMe();
//       refreshUser(updated);
//       showSuccess("Notification email updated.");
//     } catch (e) {
//       showError(e instanceof Error ? e.message : "Failed to update email.");
//     } finally {
//       setSaving(null);
//     }
//   };

//   if (authLoading)
//     return (
//       <div className="min-h-screen flex items-center justify-center">
//         <span className="spinner" style={{ width: 28, height: 28 }} />
//       </div>
//     );

//   return (
//     <div className="min-h-screen flex flex-col">
//       <Navbar />
//       <main className="flex-1 px-4 sm:px-6 py-8 max-w-2xl mx-auto w-full">
//         <div className="animate-fade-up">
//           <h1
//             className="text-xl font-bold mb-1"
//             style={{ fontFamily: "var(--font-display)" }}
//           >
//             Settings
//           </h1>
//           <p className="text-xs mb-8" style={{ color: "var(--text-muted)" }}>
//             Manage your API keys and notification preferences.
//           </p>

//           {/* Toast */}
//           {success && (
//             <div
//               className="mb-5 px-4 py-3 rounded-xl text-sm border animate-fade-in"
//               style={{
//                 background: "var(--green-dim)",
//                 color: "var(--green)",
//                 borderColor: "rgba(61,214,140,0.25)",
//               }}
//             >
//               ✓ {success}
//             </div>
//           )}
//           {error && (
//             <div
//               className="mb-5 px-4 py-3 rounded-xl text-sm border animate-fade-in"
//               style={{
//                 background: "var(--red-dim)",
//                 color: "var(--red)",
//                 borderColor: "rgba(240,84,106,0.25)",
//               }}
//             >
//               {error}
//             </div>
//           )}

//           {/* Usage badge */}
//           {user && (
//             <div
//               className="mb-6 flex items-center justify-between px-4 py-3 rounded-xl border"
//               style={{
//                 background: "var(--bg-card)",
//                 borderColor: "var(--border)",
//               }}
//             >
//               <div>
//                 <p className="text-sm font-medium" style={{ color: "var(--text-primary)" }}>
//                   Free tier usage
//                 </p>
//                 <p className="text-xs mt-0.5" style={{ color: "var(--text-muted)" }}>
//                   Each AI generation or revision counts as one use.
//                 </p>
//               </div>
//               <span
//                 className="text-xl font-bold font-mono"
//                 style={{
//                   color:
//                     user.freeUsagesRemaining > 0
//                       ? "var(--green)"
//                       : "var(--red)",
//                 }}
//               >
//                 {user.freeUsagesRemaining}
//                 <span
//                   className="text-xs font-normal ml-1"
//                   style={{ color: "var(--text-muted)" }}
//                 >
//                   / 5 left
//                 </span>
//               </span>
//             </div>
//           )}

//           {/* OpenAI Key */}
//           <Section
//             title="OpenAI API Key"
//             description="Used for GPT-4o powered draft generation. Get yours at platform.openai.com."
//             badge={user?.hasOpenAiKey ? "Key saved" : undefined}
//             badgeColor="green"
//           >
//             {user?.hasOpenAiKey ? (
//               <div className="flex items-center justify-between gap-3">
//                 <span
//                   className="text-sm font-mono"
//                   style={{ color: "var(--text-muted)" }}
//                 >
//                   sk-•••••••••••••••••••••••xxxx
//                 </span>
//                 <button
//                   onClick={() => handleDeleteKey("openai")}
//                   disabled={saving === "delete-openai"}
//                   className="text-xs px-3 py-1.5 rounded-lg transition-colors duration-150 disabled:opacity-50"
//                   style={{
//                     color: "var(--red)",
//                     border: "1px solid rgba(240,84,106,0.3)",
//                     background: "var(--red-dim)",
//                   }}
//                 >
//                   {saving === "delete-openai" ? (
//                     <span className="spinner" style={{ width: 12, height: 12 }} />
//                   ) : (
//                     "Remove"
//                   )}
//                 </button>
//               </div>
//             ) : (
//               <div className="flex gap-2">
//                 <input
//                   type="password"
//                   value={openAiKey}
//                   onChange={(e) => setOpenAiKey(e.target.value)}
//                   placeholder="sk-..."
//                   className="flex-1 text-sm px-3 py-2.5 rounded-xl font-mono"
//                   style={{
//                     background: "var(--bg-base)",
//                     border: "1px solid var(--border-bright)",
//                     color: "var(--text-primary)",
//                     outline: "none",
//                   }}
//                   onFocus={(e) =>
//                     ((e.target as HTMLInputElement).style.borderColor =
//                       "var(--accent)")
//                   }
//                   onBlur={(e) =>
//                     ((e.target as HTMLInputElement).style.borderColor =
//                       "var(--border-bright)")
//                   }
//                 />
//                 <SaveButton
//                   onClick={handleSaveOpenAi}
//                   loading={saving === "openai"}
//                 />
//               </div>
//             )}
//           </Section>

//           {/* Gemini Key */}
//           <Section
//             title="Gemini API Key"
//             description="Alternative to OpenAI. Get yours at aistudio.google.com."
//             badge={user?.hasGeminiKey ? "Key saved" : undefined}
//             badgeColor="green"
//           >
//             {user?.hasGeminiKey ? (
//               <div className="flex items-center justify-between gap-3">
//                 <span
//                   className="text-sm font-mono"
//                   style={{ color: "var(--text-muted)" }}
//                 >
//                   AIza•••••••••••••••••••••xxxx
//                 </span>
//                 <button
//                   onClick={() => handleDeleteKey("gemini")}
//                   disabled={saving === "delete-gemini"}
//                   className="text-xs px-3 py-1.5 rounded-lg transition-colors duration-150 disabled:opacity-50"
//                   style={{
//                     color: "var(--red)",
//                     border: "1px solid rgba(240,84,106,0.3)",
//                     background: "var(--red-dim)",
//                   }}
//                 >
//                   {saving === "delete-gemini" ? (
//                     <span className="spinner" style={{ width: 12, height: 12 }} />
//                   ) : (
//                     "Remove"
//                   )}
//                 </button>
//               </div>
//             ) : (
//               <div className="flex gap-2">
//                 <input
//                   type="password"
//                   value={geminiKey}
//                   onChange={(e) => setGeminiKey(e.target.value)}
//                   placeholder="AIza..."
//                   className="flex-1 text-sm px-3 py-2.5 rounded-xl font-mono"
//                   style={{
//                     background: "var(--bg-base)",
//                     border: "1px solid var(--border-bright)",
//                     color: "var(--text-primary)",
//                     outline: "none",
//                   }}
//                   onFocus={(e) =>
//                     ((e.target as HTMLInputElement).style.borderColor =
//                       "var(--accent)")
//                   }
//                   onBlur={(e) =>
//                     ((e.target as HTMLInputElement).style.borderColor =
//                       "var(--border-bright)")
//                   }
//                 />
//                 <SaveButton
//                   onClick={handleSaveGemini}
//                   loading={saving === "gemini"}
//                 />
//               </div>
//             )}
//           </Section>

//           {/* Notification email */}
//           <Section
//             title="Notification Email"
//             description="Receive upload confirmations with draft links at this address."
//           >
//             <div className="flex gap-2">
//               <input
//                 type="email"
//                 value={notificationEmail}
//                 onChange={(e) => setNotificationEmail(e.target.value)}
//                 placeholder="you@example.com"
//                 className="flex-1 text-sm px-3 py-2.5 rounded-xl"
//                 style={{
//                   background: "var(--bg-base)",
//                   border: "1px solid var(--border-bright)",
//                   color: "var(--text-primary)",
//                   outline: "none",
//                 }}
//                 onFocus={(e) =>
//                   ((e.target as HTMLInputElement).style.borderColor =
//                     "var(--accent)")
//                 }
//                 onBlur={(e) =>
//                   ((e.target as HTMLInputElement).style.borderColor =
//                     "var(--border-bright)")
//                 }
//               />
//               <SaveButton
//                 onClick={handleSaveEmail}
//                 loading={saving === "email"}
//               />
//             </div>
//           </Section>

//           {/* Account info */}
//           {user && (
//             <Section title="Account">
//               <div className="space-y-2">
//                 <InfoRow label="Name" value={user.displayName} />
//                 <InfoRow label="Email" value={user.email} />
//                 <InfoRow label="User ID" value={String(user.id)} mono />
//               </div>
//             </Section>
//           )}
//         </div>
//       </main>
//     </div>
//   );
// }

// function Section({
//   title,
//   description,
//   badge,
//   badgeColor,
//   children,
// }: {
//   title: string;
//   description?: string;
//   badge?: string;
//   badgeColor?: "green" | "amber";
//   children: React.ReactNode;
// }) {
//   return (
//     <div
//       className="mb-4 rounded-2xl border overflow-hidden"
//       style={{
//         background: "var(--bg-card)",
//         borderColor: "var(--border)",
//       }}
//     >
//       <div
//         className="flex items-start justify-between gap-2 px-5 py-4 border-b"
//         style={{ borderColor: "var(--border)" }}
//       >
//         <div>
//           <h2 className="text-sm font-semibold" style={{ color: "var(--text-primary)" }}>
//             {title}
//           </h2>
//           {description && (
//             <p className="text-xs mt-0.5 leading-relaxed" style={{ color: "var(--text-muted)" }}>
//               {description}
//             </p>
//           )}
//         </div>
//         {badge && (
//           <span
//             className="shrink-0 text-[11px] font-medium px-2 py-0.5 rounded-full"
//             style={{
//               background:
//                 badgeColor === "green" ? "var(--green-dim)" : "var(--amber-dim)",
//               color:
//                 badgeColor === "green" ? "var(--green)" : "var(--amber)",
//             }}
//           >
//             ✓ {badge}
//           </span>
//         )}
//       </div>
//       <div className="px-5 py-4">{children}</div>
//     </div>
//   );
// }

// function InfoRow({
//   label,
//   value,
//   mono,
// }: {
//   label: string;
//   value: string;
//   mono?: boolean;
// }) {
//   return (
//     <div className="flex items-center justify-between text-sm">
//       <span style={{ color: "var(--text-muted)" }}>{label}</span>
//       <span
//         style={{
//           color: "var(--text-secondary)",
//           fontFamily: mono ? "var(--font-mono)" : undefined,
//           fontSize: mono ? "12px" : undefined,
//         }}
//       >
//         {value}
//       </span>
//     </div>
//   );
// }

// function SaveButton({
//   onClick,
//   loading,
// }: {
//   onClick: () => void;
//   loading: boolean;
// }) {
//   return (
//     <button
//       onClick={onClick}
//       disabled={loading}
//       className="px-4 py-2.5 rounded-xl text-sm font-semibold transition-all duration-150 disabled:opacity-60 shrink-0"
//       style={{
//         background: "var(--accent)",
//         color: "#fff",
//       }}
//       onMouseEnter={(e) =>
//         ((e.currentTarget as HTMLButtonElement).style.background =
//           "var(--accent-dim)")
//       }
//       onMouseLeave={(e) =>
//         ((e.currentTarget as HTMLButtonElement).style.background =
//           "var(--accent)")
//       }
//     >
//       {loading ? (
//         <span className="spinner" style={{ width: 14, height: 14, borderTopColor: "#fff" }} />
//       ) : (
//         "Save"
//       )}
//     </button>
//   );
// }