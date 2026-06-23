"use client";

import React, { createContext, useContext, useState, useCallback } from "react";
import { UserDto } from "@/types";

interface AuthState {
  user: UserDto | null;
  token: string | null;
  isLoading: boolean;
}

interface AuthContextValue extends AuthState {
  login: (token: string, user: UserDto) => void;
  logout: () => void;
  refreshUser: (user: UserDto) => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  // Initialize from localStorage synchronously to avoid setting state inside an effect
  const [state, setState] = useState<AuthState>(() => {
    const storedToken = typeof window !== "undefined" ? localStorage.getItem("classmate_token") : null;
    const storedUser = typeof window !== "undefined" ? localStorage.getItem("classmate_user") : null;

    let user: UserDto | null = null;
    let token: string | null = null;

    if (storedToken && storedUser) {
      try {
        token = storedToken;
        user = JSON.parse(storedUser);
      } catch (e) {
        if (typeof window !== "undefined") {
          localStorage.removeItem("classmate_token");
          localStorage.removeItem("classmate_user");
        }
      }
    }

    return { user, token, isLoading: false };
  });

  const login = useCallback((token: string, user: UserDto) => {
    localStorage.setItem("classmate_token", token);
    localStorage.setItem("classmate_user", JSON.stringify(user));
    setState({ token, user, isLoading: false });
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem("classmate_token");
    localStorage.removeItem("classmate_user");
    setState({ token: null, user: null, isLoading: false });
  }, []);

  const refreshUser = useCallback((user: UserDto) => {
    localStorage.setItem("classmate_user", JSON.stringify(user));
    setState(prev => ({ ...prev, user }));
  }, []);

  return (
    <AuthContext.Provider
      value={{ ...state, login, logout, refreshUser }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}