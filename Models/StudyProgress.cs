using System.Text.Json.Serialization;

namespace PythonStudyCompanion.Models;

/// <summary>
/// Defines the recall rating or quiz outcome saved for a study answer.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AnswerRating>))]
public enum AnswerRating
{
    /// <summary>Indicates a missed flashcard.</summary>
    Again,
    /// <summary>Indicates a remembered flashcard.</summary>
    Good,
    /// <summary>Indicates an easily remembered flashcard.</summary>
    Easy,
    /// <summary>Indicates a correct quiz answer.</summary>
    Correct,
    /// <summary>Indicates an incorrect quiz answer.</summary>
    Incorrect
}

/// <summary>
/// Contains the versioned progress document saved in browser local storage.
/// </summary>
public sealed class StudyProgress
{
    /// <summary>Gets the document format version.</summary>
    /// <value>One for the current format.</value>
    public required int SchemaVersion { get; init; }

    /// <summary>Gets the individual flashcard and quiz answers.</summary>
    /// <value>One answer per content item in a study round.</value>
    public required List<StudyAnswer> Answers { get; init; }

    /// <summary>Gets the completed quiz rounds.</summary>
    /// <value>Quiz completion records associated with saved answers.</value>
    public required List<QuizResult> Quizzes { get; init; }

    /// <summary>Gets the coding exercises explicitly marked as reviewed.</summary>
    /// <value>At most one review per exercise per recorded local date.</value>
    public required List<ExerciseReview> ExerciseReviews { get; init; }
}

/// <summary>
/// Represents one flashcard rating or quiz answer.
/// </summary>
public sealed class StudyAnswer
{
    /// <summary>Gets the study round's identifier.</summary>
    /// <value>A nonempty identifier shared by answers in the same round.</value>
    public required Guid SessionId { get; init; }

    /// <summary>Gets the content item's stable identifier.</summary>
    /// <value>The flashcard or quiz question ID from the content catalog.</value>
    public required string ContentId { get; init; }

    /// <summary>Gets the topic at the time of study.</summary>
    /// <value>A nonempty topic name.</value>
    public required string Topic { get; init; }

    /// <summary>Gets the answer's recall rating or correctness.</summary>
    /// <value>A flashcard rating or a quiz outcome.</value>
    public required AnswerRating Rating { get; init; }

    /// <summary>Gets when the item was first answered in this round.</summary>
    /// <value>A timestamp retaining the device's local offset for daily activity.</value>
    public required DateTimeOffset AnsweredAt { get; init; }

    /// <summary>Gets whether this answer is a flashcard rating.</summary>
    /// <value>True for Again, Good, or Easy; otherwise false.</value>
    [JsonIgnore]
    public bool IsFlashcard => Rating is AnswerRating.Again or AnswerRating.Good or AnswerRating.Easy;

    /// <summary>Gets whether the answer was remembered or correct.</summary>
    /// <value>True for Good, Easy, or Correct; otherwise false.</value>
    [JsonIgnore]
    public bool IsCorrect => Rating is AnswerRating.Good or AnswerRating.Easy or AnswerRating.Correct;
}

/// <summary>
/// Identifies a completed quiz round without duplicating its answer totals.
/// </summary>
public sealed class QuizResult
{
    /// <summary>Gets the round's identifier.</summary>
    /// <value>The session identifier of its saved quiz answers.</value>
    public required Guid SessionId { get; init; }

    /// <summary>Gets the number of questions in the round.</summary>
    /// <value>A count from one through fifteen.</value>
    public required int QuestionCount { get; init; }

    /// <summary>Gets the completion time.</summary>
    /// <value>A timestamp retaining the device's local offset.</value>
    public required DateTimeOffset CompletedAt { get; init; }

    /// <summary>Gets whether the round was a retry of missed questions.</summary>
    /// <value>True for a missed-question review round.</value>
    public required bool IsRetry { get; init; }
}

/// <summary>
/// Represents a coding exercise explicitly reviewed by the learner.
/// </summary>
public sealed class ExerciseReview
{
    /// <summary>Gets the exercise's stable identifier.</summary>
    /// <value>The ID from the coding exercise catalog.</value>
    public required string ContentId { get; init; }

    /// <summary>Gets the exercise's topic.</summary>
    /// <value>A nonempty topic name.</value>
    public required string Topic { get; init; }

    /// <summary>Gets when the exercise was marked as reviewed.</summary>
    /// <value>A timestamp retaining the device's local offset.</value>
    public required DateTimeOffset ReviewedAt { get; init; }
}

/// <summary>
/// Describes the next review calculated from a card's saved ratings.
/// </summary>
public sealed class CardSchedule
{
    /// <summary>Gets the current successful review interval.</summary>
    /// <value>Zero after Again, or an interval from one through 365 days.</value>
    public required int IntervalDays { get; init; }

    /// <summary>Gets when the next review is due.</summary>
    /// <value>An absolute timestamp used for due-card comparisons.</value>
    public required DateTimeOffset DueAt { get; init; }

    /// <summary>Gets whether the last rating was Again.</summary>
    /// <value>True when the card most recently needed another attempt.</value>
    public required bool NeedsReview { get; init; }
}
