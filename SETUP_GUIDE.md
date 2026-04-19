# Ghost Route – Cutscene Setup Guide
> Zero Unity experience? No problem. Follow every step in order.

---

## What You Already Have ✅

- `CameraController.cs` — handles the zoomed-out menu view + mouse parallax, and the zoom-in when Play is clicked
- `CutsceneManager.cs` — the conductor: hides the menu, triggers the zoom, plays the animation, rings the phone after 8 seconds
- `Rotation.cs` (CubeInspectProcedural) — adds a subtle breathing rotation to the Rubik's Cube

All three scripts are complete. You just need to set up the scene around them.

---

## PART 1 – Open the Right Scene

1. In Unity, open the **Project** panel (bottom of the screen)
2. Navigate to `Assets → Scenes`
3. Double-click **Main Menu** to open it

---

## PART 2 – Create the Two Camera Anchor Points

The camera needs two "spots" to sit in: one pulled back for the menu, one zoomed in for the cutscene. You'll create these as empty objects.

**Step 1 – Create the Menu camera position:**
1. In the **Hierarchy** panel (left side), right-click on empty space
2. Click **Create Empty**
3. Rename it `CameraPos_Menu` (click the name in the Inspector to rename)
4. Move your Main Camera to roughly where you want the pulled-back shot to be, **then copy those transform values**:
   - Select `Main Camera` in the Hierarchy
   - In the Inspector, note the Position and Rotation values
   - Select `CameraPos_Menu`
   - Paste those same values into its Position and Rotation
   - Then nudge the Z position back by about **1–2 units** (e.g. if camera Z is 0, set CameraPos_Menu Z to -1.5)

**Step 2 – Create the Gameplay camera position:**
1. Right-click in Hierarchy → **Create Empty**
2. Rename it `CameraPos_Gameplay`
3. Set its Position and Rotation to exactly match where you want the first-person POV to be
   - This should be very close to where your hands animation plays from
   - If your camera is already sitting in the right FPS spot, copy those values here

> **Tip:** You can press Play, look at the scene, stop it, and adjust these anchors until it feels right. The camera will smoothly lerp between them.

---

## PART 3 – Attach CameraController to the Camera

1. In the Hierarchy, select **Main Camera**
2. In the Inspector, scroll down and click **Add Component**
3. Type `CameraController` and select it
4. You'll see new slots appear:
   - **Menu Position** → drag `CameraPos_Menu` from the Hierarchy into this slot
   - **Gameplay Position** → drag `CameraPos_Gameplay` into this slot
   - **Menu FOV**: `70` (default, feels slightly wide = pulled back)
   - **Gameplay FOV**: `55` (default, feels tighter = immersive FPS)
   - **Transition Speed**: `2.5` (how fast the zoom-in happens — raise for faster)
   - **Parallax Amount**: `0.35` (how much the camera drifts with the mouse)
   - **Parallax Smooth**: `4` (how lazily it follows — lower = floatier)

---

## PART 4 – Create the Main Menu UI

1. In the Hierarchy, right-click → **UI → Canvas**
   - This creates a Canvas (the container for all UI elements)
   - Unity will also auto-create an **EventSystem** — leave that alone, it's needed
2. Rename the Canvas to `MainMenuCanvas`
3. Select the Canvas, look at the Inspector:
   - Set **Render Mode** to `Screen Space – Overlay`

**Add a background panel (optional but recommended):**
1. Right-click on `MainMenuCanvas` in the Hierarchy → **UI → Panel**
2. This gives a semi-transparent dark overlay — looks cinematic

**Add the Play Button:**
1. Right-click on `MainMenuCanvas` → **UI → Button - TextMeshPro**
   - If it asks to import TMP Essentials, click **Import TMP Essentials**
2. Rename it `PlayButton`
3. Select it, and in the Inspector:
   - Use the **Rect Transform** to position it (center of screen is fine)
   - Expand the button in the Hierarchy, click the **Text (TMP)** child
   - Change the text to `PLAY`
   - Adjust font size, color as you like

**Style it like a detective game (optional):**
- Font color: off-white or amber `#E8D5A3`
- Button background: transparent or dark with a subtle border
- Consider adding a title text above it: right-click Canvas → **UI → Text - TextMeshPro**, type `GHOST ROUTE`

---

## PART 5 – Create the CutsceneManager

1. In the Hierarchy, right-click → **Create Empty**
2. Rename it `CutsceneManager`
3. Click **Add Component** → type `CutsceneManager` → select it
4. Fill in the slots in the Inspector:
   - **Camera Controller** → drag `Main Camera` in (it has the CameraController script on it)
   - **Main Menu UI** → drag `MainMenuCanvas` in
   - **Hands Animator** → drag your hands GameObject in (the one with the Animator component)
   - **Phone Ring Audio** → we'll set this up in Part 7
   - **Zoom Before Animation**: `0.6` (seconds to wait after zoom before animation starts)
   - **Phone Ring Delay**: `8` (seconds after animation starts until phone rings)
   - **Animation Trigger Name**: this must **exactly match** a Trigger parameter in your Animator — see Part 6

---

## PART 6 – Set Up the Animator Trigger

Your hands animation already has an Animator Controller (`HandAnimator.controller`). You need to add a **Trigger** parameter so the CutsceneManager can fire it.

1. In the Project panel, double-click `HandAnimator.controller` to open the **Animator window**
2. In the Animator window, look at the left panel — you'll see tabs: **Layers** and **Parameters**
3. Click **Parameters**
4. Click the **+** button → choose **Trigger**
5. Name it exactly: `Play`
   - This matches the default value in CutsceneManager (`animationTriggerName = "Play"`)
6. Now set up the transition:
   - You should see an `Any State` node and your animation state
   - Right-click `Any State` → **Make Transition** → click your animation state
   - Click the transition arrow to select it
   - In the Inspector, under **Conditions**, click **+**
   - Set it to use the `Play` trigger
   - Uncheck **Has Exit Time** (so it fires immediately when triggered)

---

## PART 7 – Add the Phone Ringing Sound

1. Import your phone ring audio file:
   - Drag an `.mp3` or `.wav` file into the `Assets` folder in the Project panel
2. In the Hierarchy, select your `CutsceneManager` GameObject
3. Click **Add Component** → type `Audio Source` → select it
4. In the Audio Source component:
   - **AudioClip** → drag your ring sound file in
   - **Play On Awake** → uncheck this (CutsceneManager controls when it plays)
   - **Loop** → check this if the phone should keep ringing
5. Now drag this same `CutsceneManager` GameObject into the **Phone Ring Audio** slot in the CutsceneManager script

---

## PART 8 – Wire the Play Button to the Script

This is the last connection — telling the button to call `OnPlayClicked()` when pressed.

1. In the Hierarchy, select `PlayButton`
2. In the Inspector, scroll down to find the **Button** component
3. At the bottom of the Button component, find **On Click ()** — it should say "List is Empty"
4. Click the **+** button
5. A new row appears with two slots:
   - Left slot (the object): drag `CutsceneManager` from the Hierarchy in here
   - Right slot (the function): click the dropdown → find `CutsceneManager` → click `OnPlayClicked`

---

## PART 9 – Attach the Cube Script (Optional)

If you want the Rubik's Cube to have its subtle breathing rotation:

1. Select the Rubik's Cube in the Hierarchy
2. **Add Component** → type `CubeInspectProcedural` → select it
3. Leave the default values or tweak:
   - **Tilt Speed**: `0.7`
   - **Tilt Amount**: `8`

---

## PART 10 – Test It

1. Press **Play** (the triangle button at the top center of Unity)
2. You should see:
   - The scene from a pulled-back angle
   - Moving the mouse should gently shift the camera
   - A PLAY button on screen
3. Click PLAY:
   - Menu disappears
   - Camera smoothly zooms into the FPS position
   - After ~0.6 seconds, the hands animation triggers
   - After 8 more seconds, the phone rings

---

## Troubleshooting

| Problem | Fix |
|---|---|
| Camera doesn't move | Check that `CameraPos_Menu` and `CameraPos_Gameplay` are assigned in CameraController |
| Animation doesn't play | Make sure the Trigger name in the Animator exactly matches `animationTriggerName` in CutsceneManager |
| Play button does nothing | Verify the OnClick() points to CutsceneManager → OnPlayClicked |
| Phone doesn't ring | Check that AudioSource has a clip loaded and Play On Awake is OFF |
| Mouse parallax too strong/weak | Adjust `Parallax Amount` on CameraController (try 0.1 to 0.5) |
| Zoom feels too slow/fast | Adjust `Transition Speed` on CameraController (try 1.5 to 4) |

---

## Scene Hierarchy When Done

```
Main Menu (Scene)
├── Main Camera            ← has CameraController attached
├── CameraPos_Menu         ← empty GameObject (anchor point)
├── CameraPos_Gameplay     ← empty GameObject (anchor point)
├── CutsceneManager        ← has CutsceneManager + AudioSource attached
├── MainMenuCanvas         ← UI Canvas
│   └── PlayButton
│       └── Text (TMP)
├── [Your Hands/Desk model]   ← has Animator attached
└── [Your Rubik's Cube]       ← optionally has CubeInspectProcedural attached
```

---

*Good luck, detective. 🕵️*
