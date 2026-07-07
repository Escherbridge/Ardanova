namespace ArdaNova.Application.Services.Implementations;

using System.Collections.Generic;
using ArdaNova.Application.Azoa.Quests;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>
/// Authors and publishes ArdaNova's canonical scrum-lifecycle quest definitions to
/// the AZOA node (track <c>azoa-quest-authoring</c>; contract §5).
///
/// The definitions themselves are owned by <see cref="ScrumLifecycleQuests"/> — a
/// sibling lane already produced the three DAGs (Project Lifecycle, Task Bounty,
/// Membership Credential) with all economics baked in. This service simply takes
/// those definitions and pushes them to the node via the
/// <see cref="IAzoaQuestNode"/> port, choosing the template endpoint for public
/// definitions and the plain-quest endpoint for restricted ones, then validates
/// each published DAG.
///
/// Layering: depends only on the Application-owned port — never on the
/// Infrastructure transport (dependency-inversion seam).
/// </summary>
public class AzoaQuestAuthoringService : IAzoaQuestAuthoringService
{
    private readonly IAzoaQuestNode _node;
    private readonly ILogger<AzoaQuestAuthoringService> _logger;

    public AzoaQuestAuthoringService(
        IAzoaQuestNode node,
        ILogger<AzoaQuestAuthoringService> logger)
    {
        _node = node;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<AzoaQuestPublishResultDto>> PublishLifecycleDefinitionsAsync(
        CancellationToken ct = default)
    {
        // The three canonical definitions, in publish order. Economics are already
        // decided in ArdaNova and baked into these DAGs (contract §1/§3); the node
        // computes nothing.
        var definitions = new[]
        {
            ScrumLifecycleQuests.CreateProjectLifecycleDefinition(),
            ScrumLifecycleQuests.CreateTaskBountyDefinition(),
            ScrumLifecycleQuests.CreateMembershipCredentialDefinition(),
        };

        var published = new List<AzoaPublishedQuestDto>(definitions.Length);

        foreach (var definition in definitions)
        {
            // Public definitions become templates any avatar may instantiate;
            // restricted ones are plain quests.
            var createResult = definition.IsPublic
                ? await _node.CreateTemplateAsync(definition, ct)
                : await _node.CreateQuestAsync(definition, ct);

            if (createResult.IsFailure)
            {
                _logger.LogError(
                    "Failed to publish AZOA quest definition '{QuestName}': {Error}",
                    definition.Name, createResult.Error);
                return MapFailure<AzoaQuestRef, AzoaQuestPublishResultDto>(createResult);
            }

            var questId = createResult.Value!.QuestId;
            if (string.IsNullOrWhiteSpace(questId))
            {
                return Result<AzoaQuestPublishResultDto>.Failure(
                    $"AZOA node returned no quest id when publishing '{definition.Name}'.");
            }

            // Validate the published DAG. A validation transport failure is fatal;
            // a DAG that validates as invalid is reported on the per-quest DTO.
            var validateResult = await _node.ValidateQuestAsync(questId, ct);
            if (validateResult.IsFailure)
            {
                _logger.LogError(
                    "Failed to validate AZOA quest '{QuestName}' ({QuestId}): {Error}",
                    definition.Name, questId, validateResult.Error);
                return MapFailure<AzoaQuestValidation, AzoaQuestPublishResultDto>(validateResult);
            }

            var validation = validateResult.Value!;
            if (!validation.IsValid)
            {
                _logger.LogWarning(
                    "AZOA quest '{QuestName}' ({QuestId}) published but failed validation: {Message}",
                    definition.Name, questId, validation.Message);
            }

            published.Add(new AzoaPublishedQuestDto
            {
                Name = definition.Name,
                QuestId = questId,
                PublishedAsTemplate = definition.IsPublic,
                Validated = validation.IsValid,
            });

            _logger.LogInformation(
                "Published AZOA quest '{QuestName}' as {Kind} with id {QuestId} (validated: {Validated}).",
                definition.Name,
                definition.IsPublic ? "template" : "quest",
                questId,
                validation.IsValid);
        }

        return Result<AzoaQuestPublishResultDto>.Success(
            new AzoaQuestPublishResultDto { Published = published });
    }

    /// <summary>
    /// Re-projects a failed <c>Result&lt;TIn&gt;</c> onto <c>Result&lt;TOut&gt;</c>
    /// preserving the original <see cref="ResultType"/>, so node failure semantics
    /// (Forbidden, Conflict, …) survive to the caller.
    /// </summary>
    private static Result<TOut> MapFailure<TIn, TOut>(Result<TIn> source)
    {
        var error = source.Error ?? "AZOA quest node call failed.";
        return source.Type switch
        {
            ResultType.NotFound => Result<TOut>.NotFound(error),
            ResultType.Forbidden => Result<TOut>.Forbidden(error),
            ResultType.Unauthorized => Result<TOut>.Unauthorized(error),
            ResultType.Conflict => Result<TOut>.Conflict(error),
            ResultType.ValidationError => Result<TOut>.ValidationError(error),
            ResultType.BadRequest => Result<TOut>.BadRequest(error),
            _ => Result<TOut>.Failure(error),
        };
    }
}
