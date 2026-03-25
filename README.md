# Project Eclypse

## Project folder structure
```
Assets
|-Models
 |-ModelName
  |-Animations
  |-Textures
|-Prefabs
|-Scripts
 |-Behaviours
 |-Helpers
 |-Managers
|-Settings
 |-Audio
 |-Lighting
 |-URP
|-Shared
 |-Materials
 |-Shaders
 |-Textures
```

### Folder guidelines
- Models should have a subfolder per model, which will include a subfolder for the animations and another one for the textures.
- Scripts will be split into Behaviours, Helpers, and Managers, though more can be added if there's a need for them.
- Settings will have subfolders for the Audio, Lighting and URP/environment settings.
- Shared will host ONLY materials, textures, shaders that are used for more than one object or prefab, to keep them accessible.