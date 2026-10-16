# Project collaboration preferences

- When creating or replacing models, apply them to the relevant scene or prefab and save them so the user can inspect them in Unity's Scene view before Play. Do not rely exclusively on runtime replacement for static scene objects. Preserve existing gameplay component references, interaction behavior, transforms and colliders where applicable; avoid duplicate runtime visuals.
- Back up the current scene, including unsaved editor changes, before applying model replacements. Preserve the user's current edits.
- Mok files may be edited freely within the requested task. Before changing another contributor's scripts, explain to the user which parts will change and how, so the contributor can understand the change.

- For text in new or updated Mok features, use Assets/Fonts/BMJUA_ttf.ttf (Jua), or the Jua TMP font assets in Assets/Fonts. Runtime IMGUI should use DeliveryService.Mok.MokUIFont.Font; do not destroy this shared font asset.
