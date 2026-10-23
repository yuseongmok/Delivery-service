# Project collaboration preferences

- When creating or replacing models, apply them to the relevant scene or prefab and save them so the user can inspect them in Unity's Scene view before Play. Do not rely exclusively on runtime replacement for static scene objects. Preserve existing gameplay component references, interaction behavior, transforms and colliders where applicable; avoid duplicate runtime visuals.
- Back up the current scene, including unsaved editor changes, before applying model replacements. Preserve the user's current edits.
- Mok files may be edited freely within the requested task. Before changing another contributor's scripts, explain to the user which parts will change and how, so the contributor can understand the change.

- For text in new or updated Mok features, use Assets/Fonts/BMJUA_ttf.ttf (Jua), or the Jua TMP font assets in Assets/Fonts. Runtime IMGUI should use DeliveryService.Mok.MokUIFont.Font; do not destroy this shared font asset.

## Cutscene and mission quality baseline (user agreed, 2026-10-23)

- Use the current BankPrologue scene and Assets/Scripts/Mok/Prologue implementation as the quality reference for future cutscenes and story missions. This is a reference, not permission to change unrelated missions or copy bank-specific mechanics everywhere.
- Connect cinematics and controllable gameplay clearly: establish the threat, show character reactions, then deliberately hand control to the player. Support skipping and pausing without losing required story state.
- Match character actions to physical props: reachable hand contacts, articulated doors and locking parts, visible pickup/deposit/carry/boarding sequences, and synchronized sound. Preserve saved scene geometry and existing references.
- Companions should feel present: react briefly to nearby gunfire, glance toward the player between tasks, gesture while waiting, and follow the player through real routes. Avoid interrupting critical prop actions or navigation with ambient reactions.
- Pace combat with an opening encounter, breathing room, reinforcements, and a climax linked to objective progress rather than uniform timed spawning. Keep simultaneous fire bounded and preserve an understandable escape opportunity.
- Maintain cohesive low-poly environments, purposeful lighting, readable props and routes, weapon recoil/muzzle flashes/tracers, hit feedback and ragdolls. Use spatial audio and pursuit cues where appropriate.
- Keep dialogue and useful timing settings editable. Do not imply subtitles are recorded voice: police entrance currently uses subtitles with an optional voice-clip field.
- Validate the complete mission chain, not only isolated effects: cinematic locks and handoff, objective completion, companion navigation, boarding, pursuit, final transition, pause/skip behavior, and isolation from main-game economy. Report what was actually tested and distinguish functional checks from subjective visual polish.
