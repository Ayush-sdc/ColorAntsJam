# Slither Ants Jam - Active Runtime Flow & Requirements Specification

## 1. Project Entry Point & Architecture

- **Project Root Path**: `c:\Users\ayush\Desktop\Unity Projects\Slither Ants Jam`
- **Active Scenes**:
  - `Assets/Scenes/Welcome.unity`: Dedicated standalone startup scene (Build Index 0). Always loads first on fresh application launch.
  - `Assets/Scenes/main.unity`: Main gameplay scene (Build Index 1). Houses `GameController`, `Board`, `BoardManager`, `LevelManager`, `Main Camera`, and `GameUICanvas`.
- **Editor Play Mode Startup**:
  - Controlled by [`PlayModeStartupSceneManager`](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Editor/PlayModeStartupSceneManager.cs).
  - Normal Flow: Automatically configures `EditorSceneManager.playModeStartScene = Assets/Scenes/Welcome.unity`. Pressing Play in the Editor always starts at the Welcome screen regardless of which scene is currently open.
  - Testing Override: Clearly labeled menu option `Tools > Slither Ants Jam > Play Mode Start Scene > Play Current Open Scene (Testing Override)` allows developers to directly run the currently open scene without loading Welcome.
- **Controller Singletons & Scene Cleanliness**:
  - `SoundManager`: Standalone duplicates removed from `main.unity`; single instance maintained on `GameController`.
  - `BoardCameraController`: Configured on `GameController`, controls `Main Camera` orthographic size directly.
  - `GameUIManager`: Manages overlays on `GameUICanvas` (`LevelCompletedOverlay`, `FeatureTutorialModal`, `HardLevelIntroOverlay`, `SettingsModal`, `WinPanel`, `LosePanel`).

---

## 2. Welcome Flow & Legal Terms Acceptance

- **Fresh Launch Behavior**:
  - Every fresh launch displays the Welcome screen and **waits for the player to click ACCEPT**, including returning players who already accepted previously.
  - Automatic skip of the Welcome screen on application launch is completely removed.
  - Tapping Terms of Service or Privacy Policy opens [`LegalDocumentModal`](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/UI/LegalDocumentModal.cs) without advancing to gameplay.
- **ACCEPT Button Action**:
  - Records terms acceptance via [`LegalManager.RecordTermsAcceptance`](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Legal/LegalManager.cs).
  - Loads `main.unity` exactly once at the player's saved level (`PlayerPrefs.GetInt("CurrentLevel", 1)`).
  - Saved progress and settings are strictly preserved.
- **In-Game Flow**:
  - Welcome is never reloaded between ordinary levels or upon application focus/pause changes.

---

## 3. Early-Level Completion Screen & Feature Progression

- **Progress Determination**:
  - Feature progress preview on [`LevelCompletedOverlay`](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/UI/LevelCompletedOverlay.cs) is strictly selected using the **just completed level**, NOT `Mathf.Max(justCompletedLevel, savedHighestCompleted)`.
  - Replaying an early level with high saved progress (e.g. replaying Level 6 with saved Level 45) correctly shows the milestone progress bar.
- **Progression Intervals & Formulas**:
  - **Levels 1–6 (Linked Ants)**: `completedLevel / 6f` (Level 6 = 100%)
  - **Levels 7–14 (Stacked Ants)**: `(completedLevel - 6) / 8f` (Level 14 = 100%)
  - **Levels 15–20 (Pipes)**: `(completedLevel - 14) / 6f` (Level 20 = 100%)
  - **Levels 21–29 (Locked Ants)**: `(completedLevel - 20) / 9f` (Level 29 = 100%)
  - **Level 30 onward**: Feature progress section is completely hidden; title and buttons are cleanly repositioned without empty gaps.
- **Visual Presentation**:
  - The progress bar fill and percentage text always initialize to 0% and animate smoothly upward to the target value via DOTween.
  - Reaching 100% triggers celebration punch animations on the feature icon and percentage text.

---

## 4. Repeating Feature Milestone Tutorials

- **Designated Levels & Features**:
  - **Level 7**: Linked Ants
  - **Level 15**: Stacked Ants
  - **Level 21**: Pipes
  - **Level 30**: Locked Ants
- **Trigger Rules**:
  - Overlays appear **EVERY time** their designated level is entered: normal progression, level restart, replay, level selection, and saved-level loading.
  - Persistent acknowledgement flags (`TutorialSeen_*`) do **not** suppress these milestone overlays.
  - Overlays are guarded per level-load event (`currentLevelLoadId`) so closing Settings or pausing the app does not cause duplicate overlays.
- **Input & Game State**:
  - Gameplay timer and input remain paused until the player taps OK.
  - Tapping OK smoothly dismisses the tutorial and resumes gameplay.

---

## 5. Camera Optics & Modest Zoom-Out Limit

- **Framing**:
  - Default view orthographic size: `10.0f`.
  - Closer default framing keeps ants large, crisp, and readable.
- **Zoom Constraints**:
  - `minZoom = 5.5f` (closest view).
  - `maxZoomMultiplier = 1.2f`.
  - `maxZoom = 12.0f` (approximately 1.2x default zoom).
  - Enforced in [`BoardCameraController`](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Camera/BoardCameraController.cs) on `Awake()`, `OnEnable()`, `AdjustZoom()`, and `SetZoom()`.
  - Both desktop mouse wheel and mobile pinch-to-zoom cannot zoom out past 12.0.
- **Panning Freedom**:
  - Dynamic pan limits calculated from level footprint and HUD boundaries allow horizontal and vertical panning so all board objects remain reachable below the top HUD and above the bottom margin.

---

## 6. Test Suite & Evidence Artifacts

- **Verification Suites Executed**:
  1. `RealFlowInteractiveDemonstration`: Demonstrations A–F verified with 0 failures.
  2. `WelcomeAndProgressionVerificationSuite`: 8 tests verified with 0 failures.
  3. `FixesVerificationTestSuite`: 12 tests verified with 0 failures.
  4. `TutorialAndLegalPlayModeTests`: 11 tests verified with 0 failures.
  5. `PlayModeConcurrencyAndScreensTests`: 11 tests verified with 0 failures.
  6. `FeatureUIPlayModeTests`: 4 tests verified with 0 failures.
- **Evidence Files in `Assets/TestEvidence/Screenshots/`**:
  - `A_Welcome_Visible_ReturningPlayer.png`: Welcome visible on launch with existing profile.
  - `B_Gameplay_Loaded_SavedLevel.png`: Gameplay loaded at saved Level 7 after Accept.
  - `C_Level6_Completion_LinkedAnts_Fill100.png`: Level 6 win screen showing Linked Ants progress filling 0% to 100%.
  - `D1_Level7_LinkedAnts_Overlay.png`: Level 7 entry showing Linked Ants tutorial.
  - `D2_Level7_Restart_LinkedAnts_Overlay.png`: Level 7 restart showing Linked Ants tutorial repeating.
  - `E1_Level15_StackedAnts_Overlay.png`: Level 15 entry showing Stacked Ants tutorial.
  - `E2_Level21_Pipes_Overlay.png`: Level 21 entry showing Pipes tutorial.
  - `E3_Level30_LockedAnts_Overlay.png`: Level 30 entry showing Locked Ants tutorial.
  - `F_Camera_MaxZoomOut_Limit12.png`: Camera zoomed out to maximum limit, stopping strictly at 12.0.
