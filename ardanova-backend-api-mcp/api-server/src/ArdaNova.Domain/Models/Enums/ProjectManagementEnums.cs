using System.Text.Json.Serialization;

namespace ArdaNova.Domain.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RoadmapStatus
{
    DRAFT,
    ACTIVE,
    COMPLETED,
    ARCHIVED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PhaseStatus
{
    PLANNED,
    IN_PROGRESS,
    COMPLETED,
    CANCELLED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EpicStatus
{
    PLANNED,
    IN_PROGRESS,
    COMPLETED,
    CANCELLED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SprintStatus
{
    PLANNED,
    ACTIVE,
    COMPLETED,
    CANCELLED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PBIType
{
    FEATURE,
    ENHANCEMENT,
    BUG,
    TECHNICAL_DEBT,
    SPIKE
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PBIStatus
{
    NEW,
    READY,
    IN_PROGRESS,
    DONE,
    CANCELLED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BacklogItemType
{
    TASK,
    BUG,
    IMPROVEMENT,
    RESEARCH
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BacklogStatus
{
    NEW,
    READY,
    IN_PROGRESS,
    DONE,
    BLOCKED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Priority
{
    LOW,
    MEDIUM,
    HIGH,
    CRITICAL
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProjectRole
{
    FOUNDER,
    LEADER,
    CORE_CONTRIBUTOR,
    CONTRIBUTOR,
    OBSERVER
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CompensationModel
{
    FIXED_TOKEN,
    HOURLY_TOKEN,
    EQUITY_PERCENT,
    HYBRID,
    BOUNTY,
    MILESTONE
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubmissionStatus
{
    PENDING,
    APPROVED,
    REJECTED,
    REVISION_REQUESTED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EscrowStatus
{
    NONE,
    PENDING,
    FUNDED,
    RELEASED,
    DISPUTED,
    REFUNDED
}
