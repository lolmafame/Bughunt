# Repository Guidelines

## Project Structure & Module Organization

This is a Unity project targeting Unity `6000.3.0b4`. Open the repository root—the directory containing `Assets/`, `Packages/`, and `ProjectSettings/`—in Unity Hub. Gameplay and UI C# scripts live throughout `Assets/`, especially in `Assets/codes/`, `Assets/scripts/`, and the root of `Assets/`. Production scenes are primarily under `Assets/GABRIEL FOLDER/Prefabs/FINAL LEVEL SCENES/`; scene order is defined in `ProjectSettings/EditorBuildSettings.asset`. Third-party integrations include Firebase, the i5 Toolkit, URP, Cinemachine, and the Input System.

Treat `Library/`, `Logs/`, `Temp/`, generated solution/project files, and local `UserSettings/` as disposable outputs. Do not edit them or commit them. Keep every Unity asset together with its `.meta` file so GUID references remain stable.

## Build, Test, and Development Commands

- Open locally: add this folder in Unity Hub and select editor `6000.3.0b4`, then use Play Mode for development.
- Build: use **File > Build Profiles**, verify the enabled scenes, select the target platform, and choose **Build**.
- Run Edit Mode tests from PowerShell:
  `Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults.xml -quit`
- Run Play Mode tests by replacing `EditMode` with `PlayMode`.

Unity regenerates `Bughunt.sln` and `Assembly-CSharp*.csproj`; never hand-edit these files.

## Coding Style & Naming Conventions

Use four spaces for C# indentation and place braces on new lines. Use PascalCase for classes, enums, public methods, and properties; use camelCase for parameters, locals, and serialized fields. Keep each `MonoBehaviour` in a same-named file (for example, `TerminalManager` in `TerminalManager.cs`). Prefer `[SerializeField] private` for Inspector state, cache component references, and guard optional scene references against `null`. Avoid broad reformatting of scenes, prefabs, or unrelated scripts.

## Testing Guidelines

Unity Test Framework `1.6.0` is installed, but no project test assemblies currently exist. Add tests under `Assets/Tests/EditMode/` or `Assets/Tests/PlayMode/`, with an `.asmdef` that references the test framework. Name files after the class under test, such as `TerminalManagerTests.cs`. Before submitting, run relevant automated tests and manually exercise affected scenes in Play Mode, including scene transitions and Firebase-dependent flows when applicable.

## Commit & Pull Request Guidelines

Recent history uses short, descriptive summaries but no enforced format. Use concise imperative subjects such as `Fix tutorial terminal progression`; keep each commit focused and include associated `.meta` files. Pull requests should explain the player-visible change, list scenes and prefabs touched, document test steps, and link related issues. Include screenshots or a short recording for UI, animation, lighting, or level-layout changes. Call out package, build-profile, or Firebase configuration changes explicitly.
