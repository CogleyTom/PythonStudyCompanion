# Python Study Companion

A personal Python learning and technical interview preparation app built with .NET 10, C#, and Blazor WebAssembly. Designed as a mobile-first Progressive Web App (PWA) for iPhone, with local progress storage and JSON study content.

No backend, database server, authentication, or Python execution engine is required.

## Project status

Milestones 1-4 are implemented. Milestone 5 remains: final mobile polish, real-iPhone installation and offline testing, and deployment configuration.

## Features

- **Home:** Due/new flashcards, total card reviews, quiz accuracy, daily activity, and study streak.
- **Flashcards:** Topic selection, 5/10/15-card session limits, tap-to-flip cards, navigation, shuffle within review priorities, and Again/Good/Easy ratings.
- **Quizzes:** Topic selection, 5/10/15-question rounds, immediate feedback and explanations, scores, missed-question review, and saved quiz history. Smaller topic selections use the available questions without adding duplicates.
- **Coding practice:** Topic and difficulty filters, problem statements, examples, progressive hints, reference solutions, and explanations. Code is displayed, never executed.
- **Progress:** Local persistence, spaced repetition, topic proficiency, seven-day activity, coding reviews, and JSON backup export/import.
- **Interface:** Four navigation tabs, responsive layouts, system-aware light/dark mode, and reduced-motion support.

Starter content includes **32 flashcards, 40 quiz questions, and 8 coding exercises** across:

1. Python Fundamentals
2. Python Functions
3. Lists, Tuples, Sets, and Dictionaries
4. Object-Oriented Programming
5. Data Structures
6. Algorithms and Coding Patterns
7. Big O Complexity
8. Technical Interview Questions

## Run locally

### Visual Studio 2026

1. Install the **ASP.NET and web development** workload and **.NET 10 SDK**.
2. Open `PythonStudyCompanion.slnx`.
3. Select the **https** launch profile and press **F5**.
4. Trust the ASP.NET Core development certificate if prompted.

### Command line

Run from the project folder:

```powershell
dotnet run --project .\PythonStudyCompanion.csproj --launch-profile https
```

The configured HTTPS development address is `https://localhost:7253`.

To build or check formatting:

```powershell
dotnet build .\PythonStudyCompanion.slnx
dotnet format .\PythonStudyCompanion.slnx --verify-no-changes --no-restore
```

## Project structure

```text
Layout\                         Shared application shell and bottom navigation
Models\                         Study content and progress models
Pages\                          Dashboard, flashcards, practice, and progress screens
Services\                       JSON loading, progress storage, and scheduling
Properties\launchSettings.json   Local development launch profiles
wwwroot\
  css\app.css                   Application styling
  data\flashcards.json          Flashcard catalog
  data\quizzes.json             Multiple-choice question catalog
  data\coding-exercises.json    Coding exercise catalog
  js\progress.js               Local storage and backup-download interop
  manifest.webmanifest          PWA installation metadata
  service-worker.js            Development worker without offline caching
  service-worker.published.js  Published offline-caching worker
App.razor                       Routing
Program.cs                      Application startup and service registration
PythonStudyCompanion.csproj      .NET project
PythonStudyCompanion.slnx        Visual Studio solution
```

## Add study content

Edit the appropriate JSON array under `wwwroot\data`. Models in `Models` define the required fields.

- Keep IDs **unique within each catalog and stable over time**; saved progress uses those IDs.
- Use consistent topic names across the catalogs.
- Content is plain text, not HTML or Markdown. Use `\n` inside JSON strings for line breaks.
- Quiz questions require exactly four distinct, nonempty options, a zero-based `correctAnswerIndex` from `0` to `3`, and an explanation.
- Coding exercises require a difficulty of `Easy`, `Medium`, or `Hard`, example input/output, at least one hint, a solution, and an explanation.
- Republish after content changes so the published offline cache includes the updated files.

For example, a flashcard entry looks like:

```json
{
  "id": "fundamentals-example",
  "topic": "Python Fundamentals",
  "front": "What does len('Python') return?",
  "back": "6\n\nlen returns the number of characters in this string."
}
```

## Progress and review rules

Progress is stored in this browser on this device. There are no accounts or cloud synchronization.

### Flashcard scheduling

| Rating | Next review |
|---|---|
| Again | In 10 minutes; resets the successful interval |
| Good | Starts at 1 day, then doubles the previous successful interval |
| Easy | Starts at 3 days, then triples the previous successful interval |

Intervals are capped at 365 days. New cards count as due. Sessions prioritize due misses, other due cards, then new cards. Disable **Due and new cards only** to practice ahead.

Changing a card's rating within the same round corrects the original attempt instead of adding another. Starting a new review round counts as another attempt.

### Quizzes, activity, and proficiency

- Quiz answers save before feedback is shown, including answers in unfinished rounds. Completed rounds and missed-question retries have separate history entries.
- Quiz accuracy includes all saved quiz answers, including unfinished rounds and retries.
- The daily goal is **10 study actions**: card ratings, quiz answers, or coding exercises explicitly marked reviewed.
- Coding reviews count at most once per exercise per recorded local date. Revealing a solution alone does not count, and coding reviews are not scored as correct answers.
- Streaks use recorded local study dates and remain current through yesterday.
- Topic proficiency uses the latest 20 answers. **Confident** requires at least 5 answers across 3 distinct items with at least 80% accuracy.
- Active study screens are not resumed after navigation or reload; already saved answers remain recorded.

### Backups

Use the **Progress** tab to export or import a JSON backup.

- Export downloads the currently saved progress document.
- Import accepts backups up to 10 MiB and validates the format before showing a preview.
- **Replace local progress** replaces the current data; it does not merge records.
- Invalid backups and failed writes leave existing saved data unchanged.
- Clearing browser website data removes local progress. Export a backup before moving devices or changing the site's address.

If storage is unavailable or another tab has saved newer progress, the app shows an error rather than silently advancing. Reload to read newer data; backup controls remain available for recovery.

## Publish and offline use

```powershell
dotnet publish .\PythonStudyCompanion.csproj --configuration Release
```

The static site is generated in:

```text
bin\Release\net10.0\publish\wwwroot\
```

Host the contents of that folder over HTTPS. Configure navigation routes such as `/practice/quiz` to fall back to `index.html`, while serving existing static assets normally.

The current app assumes deployment at the site's root. Subfolder hosting requires matching base-path changes in `wwwroot\index.html` and `wwwroot\service-worker.published.js`.

**Offline caching is enabled in published builds, not development runs.** Load the published app online and allow its service worker to finish installing and caching before testing offline.

### iPhone installation target

After HTTPS deployment, open the site in Safari and use **Share > Add to Home Screen**. Final verification on a physical iPhone is part of Milestone 5, including:

- Installation, app icons, layout, and touch controls.
- Offline launch and navigation after closing and reopening.
- Saved progress and backup import/export.
- App updates without losing progress.

Deployment configuration and update instructions will be finalized in Milestone 5.
