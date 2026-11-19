using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;
using PythonStudyCompanion.Models;

namespace PythonStudyCompanion.Services;

/// <summary>
/// Saves study activity locally and derives review schedules and progress statistics.
/// </summary>
public sealed class ProgressService : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    private readonly IJSRuntime js;
    private readonly TimeProvider clock;
    private readonly SemaphoreSlim gate = new(1, 1);
    private Task<IJSObjectReference>? module;
    private Task pendingSaves = Task.CompletedTask;
    private StudyProgress data = EmptyProgress();
    private Dictionary<string, CardSchedule> schedules = new(StringComparer.Ordinal);
    private Dictionary<string, StudyAnswer> lastQuizAnswers = new(StringComparer.Ordinal);
    private string? storedJson;
    private bool initialized;

    /// <summary>Initializes a browser progress service.</summary>
    /// <param name="js">The JavaScript runtime used to access local storage.</param>
    /// <param name="clock">The clock supplying local dates and absolute review times.</param>
    public ProgressService(IJSRuntime js, TimeProvider clock)
    {
        this.js = js;
        this.clock = clock;
    }

    /// <summary>Gets the maximum accepted backup file size.</summary>
    /// <value>Ten mebibytes.</value>
    public const long MaxBackupBytes = 10 * 1024 * 1024;

    /// <summary>Occurs after progress or its storage status changes.</summary>
    public event Action? Changed;

    /// <summary>Gets whether progress was successfully loaded.</summary>
    /// <value>False when browser storage is unavailable or its document is invalid.</value>
    public bool IsAvailable { get; private set; }

    /// <summary>Gets the most recent storage error.</summary>
    /// <value>An actionable message, or null when the last operation succeeded.</value>
    public string? Error { get; private set; }

    /// <summary>Gets the current date on this device.</summary>
    /// <value>The local date used for today's activity and streaks.</value>
    public DateOnly Today => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);

    /// <summary>Gets the daily study-action goal.</summary>
    /// <value>Ten card ratings, quiz answers, or explicitly reviewed coding exercises.</value>
    public int DailyGoal => 10;

    /// <summary>Gets the saved flashcard review count.</summary>
    /// <value>One review per card per round; corrected ratings are not counted twice.</value>
    public int CardsStudied => data.Answers.Count(answer => answer.IsFlashcard);

    /// <summary>Gets the remembered and correct answer count.</summary>
    /// <value>Good and Easy card ratings plus correct quiz answers.</value>
    public int CorrectAnswers => data.Answers.Count(answer => answer.IsCorrect);

    /// <summary>Gets the missed and incorrect answer count.</summary>
    /// <value>Again card ratings plus incorrect quiz answers.</value>
    public int IncorrectAnswers => data.Answers.Count - CorrectAnswers;

    /// <summary>Gets aggregate quiz accuracy.</summary>
    /// <value>The percentage of saved quiz answers that were correct, or null before any answers.</value>
    public int? QuizAccuracy => Percentage(data.Answers.Where(answer => !answer.IsFlashcard));

    /// <summary>Gets completed quiz rounds.</summary>
    /// <value>The saved completion records, including separately scored retry rounds.</value>
    public IReadOnlyList<QuizResult> Quizzes => data.Quizzes;

    /// <summary>Gets the count of distinct coding exercises reviewed.</summary>
    /// <value>The number of exercise IDs explicitly marked as reviewed.</value>
    public int ExercisesReviewed => data.ExerciseReviews.Select(review => review.ContentId).Distinct().Count();

    /// <summary>Gets topics with any saved study activity.</summary>
    /// <value>Distinct topic names from answers and coding reviews.</value>
    public IEnumerable<string> StudiedTopics => data.Answers.Select(answer => answer.Topic)
        .Concat(data.ExerciseReviews.Select(review => review.Topic)).Distinct();

    /// <summary>Gets the current consecutive-day study streak.</summary>
    /// <value>A streak ending today or yesterday; zero when both days have no activity.</value>
    public int CurrentStreak
    {
        get
        {
            var dates = ActivityDates().ToHashSet();
            var date = dates.Contains(Today) ? Today : Today.AddDays(-1);
            var streak = 0;
            while (dates.Contains(date))
            {
                streak++;
                date = date.AddDays(-1);
            }
            return streak;
        }
    }

    /// <summary>Loads the saved progress once for this app instance.</summary>
    /// <returns>A task completed after storage is read and validated.</returns>
    public async Task InitializeAsync()
    {
        await gate.WaitAsync();
        try
        {
            if (initialized)
            {
                return;
            }
            initialized = true;
            storedJson = await (await ModuleAsync()).InvokeAsync<string?>("read");
            data = storedJson is null ? EmptyProgress() : ParseBackup(storedJson);
            RebuildIndexes();
            IsAvailable = true;
            Error = null;
        }
        catch (Exception exception) when (exception is JSException or JsonException)
        {
            IsAvailable = false;
            Error = "Saved progress couldn't be read. Existing storage has not been replaced. Reload the app to retry, or restore a valid backup on the Progress page.";
            if (module is { IsFaulted: true })
            {
                module = null;
            }
        }
        finally
        {
            gate.Release();
        }
        Changed?.Invoke();
    }

    /// <summary>Gets the number of study actions on a recorded local date.</summary>
    /// <param name="date">The local calendar date to count.</param>
    /// <returns>Saved answers plus explicitly reviewed coding exercises for that date.</returns>
    public int ActivityOn(DateOnly date) => data.Answers.Count(answer => LocalDate(answer.AnsweredAt) == date)
        + data.ExerciseReviews.Count(review => LocalDate(review.ReviewedAt) == date);

    /// <summary>Gets a card's calculated review schedule.</summary>
    /// <param name="cardId">The stable flashcard identifier.</param>
    /// <returns>The current schedule, or null for a new card.</returns>
    public CardSchedule? GetCardSchedule(string cardId) => schedules.GetValueOrDefault(cardId);

    /// <summary>Determines whether a card is new or due for review.</summary>
    /// <param name="cardId">The stable flashcard identifier.</param>
    /// <returns>True when the card is new or its scheduled time has arrived.</returns>
    public bool IsCardDue(string cardId) => !schedules.TryGetValue(cardId, out var schedule)
        || schedule.DueAt <= clock.GetUtcNow();

    /// <summary>Gets a card's study priority.</summary>
    /// <param name="cardId">The stable flashcard identifier.</param>
    /// <returns>Zero for due misses, one for other due cards, two for new cards, or three for future reviews.</returns>
    public int CardPriority(string cardId) => schedules.TryGetValue(cardId, out var schedule)
        ? schedule.DueAt > clock.GetUtcNow() ? 3 : schedule.NeedsReview ? 0 : 1
        : 2;

    /// <summary>Builds a short quiz, prioritizing missed and then new questions.</summary>
    /// <param name="questions">Questions from the selected topics.</param>
    /// <param name="size">The maximum question count.</param>
    /// <returns>A deterministic round rotating through topics within each priority.</returns>
    public List<QuizQuestion> BuildQuiz(IEnumerable<QuizQuestion> questions, int size)
    {
        var selected = new List<QuizQuestion>();
        foreach (var priority in questions.GroupBy(question => QuizPriority(question.Id)).OrderBy(group => group.Key))
        {
            var topics = priority.OrderBy(question => lastQuizAnswers.GetValueOrDefault(question.Id)?.AnsweredAt)
                .GroupBy(question => question.Topic).Select(topic => new Queue<QuizQuestion>(topic)).ToList();
            while (selected.Count < size && topics.Any(topic => topic.Count > 0))
            {
                foreach (var topic in topics)
                {
                    if (selected.Count == size)
                    {
                        break;
                    }
                    if (topic.TryDequeue(out var question))
                    {
                        selected.Add(question);
                    }
                }
            }
        }
        return selected;
    }

    /// <summary>Gets a completed quiz's correct-answer count.</summary>
    /// <param name="quiz">The completed round.</param>
    /// <returns>The number of correct answers in that saved round.</returns>
    public int QuizCorrectCount(QuizResult quiz) => data.Answers.Count(answer =>
        answer.SessionId == quiz.SessionId && !answer.IsFlashcard && answer.IsCorrect);

    /// <summary>Gets a topic's recent proficiency percentage.</summary>
    /// <param name="topic">The topic name.</param>
    /// <returns>Accuracy over its latest twenty answers, or null before any answers.</returns>
    public int? TopicAccuracy(string topic) => Percentage(RecentTopicAnswers(topic));

    /// <summary>Gets a deterministic topic proficiency stage.</summary>
    /// <param name="topic">The topic name.</param>
    /// <returns>Not started, Learning, Building, or Confident based on recent accuracy and practice breadth.</returns>
    public string TopicLevel(string topic)
    {
        var answers = RecentTopicAnswers(topic).ToList();
        if (answers.Count == 0)
        {
            return "Not started";
        }
        if (answers.Count < 5 || answers.Select(answer => (answer.IsFlashcard, answer.ContentId)).Distinct().Count() < 3)
        {
            return "Learning";
        }
        return Percentage(answers) >= 80 ? "Confident" : "Building";
    }

    /// <summary>Determines whether an exercise has been reviewed on this device's current date.</summary>
    /// <param name="exerciseId">The stable exercise identifier.</param>
    /// <returns>True when that exercise is already marked reviewed today.</returns>
    public bool IsExerciseReviewedToday(string exerciseId) => data.ExerciseReviews.Any(review =>
        review.ContentId == exerciseId && LocalDate(review.ReviewedAt) == Today);

    /// <summary>Saves or corrects a flashcard rating without duplicating the round's attempt.</summary>
    /// <param name="sessionId">The current study round identifier.</param>
    /// <param name="card">The reviewed flashcard.</param>
    /// <param name="rating">Again, Good, or Easy.</param>
    /// <returns>True when saved; otherwise false with an error message.</returns>
    public Task<bool> RecordCardAsync(Guid sessionId, Flashcard card, AnswerRating rating) =>
        SaveAsync(snapshot =>
        {
            if (rating is not (AnswerRating.Again or AnswerRating.Good or AnswerRating.Easy))
            {
                throw new JsonException("Flashcards require an Again, Good, or Easy rating.");
            }
            var index = snapshot.Answers.FindIndex(answer => answer.SessionId == sessionId && answer.ContentId == card.Id);
            var answer = new StudyAnswer
            {
                SessionId = sessionId,
                ContentId = card.Id,
                Topic = card.Topic,
                Rating = rating,
                AnsweredAt = index < 0 ? clock.GetLocalNow() : snapshot.Answers[index].AnsweredAt
            };
            if (index < 0)
            {
                snapshot.Answers.Add(answer);
            }
            else
            {
                snapshot.Answers[index] = answer;
            }
        });

    /// <summary>Saves a quiz answer and atomically records completion with the final answer.</summary>
    /// <param name="sessionId">The current quiz round identifier.</param>
    /// <param name="question">The answered question.</param>
    /// <param name="correct">Whether the chosen answer was correct.</param>
    /// <param name="questionCount">The total number of questions in the round.</param>
    /// <param name="isRetry">Whether this round retries previously missed questions.</param>
    /// <returns>True when saved; otherwise false with an error message.</returns>
    public Task<bool> RecordQuizAnswerAsync(Guid sessionId, QuizQuestion question, bool correct, int questionCount, bool isRetry) =>
        SaveAsync(snapshot =>
        {
            if (questionCount is < 1 or > 15)
            {
                throw new JsonException("A quiz round must contain one through fifteen questions.");
            }
            if (snapshot.Answers.Any(answer => answer.SessionId == sessionId && answer.ContentId == question.Id))
            {
                return;
            }
            snapshot.Answers.Add(new StudyAnswer
            {
                SessionId = sessionId,
                ContentId = question.Id,
                Topic = question.Topic,
                Rating = correct ? AnswerRating.Correct : AnswerRating.Incorrect,
                AnsweredAt = clock.GetLocalNow()
            });
            if (snapshot.Answers.Count(answer => answer.SessionId == sessionId) == questionCount)
            {
                snapshot.Quizzes.Add(new QuizResult
                {
                    SessionId = sessionId,
                    QuestionCount = questionCount,
                    CompletedAt = clock.GetLocalNow(),
                    IsRetry = isRetry
                });
            }
        });

    /// <summary>Marks an exercise reviewed, at most once per local date.</summary>
    /// <param name="exercise">The exercise explicitly reviewed by the learner.</param>
    /// <returns>True when saved; otherwise false with an error message.</returns>
    public Task<bool> RecordExerciseAsync(CodingExercise exercise) => SaveAsync(snapshot =>
    {
        if (!snapshot.ExerciseReviews.Any(review => review.ContentId == exercise.Id && LocalDate(review.ReviewedAt) == Today))
        {
            snapshot.ExerciseReviews.Add(new ExerciseReview
            {
                ContentId = exercise.Id,
                Topic = exercise.Topic,
                ReviewedAt = clock.GetLocalNow()
            });
        }
    });

    /// <summary>Parses and validates a backup without modifying saved progress.</summary>
    /// <param name="json">The JSON backup text.</param>
    /// <returns>The validated progress document.</returns>
    /// <exception cref="JsonException">The document has an unsupported version or invalid records.</exception>
    public static StudyProgress ParseBackup(string json)
    {
        var progress = JsonSerializer.Deserialize<StudyProgress>(json, JsonOptions)
            ?? throw new JsonException("The backup must contain a progress object.");
        Validate(progress);
        return progress;
    }

    /// <summary>Replaces progress with a validated backup only after the browser write succeeds.</summary>
    /// <param name="progress">The backup selected and confirmed by the learner.</param>
    /// <returns>True when restored; otherwise false with existing saved data unchanged.</returns>
    public Task<bool> ImportAsync(StudyProgress progress) => SaveAsync(_ => { }, progress);

    /// <summary>Downloads the currently saved document, preserving unreadable data for recovery.</summary>
    /// <returns>True when the browser download is requested; otherwise false with an error message.</returns>
    public async Task<bool> ExportAsync()
    {
        try
        {
            var browser = await ModuleAsync();
            var json = await browser.InvokeAsync<string?>("read") ?? JsonSerializer.Serialize(EmptyProgress(), JsonOptions);
            await browser.InvokeVoidAsync("download", json, $"python-study-progress-{Today:yyyy-MM-dd}.json");
            return true;
        }
        catch (JSException)
        {
            Error = "The backup couldn't be downloaded. Check browser storage and download permissions, then try again.";
            Changed?.Invoke();
            return false;
        }
    }

    /// <summary>Waits for progress writes already queued before an application update.</summary>
    /// <returns>A task completed after already queued progress writes finish.</returns>
    public Task WaitForPendingSavesAsync() => pendingSaves;

    /// <summary>Releases the JavaScript module used by this service.</summary>
    /// <returns>A task completed after the module reference is released.</returns>
    public async ValueTask DisposeAsync()
    {
        if (module is { IsCompletedSuccessfully: true })
        {
            await module.Result.DisposeAsync();
        }
        gate.Dispose();
    }

    private Task<IJSObjectReference> ModuleAsync() =>
        module ??= js.InvokeAsync<IJSObjectReference>("import", "./js/progress.js").AsTask();

    private Task<bool> SaveAsync(Action<StudyProgress> change, StudyProgress? replacement = null)
    {
        var save = SaveCoreAsync(change, replacement);
        pendingSaves = Task.WhenAll(pendingSaves, save);
        return save;
    }

    private async Task<bool> SaveCoreAsync(Action<StudyProgress> change, StudyProgress? replacement)
    {
        await InitializeAsync();
        await gate.WaitAsync();
        try
        {
            if (!IsAvailable && replacement is null)
            {
                return false;
            }
            var snapshot = replacement ?? new StudyProgress
            {
                SchemaVersion = 1,
                Answers = [.. data.Answers],
                Quizzes = [.. data.Quizzes],
                ExerciseReviews = [.. data.ExerciseReviews]
            };
            change(snapshot);
            Validate(snapshot);
            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            if (!await (await ModuleAsync()).InvokeAsync<bool>("write", storedJson, json))
            {
                Error = "Progress changed in another tab. Reload this app before saving so that newer progress isn't overwritten.";
                return false;
            }
            storedJson = json;
            data = snapshot;
            RebuildIndexes();
            IsAvailable = true;
            Error = null;
            return true;
        }
        catch (Exception exception) when (exception is JSException or JsonException)
        {
            Error = "Progress couldn't be saved. Existing saved data is unchanged. Check available browser storage and try again, or export a backup.";
            return false;
        }
        finally
        {
            gate.Release();
            Changed?.Invoke();
        }
    }

    private void RebuildIndexes()
    {
        schedules = new(StringComparer.Ordinal);
        lastQuizAnswers = new(StringComparer.Ordinal);
        foreach (var answer in data.Answers.OrderBy(answer => answer.AnsweredAt))
        {
            if (!answer.IsFlashcard)
            {
                lastQuizAnswers[answer.ContentId] = answer;
                continue;
            }
            var previous = schedules.GetValueOrDefault(answer.ContentId)?.IntervalDays ?? 0;
            var interval = answer.Rating switch
            {
                AnswerRating.Good => previous == 0 ? 1 : Math.Min(365, previous * 2),
                AnswerRating.Easy => previous == 0 ? 3 : Math.Min(365, previous * 3),
                _ => 0
            };
            schedules[answer.ContentId] = new CardSchedule
            {
                IntervalDays = interval,
                DueAt = interval == 0 ? answer.AnsweredAt.AddMinutes(10) : answer.AnsweredAt.AddDays(interval),
                NeedsReview = answer.Rating == AnswerRating.Again
            };
        }
    }

    private int QuizPriority(string id) => lastQuizAnswers.TryGetValue(id, out var answer) ? answer.IsCorrect ? 2 : 0 : 1;
    private IEnumerable<StudyAnswer> RecentTopicAnswers(string topic) => data.Answers.Where(answer => answer.Topic == topic)
        .OrderByDescending(answer => answer.AnsweredAt).Take(20);
    private IEnumerable<DateOnly> ActivityDates() => data.Answers.Select(answer => LocalDate(answer.AnsweredAt))
        .Concat(data.ExerciseReviews.Select(review => LocalDate(review.ReviewedAt)));
    private static DateOnly LocalDate(DateTimeOffset timestamp) => DateOnly.FromDateTime(timestamp.DateTime);
    private static int? Percentage(IEnumerable<StudyAnswer> answers)
    {
        var items = answers.ToList();
        return items.Count == 0 ? null : (int)Math.Round(100.0 * items.Count(answer => answer.IsCorrect) / items.Count);
    }

    private static StudyProgress EmptyProgress() => new() { SchemaVersion = 1, Answers = [], Quizzes = [], ExerciseReviews = [] };

    private static void Validate(StudyProgress progress)
    {
        if (progress.SchemaVersion != 1 || progress.Answers is null || progress.Quizzes is null || progress.ExerciseReviews is null)
        {
            throw new JsonException("Use a version 1 Python Study Companion backup containing answers, quizzes, and exerciseReviews.");
        }
        var answers = new HashSet<(Guid, string)>();
        foreach (var answer in progress.Answers)
        {
            if (answer is null || answer.SessionId == Guid.Empty || string.IsNullOrWhiteSpace(answer.ContentId) ||
                string.IsNullOrWhiteSpace(answer.Topic) || !Enum.IsDefined(answer.Rating) || !ValidTime(answer.AnsweredAt) ||
                !answers.Add((answer.SessionId, answer.ContentId)))
            {
                throw new JsonException("The backup contains a missing, invalid, or duplicate study answer.");
            }
        }
        var sessions = progress.Answers.GroupBy(answer => answer.SessionId).ToDictionary(group => group.Key, group => group.ToList());
        if (sessions.Values.Any(session => session.Any(answer => answer.IsFlashcard) && session.Any(answer => !answer.IsFlashcard)))
        {
            throw new JsonException("A study round cannot mix quiz answers and flashcard ratings.");
        }
        var quizzes = new HashSet<Guid>();
        foreach (var quiz in progress.Quizzes)
        {
            if (quiz is null || quiz.SessionId == Guid.Empty || !quizzes.Add(quiz.SessionId) || !ValidTime(quiz.CompletedAt) ||
                quiz.QuestionCount is < 1 or > 15 || !sessions.TryGetValue(quiz.SessionId, out var session) ||
                session.Count != quiz.QuestionCount || session.Any(answer => answer.IsFlashcard))
            {
                throw new JsonException("The backup contains an invalid quiz result or is missing its answers.");
            }
        }
        var exercises = new HashSet<(string, DateOnly)>();
        foreach (var review in progress.ExerciseReviews)
        {
            if (review is null || string.IsNullOrWhiteSpace(review.ContentId) || string.IsNullOrWhiteSpace(review.Topic) ||
                !ValidTime(review.ReviewedAt) || !exercises.Add((review.ContentId, LocalDate(review.ReviewedAt))))
            {
                throw new JsonException("The backup contains a missing, invalid, or duplicate coding review.");
            }
        }
    }

    private static bool ValidTime(DateTimeOffset timestamp) => timestamp != default
        && timestamp < DateTimeOffset.MaxValue.AddDays(-366) && timestamp > DateTimeOffset.MinValue.AddDays(1);
}
