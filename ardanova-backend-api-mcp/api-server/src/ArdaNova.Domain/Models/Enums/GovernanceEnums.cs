using System.Text.Json.Serialization;

namespace ArdaNova.Domain.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProposalType
{
    TREASURY,
    GOVERNANCE,
    STRATEGIC,
    OPERATIONAL,
    EMERGENCY,
    CONSTITUTIONAL,
    TOKEN
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProposalStatus
{
    DRAFT,
    ACTIVE,
    PASSED,
    REJECTED,
    EXECUTED,
    CANCELLED,
    EXPIRED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionType
{
    DEPOSIT,
    WITHDRAWAL,
    TASK_PAYMENT,
    PROPOSAL_EXECUTION,
    DIVIDEND,
    FEE
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VestingFrequency
{
    DAILY,
    WEEKLY,
    MONTHLY,
    QUARTERLY
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ICOStatus
{
    PENDING,
    ACTIVE,
    SUCCESSFUL,
    FAILED,
    CANCELLED,
    REFUNDING
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContributionStatus
{
    PENDING,
    CONFIRMED,
    REFUNDED,
    FAILED
}
