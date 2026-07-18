"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import {
  ArrowLeft,
  Loader2,
  Plus,
  X,
  Info,
  Calendar,
  Clock,
  Users,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "~/components/ui/select";
import { useEnumOptions } from "~/hooks/use-enum";
import { api } from "~/trpc/react";
import { toast } from "sonner";

type EventFormat = "virtual" | "in-person" | "hybrid";

const eventFormats: readonly {
  id: EventFormat;
  label: string;
}[] = [
  { id: "virtual", label: "Virtual" },
  { id: "in-person", label: "In-Person" },
  { id: "hybrid", label: "Hybrid" },
];

function mapEventTypeIdToRouter(
  id: string,
): "meetup" | "workshop" | "hackathon" | "conference" | "webinar" | "ama" {
  const u = id.toUpperCase();
  if (u.includes("HACK")) return "hackathon";
  if (u.includes("CONFERENCE")) return "conference";
  if (u.includes("WEBINAR")) return "webinar";
  if (u.includes("AMA")) return "ama";
  if (u.includes("WORKSHOP")) return "workshop";
  if (u.includes("MEET")) return "meetup";
  return "meetup";
}

const timezones = [
  { id: "UTC", label: "UTC" },
  { id: "EST", label: "EST (UTC-5)" },
  { id: "PST", label: "PST (UTC-8)" },
  { id: "CET", label: "CET (UTC+1)" },
  { id: "IST", label: "IST (UTC+5:30)" },
  { id: "JST", label: "JST (UTC+9)" },
];

interface EventFormData {
  title: string;
  description: string;
  type: string;
  format: EventFormat;
  date: string;
  startTime: string;
  endTime: string;
  timezone: string;
  location: string;
  virtualLink: string;
  maxAttendees: string;
  tags: string[];
}

export default function CreateEventPage() {
  const router = useRouter();
  const createEvent = api.event.create.useMutation({
    onSuccess: () => {
      toast.success("Event created");
      router.push("/events");
    },
    onError: (e) => toast.error(e.message),
  });
  const { options: eventTypes } = useEnumOptions("EventType");
  const [formData, setFormData] = useState<EventFormData>({
    title: "",
    description: "",
    type: "",
    format: "virtual",
    date: "",
    startTime: "",
    endTime: "",
    timezone: "UTC",
    location: "",
    virtualLink: "",
    maxAttendees: "",
    tags: [],
  });
  const [newTag, setNewTag] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleChange = <Key extends keyof EventFormData>(
    field: Key,
    value: EventFormData[Key],
  ) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      });
    }
  };

  const addTag = () => {
    if (newTag.trim() && !formData.tags.includes(newTag.trim())) {
      setFormData((prev) => ({
        ...prev,
        tags: [...prev.tags, newTag.trim()],
      }));
      setNewTag("");
    }
  };

  const removeTag = (tag: string) => {
    setFormData((prev) => ({
      ...prev,
      tags: prev.tags.filter((t) => t !== tag),
    }));
  };

  const validateForm = () => {
    const newErrors: Record<string, string> = {};
    if (!formData.title.trim()) newErrors.title = "Title is required";
    if (!formData.description.trim())
      newErrors.description = "Description is required";
    if (formData.description.length < 20)
      newErrors.description = "Must be at least 20 characters";
    if (!formData.type) newErrors.type = "Event type is required";
    if (!formData.date) newErrors.date = "Date is required";
    if (!formData.startTime) newErrors.startTime = "Start time is required";
    if (
      (formData.format === "in-person" || formData.format === "hybrid") &&
      !formData.location.trim()
    ) {
      newErrors.location = "Location is required for in-person/hybrid events";
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return;

    setIsSubmitting(true);
    try {
      const type = mapEventTypeIdToRouter(formData.type);
      await createEvent.mutateAsync({
        title: formData.title.trim(),
        description: formData.description.trim(),
        type,
        format: formData.format,
        date: formData.date,
        startTime: formData.startTime,
        endTime: formData.endTime || undefined,
        timezone: formData.timezone,
        location: formData.location.trim() || undefined,
        virtualLink: formData.virtualLink.trim() || undefined,
        maxAttendees: /^\d+$/.test(formData.maxAttendees)
          ? parseInt(formData.maxAttendees, 10)
          : undefined,
        tags: formData.tags.length ? formData.tags.join(",") : undefined,
      });
    } catch (error) {
      setErrors({
        submit:
          error instanceof Error ? error.message : "Event could not be created",
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="bg-background min-h-screen">
      <div className="mx-auto max-w-2xl px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <Button variant="ghost" asChild className="mb-4 -ml-2">
            <Link href="/events">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Events
            </Link>
          </Button>
          <h1 className="flex items-center gap-3 text-3xl font-bold">
            <div className="bg-system/20 flex h-10 w-10 items-center justify-center rounded-none">
              <Calendar className="text-system h-5 w-5" />
            </div>
            Create Event
          </h1>
          <p className="text-muted-foreground mt-2">
            Organize a community event for members to join
          </p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Event Info */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">Event Details</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label
                  htmlFor="event-title"
                  className="mb-2 block text-sm font-medium"
                >
                  Event Title <span className="text-system">*</span>
                </label>
                <input
                  type="text"
                  id="event-title"
                  required
                  aria-invalid={Boolean(errors.title)}
                  aria-describedby={
                    errors.title ? "event-title-error" : undefined
                  }
                  value={formData.title}
                  onChange={(e) => handleChange("title", e.target.value)}
                  placeholder="e.g., Web3 Developer Workshop"
                  className={`bg-muted/50 focus-visible:ring-ring/50 w-full rounded-none border-2 px-4 py-3 focus:ring-2 focus:outline-none ${
                    errors.title ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.title && (
                  <p
                    id="event-title-error"
                    role="alert"
                    className="text-destructive mt-1 text-sm"
                  >
                    {errors.title}
                  </p>
                )}
              </div>

              <div>
                <label
                  htmlFor="event-description"
                  className="mb-2 block text-sm font-medium"
                >
                  Description <span className="text-system">*</span>
                </label>
                <textarea
                  id="event-description"
                  required
                  aria-invalid={Boolean(errors.description)}
                  aria-describedby={
                    errors.description ? "event-description-error" : undefined
                  }
                  value={formData.description}
                  onChange={(e) => handleChange("description", e.target.value)}
                  placeholder="Describe the event, what attendees will learn or experience..."
                  rows={4}
                  className={`bg-muted/50 focus-visible:ring-ring/50 w-full resize-none rounded-none border-2 px-4 py-3 focus:ring-2 focus:outline-none ${
                    errors.description ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.description && (
                  <p
                    id="event-description-error"
                    role="alert"
                    className="text-destructive mt-1 text-sm"
                  >
                    {errors.description}
                  </p>
                )}
              </div>

              <div>
                <label
                  htmlFor="event-type"
                  className="mb-2 block text-sm font-medium"
                >
                  Event Type <span className="text-system">*</span>
                </label>
                <Select
                  value={formData.type}
                  onValueChange={(value) => handleChange("type", value)}
                >
                  <SelectTrigger
                    id="event-type"
                    aria-required="true"
                    aria-invalid={Boolean(errors.type)}
                    aria-describedby={
                      errors.type ? "event-type-error" : undefined
                    }
                    className={
                      errors.type ? "border-destructive" : "border-border"
                    }
                  >
                    <SelectValue placeholder="Select type" />
                  </SelectTrigger>
                  <SelectContent>
                    {eventTypes.map((type) => (
                      <SelectItem key={type.id} value={type.id}>
                        {type.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {errors.type && (
                  <p
                    id="event-type-error"
                    role="alert"
                    className="text-destructive mt-1 text-sm"
                  >
                    {errors.type}
                  </p>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Format */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">Event Format</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div
                className="grid grid-cols-1 gap-3 sm:grid-cols-3"
                role="group"
                aria-label="Event format"
              >
                {eventFormats.map((format) => {
                  return (
                    <button
                      key={format.id}
                      type="button"
                      onClick={() => handleChange("format", format.id)}
                      aria-pressed={formData.format === format.id}
                      className={`flex min-h-11 flex-col items-center gap-2 rounded-none border-2 px-4 py-4 text-sm font-medium transition-all ${
                        formData.format === format.id
                          ? "border-system bg-system/10 text-system"
                          : "border-border hover:border-system/50"
                      }`}
                    >
                      {format.label}
                    </button>
                  );
                })}
              </div>

              {(formData.format === "in-person" ||
                formData.format === "hybrid") && (
                <div>
                  <label
                    htmlFor="event-location"
                    className="mb-2 block text-sm font-medium"
                  >
                    Location <span className="text-system">*</span>
                  </label>
                  <input
                    type="text"
                    id="event-location"
                    required
                    aria-invalid={Boolean(errors.location)}
                    aria-describedby={
                      errors.location ? "event-location-error" : undefined
                    }
                    value={formData.location}
                    onChange={(e) => handleChange("location", e.target.value)}
                    placeholder="e.g., New York, NY"
                    className={`bg-muted/50 focus-visible:ring-ring/50 w-full rounded-none border-2 px-4 py-3 focus:ring-2 focus:outline-none ${
                      errors.location ? "border-destructive" : "border-border"
                    }`}
                  />
                  {errors.location && (
                    <p
                      id="event-location-error"
                      role="alert"
                      className="text-destructive mt-1 text-sm"
                    >
                      {errors.location}
                    </p>
                  )}
                </div>
              )}

              {(formData.format === "virtual" ||
                formData.format === "hybrid") && (
                <div>
                  <label
                    htmlFor="event-virtual-link"
                    className="mb-2 block text-sm font-medium"
                  >
                    Virtual Meeting Link
                  </label>
                  <input
                    type="url"
                    id="event-virtual-link"
                    value={formData.virtualLink}
                    onChange={(e) =>
                      handleChange("virtualLink", e.target.value)
                    }
                    placeholder="e.g., https://zoom.us/j/..."
                    className="bg-muted/50 border-border focus-visible:ring-ring/50 w-full rounded-none border-2 px-4 py-3 focus:ring-2 focus:outline-none"
                  />
                </div>
              )}
            </CardContent>
          </Card>

          {/* Date & Time */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center gap-2 text-lg">
                <Clock className="text-system h-5 w-5" />
                Date & Time
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div>
                  <label
                    htmlFor="event-date"
                    className="mb-2 block text-sm font-medium"
                  >
                    Date <span className="text-system">*</span>
                  </label>
                  <input
                    type="date"
                    id="event-date"
                    required
                    aria-invalid={Boolean(errors.date)}
                    aria-describedby={
                      errors.date ? "event-date-error" : undefined
                    }
                    value={formData.date}
                    onChange={(e) => handleChange("date", e.target.value)}
                    className={`bg-muted/50 focus-visible:ring-ring/50 w-full rounded-none border-2 px-4 py-3 focus:ring-2 focus:outline-none ${
                      errors.date ? "border-destructive" : "border-border"
                    }`}
                  />
                  {errors.date && (
                    <p
                      id="event-date-error"
                      role="alert"
                      className="text-destructive mt-1 text-sm"
                    >
                      {errors.date}
                    </p>
                  )}
                </div>

                <div>
                  <label
                    htmlFor="event-timezone"
                    className="mb-2 block text-sm font-medium"
                  >
                    Timezone
                  </label>
                  <Select
                    value={formData.timezone}
                    onValueChange={(value) => handleChange("timezone", value)}
                  >
                    <SelectTrigger id="event-timezone">
                      <SelectValue placeholder="Select timezone" />
                    </SelectTrigger>
                    <SelectContent>
                      {timezones.map((tz) => (
                        <SelectItem key={tz.id} value={tz.id}>
                          {tz.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div>
                  <label
                    htmlFor="event-start-time"
                    className="mb-2 block text-sm font-medium"
                  >
                    Start Time <span className="text-system">*</span>
                  </label>
                  <input
                    type="time"
                    id="event-start-time"
                    required
                    aria-invalid={Boolean(errors.startTime)}
                    aria-describedby={
                      errors.startTime ? "event-start-time-error" : undefined
                    }
                    value={formData.startTime}
                    onChange={(e) => handleChange("startTime", e.target.value)}
                    className={`bg-muted/50 focus-visible:ring-ring/50 w-full rounded-none border-2 px-4 py-3 focus:ring-2 focus:outline-none ${
                      errors.startTime ? "border-destructive" : "border-border"
                    }`}
                  />
                  {errors.startTime && (
                    <p
                      id="event-start-time-error"
                      role="alert"
                      className="text-destructive mt-1 text-sm"
                    >
                      {errors.startTime}
                    </p>
                  )}
                </div>

                <div>
                  <label
                    htmlFor="event-end-time"
                    className="mb-2 block text-sm font-medium"
                  >
                    End Time
                  </label>
                  <input
                    type="time"
                    id="event-end-time"
                    value={formData.endTime}
                    onChange={(e) => handleChange("endTime", e.target.value)}
                    className="bg-muted/50 border-border focus-visible:ring-ring/50 w-full rounded-none border-2 px-4 py-3 focus:ring-2 focus:outline-none"
                  />
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Capacity */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center gap-2 text-lg">
                <Users className="text-success h-5 w-5" />
                Capacity
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label
                  htmlFor="event-capacity"
                  className="mb-2 block text-sm font-medium"
                >
                  Maximum Attendees
                </label>
                <input
                  type="number"
                  id="event-capacity"
                  aria-describedby="event-capacity-help"
                  value={formData.maxAttendees}
                  onChange={(e) => handleChange("maxAttendees", e.target.value)}
                  placeholder="Leave empty for unlimited"
                  min="1"
                  className="bg-muted/50 border-border focus-visible:ring-ring/50 w-full rounded-none border-2 px-4 py-3 focus:ring-2 focus:outline-none"
                />
                <p
                  id="event-capacity-help"
                  className="text-muted-foreground mt-1 text-sm"
                >
                  Leave empty for no limit
                </p>
              </div>
            </CardContent>
          </Card>

          {/* Tags */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">Tags</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <label htmlFor="event-tag" className="sr-only">
                Add an event tag
              </label>
              <div className="flex gap-2">
                <input
                  type="text"
                  id="event-tag"
                  value={newTag}
                  onChange={(e) => setNewTag(e.target.value)}
                  placeholder="Add a tag..."
                  onKeyDown={(e) => {
                    if (e.key === "Enter") {
                      e.preventDefault();
                      addTag();
                    }
                  }}
                  className="bg-muted/50 border-border focus-visible:ring-ring/50 flex-1 rounded-none border-2 px-4 py-3 focus:ring-2 focus:outline-none"
                />
                <Button
                  type="button"
                  variant="outline"
                  onClick={addTag}
                  className="min-h-11 min-w-11 px-4"
                  aria-label="Add tag"
                >
                  <Plus className="h-4 w-4" />
                </Button>
              </div>
              {formData.tags.length > 0 && (
                <div className="flex flex-wrap gap-2">
                  {formData.tags.map((tag) => (
                    <Badge
                      key={tag}
                      variant="secondary"
                      className="gap-1 pr-0 pl-3"
                    >
                      {tag}
                      <button
                        type="button"
                        onClick={() => removeTag(tag)}
                        aria-label={`Remove ${tag} tag`}
                        className="focus-visible:ring-ring hover:bg-destructive/20 flex min-h-11 min-w-11 items-center justify-center focus-visible:ring-2 focus-visible:outline-none"
                      >
                        <X className="h-3 w-3" aria-hidden="true" />
                      </button>
                    </Badge>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>

          {/* Submit Error */}
          {errors.submit && (
            <Card className="bg-destructive/10 border-destructive">
              <CardContent className="py-4">
                <p role="alert" className="text-destructive text-sm">
                  {errors.submit}
                </p>
              </CardContent>
            </Card>
          )}

          {/* Submit Button */}
          <div className="flex flex-col gap-3 sm:flex-row sm:gap-4">
            <Button
              type="submit"
              className="bg-system text-system-foreground hover:bg-system/90 flex-1 py-6 font-semibold"
              disabled={isSubmitting}
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Creating Event...
                </>
              ) : (
                "Create Event"
              )}
            </Button>
            <Button type="button" variant="outline" asChild className="py-6">
              <Link href="/events">Cancel</Link>
            </Button>
          </div>

          {/* Info Note */}
          <div className="bg-muted/30 border-border flex items-start gap-3 rounded-none border p-4">
            <Info className="text-system mt-0.5 h-5 w-5" />
            <div className="text-muted-foreground text-sm">
              <p>
                After creating the event, community members can register to
                attend. You&apos;ll be able to manage attendees and send
                updates.
              </p>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}
