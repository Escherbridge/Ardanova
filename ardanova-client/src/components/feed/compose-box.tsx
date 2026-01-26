"use client";

import { useState, useRef } from "react";
import { motion, AnimatePresence } from "framer-motion";
import {
  Image,
  FileVideo,
  AtSign,
  Hash,
  Smile,
  Send,
  X,
  ChevronDown,
  FolderKanban,
  Users,
  Globe,
} from "lucide-react";

import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "~/components/ui/dropdown-menu";
import { cn } from "~/lib/utils";

interface ComposeBoxProps {
  user: {
    name: string;
    avatar?: string;
  };
  onSubmit?: (post: {
    text: string;
    scope: { type: string; id?: string; name?: string };
    media?: File[];
  }) => void;
  placeholder?: string;
  scopes?: { type: string; id: string; name: string }[];
}

export function ComposeBox({
  user,
  onSubmit,
  placeholder = "What's happening?",
  scopes = [],
}: ComposeBoxProps) {
  const [text, setText] = useState("");
  const [isFocused, setIsFocused] = useState(false);
  const [scope, setScope] = useState<{
    type: string;
    id?: string;
    name?: string;
  }>({ type: "public" });
  const [media, setMedia] = useState<File[]>([]);
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const maxLength = 500;
  const remaining = maxLength - text.length;

  const handleSubmit = () => {
    if (text.trim() || media.length > 0) {
      onSubmit?.({ text: text.trim(), scope, media });
      setText("");
      setMedia([]);
      setIsFocused(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" && (e.metaKey || e.ctrlKey)) {
      e.preventDefault();
      handleSubmit();
    }
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files || []);
    setMedia((prev) => [...prev, ...files].slice(0, 4));
  };

  const removeMedia = (index: number) => {
    setMedia((prev) => prev.filter((_, i) => i !== index));
  };

  const scopeIcons = {
    public: Globe,
    project: FolderKanban,
    guild: Users,
  };

  const ScopeIcon = scopeIcons[scope.type as keyof typeof scopeIcons] || Globe;

  return (
    <div className="border-b-2 border-border bg-card">
      <div className="p-4">
        <div className="flex gap-3">
          <Avatar className="size-10 border-2 border-border shrink-0">
            <AvatarImage src={user.avatar} alt={user.name} />
            <AvatarFallback>{user.name.charAt(0)}</AvatarFallback>
          </Avatar>

          <div className="flex-1 min-w-0">
            {/* Scope selector */}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <button className="flex items-center gap-1.5 text-sm text-primary hover:text-primary/80 transition-colors mb-2">
                  <ScopeIcon className="size-3.5" />
                  <span>{scope.name || "Everyone"}</span>
                  <ChevronDown className="size-3.5" />
                </button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start">
                <DropdownMenuItem
                  onClick={() => setScope({ type: "public" })}
                >
                  <Globe className="size-4 mr-2" />
                  Everyone
                </DropdownMenuItem>
                {scopes.map((s) => (
                  <DropdownMenuItem
                    key={s.id}
                    onClick={() =>
                      setScope({ type: s.type, id: s.id, name: s.name })
                    }
                  >
                    {s.type === "project" ? (
                      <FolderKanban className="size-4 mr-2" />
                    ) : (
                      <Users className="size-4 mr-2" />
                    )}
                    {s.name}
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>

            {/* Text input */}
            <textarea
              ref={textareaRef}
              value={text}
              onChange={(e) => setText(e.target.value)}
              onFocus={() => setIsFocused(true)}
              onKeyDown={handleKeyDown}
              placeholder={placeholder}
              rows={isFocused ? 3 : 1}
              maxLength={maxLength}
              className={cn(
                "w-full bg-transparent text-foreground placeholder:text-muted-foreground resize-none outline-none text-lg transition-all",
                isFocused && "min-h-[80px]"
              )}
            />

            {/* Media previews */}
            <AnimatePresence>
              {media.length > 0 && (
                <motion.div
                  initial={{ opacity: 0, height: 0 }}
                  animate={{ opacity: 1, height: "auto" }}
                  exit={{ opacity: 0, height: 0 }}
                  className="grid grid-cols-2 gap-2 mt-3"
                >
                  {media.map((file, i) => (
                    <div
                      key={i}
                      className="relative aspect-video bg-secondary border-2 border-border"
                    >
                      <img
                        src={URL.createObjectURL(file)}
                        alt=""
                        className="w-full h-full object-cover"
                      />
                      <button
                        onClick={() => removeMedia(i)}
                        className="absolute top-1 right-1 p-1 bg-black/50 text-white hover:bg-black/70 transition-colors"
                      >
                        <X className="size-3" />
                      </button>
                    </div>
                  ))}
                </motion.div>
              )}
            </AnimatePresence>

            {/* Actions */}
            <AnimatePresence>
              {(isFocused || text.length > 0) && (
                <motion.div
                  initial={{ opacity: 0, y: -10 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -10 }}
                  className="flex items-center justify-between mt-3 pt-3 border-t border-border"
                >
                  <div className="flex items-center gap-1">
                    <input
                      ref={fileInputRef}
                      type="file"
                      accept="image/*,video/*"
                      multiple
                      className="hidden"
                      onChange={handleFileSelect}
                    />
                    <Button
                      variant="ghost"
                      size="icon-sm"
                      onClick={() => fileInputRef.current?.click()}
                      disabled={media.length >= 4}
                    >
                      <Image className="size-4 text-primary" />
                    </Button>
                    <Button variant="ghost" size="icon-sm">
                      <FileVideo className="size-4 text-primary" />
                    </Button>
                    <Button variant="ghost" size="icon-sm">
                      <AtSign className="size-4 text-primary" />
                    </Button>
                    <Button variant="ghost" size="icon-sm">
                      <Hash className="size-4 text-primary" />
                    </Button>
                    <Button variant="ghost" size="icon-sm">
                      <Smile className="size-4 text-primary" />
                    </Button>
                  </div>

                  <div className="flex items-center gap-3">
                    {/* Character count */}
                    <span
                      className={cn(
                        "text-xs",
                        remaining < 50
                          ? remaining < 0
                            ? "text-destructive"
                            : "text-neon-yellow"
                          : "text-muted-foreground"
                      )}
                    >
                      {remaining}
                    </span>

                    <Button
                      variant="neon"
                      size="sm"
                      onClick={handleSubmit}
                      disabled={text.trim().length === 0 && media.length === 0}
                    >
                      <Send className="size-4 mr-1.5" />
                      Post
                    </Button>
                  </div>
                </motion.div>
              )}
            </AnimatePresence>
          </div>
        </div>
      </div>
    </div>
  );
}
