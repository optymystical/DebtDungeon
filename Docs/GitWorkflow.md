# Git workflow for Debt Collectors

This is the day-to-day routine. The README covers first-time install; this covers everything after.

## Branches

| Branch | Who | What it's for |
| --- | --- | --- |
| `main` | Nobody pushes here | Milestone builds only. Chris merges `develop` into it at each milestone. |
| `develop` | Everyone, through PRs | The integration branch. Always has to open and compile. |
| `feature/greg-floor-generation` | Gregory | Module 1 rewrite |
| `feature/larry-faction-ai` | Larry | Module 2 rewrite |
| `feature/matthew-player-combat` | Matthew | Module 3 rewrite |
| `feature/chris-progression` | Christopher | Module 4 rewrite |
| `feature/chris-core` | Christopher | Core contract changes (v1.2 and later) |
| `prototype/greg-floor-generation` | Nobody | Greg's Module Week prototype. **Reference only. Never merge it.** |

Need a second branch for a side task (a Shared Pool card, say)? Branch from `develop` as `feature/<name>-<topic>`.

## First time on your branch

```
git fetch origin
git checkout feature/<your-branch>
```
Open the project in Unity **6000.3.27f2** (exactly that version).

## Every work session

1. **Bring in everyone else's work first:**
   ```
   git checkout feature/<your-branch>
   git pull
   git merge origin/develop
   ```
   (In GitHub Desktop: Branch → *Update from develop*.)
2. Work in **your module folder** and **your sandbox scene** (`Assets/_Project/Scenes/Sandbox/<You>_<Topic>.unity`).
3. Commit small and often, with short imperative messages: `Add raid timer to RaidManager`.
4. `git push` at the end of the session, even if it's unfinished. Your branch is your backup.

## Getting work into develop

1. Merge the latest `develop` into your branch (step 1 above) and make sure it still compiles.
2. Push, then on GitHub open a **Pull Request**: base `develop` ← compare `feature/<your-branch>`.
3. Fill in the PR template. If AI helped write the code, tick that box and include the Defense Notes.
4. Ask one teammate in Discord to review. Merge after they approve.
5. Keep your branch afterwards and keep working on it. Merge `develop` into it again next session.

## Rules that save us from merge hell

- **Scenes:** your sandbox scene is yours alone. `Main` and `MainMenu` are shared: post in Discord before you edit one, and again when you've pushed. Build things as **prefabs** so the shared scenes barely change.
- **Core** (`Assets/_Project/Core/`) belongs to Chris. Need a change? Ask in Discord. He bumps the version and logs it in `Core/CHANGELOG.md`.
- **Never edit another person's module folder.** Talk to them instead.
- **`.meta` files always travel with their asset.** Move and rename assets **inside Unity**, never in File Explorer, or references break.
- **Never commit** `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `.sln` or `.csproj` files (`.gitignore` already blocks them).
- **Project Settings** (layers, tags, input, physics) are shared. Ask Chris before you change them.
- Art and audio go through **Git LFS** automatically. Run `git lfs install` once per machine.

## When a scene or prefab conflicts

1. Don't panic, and don't push a half-merged scene.
2. If you set up UnityYAMLMerge (README), run `git mergetool`.
3. If not, keep the other person's version (`git checkout --theirs <file>`), then redo your scene change. That's why we keep scene edits small.
4. Still stuck? Post the error in Discord before you try anything else.

## Saturday bug sweep

Before Saturday's 6 PM meeting, get your week's work merged into `develop` so the sweep covers it. Bring Console errors or screenshots of anything weird.
