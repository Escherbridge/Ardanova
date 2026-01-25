"use client";

import { api } from "~/trpc/react";

export function Post() {
  return (
    <div className="text-center">
      <h1 className="text-2xl font-bold">Welcome to ArdaNova</h1>
      <p className="text-gray-600">The Social Network for Doing</p>
    </div>
  );
}
