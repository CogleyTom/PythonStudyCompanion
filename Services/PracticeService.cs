using System.Net.Http.Json;
using System.Text.Json;
using PythonStudyCompanion.Models;

namespace PythonStudyCompanion.Services;

/// <summary>
/// Loads and validates the locally hosted quiz and coding content.
/// </summary>
public sealed class PracticeService
{
    private readonly HttpClient http;

    /// <summary>
    /// Initializes a practice content service.
    /// </summary>
    /// <param name="http">The HTTP client configured with the application's base address.</param>
    public PracticeService(HttpClient http)
    {
        this.http = http;
    }

    /// <summary>
    /// Loads quiz questions in content-file order.
    /// </summary>
    /// <returns>The validated question catalog, which may be empty.</returns>
    /// <exception cref="HttpRequestException">The quiz file cannot be retrieved.</exception>
    /// <exception cref="OperationCanceledException">The request times out or is canceled.</exception>
    /// <exception cref="JsonException">The quiz content is malformed or invalid.</exception>
    public async Task<IReadOnlyList<QuizQuestion>> LoadQuizQuestionsAsync()
    {
        var questions = await http.GetFromJsonAsync<List<QuizQuestion>>("data/quizzes.json")
            ?? throw new JsonException("The quiz file must contain a JSON array.");
        var identifiers = new HashSet<string>(StringComparer.Ordinal);

        foreach (var question in questions)
        {
            if (question is null ||
                string.IsNullOrWhiteSpace(question.Id) ||
                !identifiers.Add(question.Id) ||
                string.IsNullOrWhiteSpace(question.Topic) ||
                string.IsNullOrWhiteSpace(question.Question) ||
                string.IsNullOrWhiteSpace(question.Explanation) ||
                question.Options is not { Length: 4 } ||
                question.Options.Any(string.IsNullOrWhiteSpace) ||
                question.Options.Select(option => option.Trim()).Distinct(StringComparer.Ordinal).Count() != 4 ||
                question.CorrectAnswerIndex < 0 ||
                question.CorrectAnswerIndex >= question.Options.Length)
            {
                throw new JsonException("Each question needs a unique ID, topic, question, explanation, four distinct answers, and a valid correct-answer index.");
            }
        }

        return questions;
    }

    /// <summary>
    /// Loads coding exercises in content-file order.
    /// </summary>
    /// <returns>The validated exercise catalog, which may be empty.</returns>
    /// <exception cref="HttpRequestException">The coding file cannot be retrieved.</exception>
    /// <exception cref="OperationCanceledException">The request times out or is canceled.</exception>
    /// <exception cref="JsonException">The coding content is malformed or invalid.</exception>
    public async Task<IReadOnlyList<CodingExercise>> LoadCodingExercisesAsync()
    {
        var exercises = await http.GetFromJsonAsync<List<CodingExercise>>("data/coding-exercises.json")
            ?? throw new JsonException("The coding file must contain a JSON array.");
        var identifiers = new HashSet<string>(StringComparer.Ordinal);

        foreach (var exercise in exercises)
        {
            if (exercise is null ||
                string.IsNullOrWhiteSpace(exercise.Id) ||
                !identifiers.Add(exercise.Id) ||
                string.IsNullOrWhiteSpace(exercise.Topic) ||
                string.IsNullOrWhiteSpace(exercise.Title) ||
                exercise.Difficulty is not ("Easy" or "Medium" or "Hard") ||
                string.IsNullOrWhiteSpace(exercise.Description) ||
                string.IsNullOrWhiteSpace(exercise.ExampleInput) ||
                string.IsNullOrWhiteSpace(exercise.ExpectedOutput) ||
                exercise.Hints is not { Length: > 0 } ||
                exercise.Hints.Any(string.IsNullOrWhiteSpace) ||
                string.IsNullOrWhiteSpace(exercise.Solution) ||
                string.IsNullOrWhiteSpace(exercise.Explanation))
            {
                throw new JsonException("Each exercise needs a unique ID, topic, title, valid difficulty, problem, example, output, hints, solution, and explanation.");
            }
        }

        return exercises;
    }
}
