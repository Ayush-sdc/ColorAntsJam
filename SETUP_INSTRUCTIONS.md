# Color Ants Jam - Setup & Getting Started Instructions

**Project:** Color Ants Jam (inspired by *Color Slither Jam*)  
**Target Platform:** Unity Android (Universal Render Pipeline - URP)  
**Authoritative Unity Version:** `Unity 6000.3.23f1` (6000.3.23f1 09d2ecc7fb28)  
**Repository:** `https://github.com/Ayush-sdc/ColorAntsJam.git`  

---

## 1. Prerequisites

Before opening the project, ensure you have the following installed:
1. **Unity Hub**: Version 3.8.0 or newer.
2. **Unity Editor**: `6000.3.23f1` (Unity 6).
   - Ensure the **Android Build Support** module is installed (including *Android SDK & NDK Tools* and *OpenJDK*).
3. **Git & Git LFS**:
   - Install [Git LFS](https://git-lfs.github.com/):
     ```bash
     git lfs install
     ```

---

## 2. Cloning the Repository

Because this repository uses **Git LFS** for binary assets (models, textures, audio, packages), ensure you clone with LFS enabled:

```bash
# 1. Clone the repository
git clone https://github.com/Ayush-sdc/ColorAntsJam.git

# 2. Enter directory
cd ColorAntsJam

# 3. Pull LFS binary files (if not automatically pulled)
git lfs pull
```

---

## 3. Opening the Project in Unity

1. Open **Unity Hub**.
2. Click **Add** -> **Add project from disk**.
3. Select the cloned `ColorAntsJam` folder.
4. If prompted to select a Unity Editor version, choose **Unity 6000.3.23f1**.
5. Wait for Unity to import assets and compile package dependencies. This initial import takes 2–4 minutes as URP shaders and models are compiled into the local `Library/` cache.

---

## 4. Primary Scene & Build Settings

- **Main Scene Location:** `Assets/Scenes/main.unity`
- **Opening the Scene:**
  In the Unity Project window, navigate to `Assets` -> `Scenes` -> double click `main.unity`.
- **Active Build Scene:**
  Check `File` -> `Build Profiles` or `Build Settings`. `Assets/Scenes/main.unity` is registered as Build Index `0`.

---

## 5. Running the Game in Editor

1. Open `Assets/Scenes/main.unity`.
2. In the **Game View**, set the aspect ratio:
   - Recommended: **Portrait (9:16)** or **1080x1920** (matching mobile device gameplay).
3. Press the **Play** button in the Unity Editor toolbar.
4. **Gameplay Controls:**
   - **Left Mouse Click / Touch Tap**: Tap on an ant queue to command it to slither forward into its matching-color holder.
   - **Click & Drag**: Pan the camera across large boards (Levels with dimensions > 10x10).
   - **Keyboard Navigation Shortcuts** (Game Mode):
     - Press **`E`**: Advance immediately to Next Level.
     - Press **`Q`**: Return to Previous Level.

---

## 6. Level Progression & Editor Utilities

### Resetting Progress:
- To reset player progression back to Level 1, in the top Unity menu select:
  `Tools` -> `Slither Ants Jam` -> `Progression` -> `Reset Progression to Level 1`.

### Importing / Exporting Reference Levels:
- The authoritative 45 levels are stored as raw JSON files in:
  `Assets/Data/ReferenceLevelJSON/Level_001.json` ... `Level_045.json`.
- To re-import all 45 levels into ScriptableObjects, select:
  `Tools` -> `Slither Ants Jam` -> `Import Reference Levels`.
- To export all 45 levels back to JSON, select:
  `Tools` -> `Slither Ants Jam` -> `Export Reference Levels to JSON`.

### Automated Level Validation:
- Run the standalone 45-level validation suite from terminal:
  ```bash
  python validate_all_levels.py
  ```

### Running Unit & Edit Mode Tests:
- In Unity Editor, open the Test Runner window:
  `Window` -> `General` -> `Test Runner`.
- Select the **EditMode** tab and click **Run All** to execute test suites for locked ants, stacked ants, directional pipes, linked groups, and JSON validation.

### Rebuilding Fail Screen UI:
- To rebuild or inspect the Level Failed popup hierarchy:
  `Tools` -> `Slither Ants Jam` -> `Setup Fail Screen`.

---

## 7. Android Build Instructions

1. Switch platform to **Android**:
   `File` -> `Build Profiles` -> Select `Android` -> Click `Switch Platform`.
2. Configure **Player Settings**:
   - `Edit` -> `Project Settings` -> `Player`.
   - Update **Package Name** from template default to your company bundle ID (e.g. `com.company.colorantsjam`).
   - Set **Target API Level** to `Android 14.0 (API Level 34)` or higher for Google Play requirements.
3. Build APK / App Bundle:
   - Click `Build` or `Build and Run`.

---

## 8. Verification & Clean Checkout Checklist

- [x] All 45 levels exist in `Assets/Data/ReferenceLevelJSON/` and `Assets/Data/ReferenceLevels/`.
- [x] `LevelDatabase.asset` references all 45 reference levels.
- [x] Unity packages compile without errors in Unity 6000.3.23f1.
- [x] Git LFS correctly tracks large binary assets (`.fbx`, `.unitypackage`, `.png`, `.psd`).
- [x] Unity-generated folders (`Library/`, `Temp/`, `Logs/`, `UserSettings/`, `obj/`) are properly ignored by `.gitignore`.
