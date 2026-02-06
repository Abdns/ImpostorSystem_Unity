# ImpostorSystem

[![Unity](https://img.shields.io/badge/Unity-6000.3%2B-black?logo=unity)](https://unity.com/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

**Octahedral Impostor** baking and rendering system for Unity URP. 

https://github.com/user-attachments/assets/14c06692-19fe-429e-a163-3e89a9274d09

## Features

- **Octahedral Projection** — Capture object from all directions for correct display at any angle
- **Hemisphere / Full Sphere** — Capture mode based on object type
- **Parallax Correction** — Depth simulation for realistic perspective view
- **PBR Compatible** — Full metallic/smoothness workflow support
- **Dynamic Lighting** — Responds to directional and point lights
- **Correct Shadows** — Impostors cast and receive shadows
- **URP Compatible** — Works with Unity's Universal Render Pipeline

## Requirements

- Unity 6000.0 or higher
- Universal Render Pipeline (URP) 17.3+
- Shader Model 4.5

> **Note:** Impostor rendering works only in **Forward rendering path**. Source objects being baked must have shaders with a **GBuffer pass** (standard URP Lit shader is supported).

## Quick Start

### Step 1: Prepare Object

1. Create an empty GameObject and add your model as a child object
2. Ensure the model has correct materials with textures

### Step 2: Add Impostor Component and Configure Settings

1. Select the parent GameObject
2. Add the `Impostor` component (`Add Component → Impostor`)
3. Configure Settings

### Step 4: Bake

1. Click the **"Bake Impostor"** button in the inspector
2. Select folder to save assets
3. Wait for the process to complete

After baking, the following assets will be created:
- `{ObjectName}_ImpostorAlbedoMap.png` — Albedo texture atlas
- `{ObjectName}_ImpostorNormalMap.png` — Normal + depth texture atlas
- `{ObjectName}_ImpostorMaterial.asset` — Configured material
- `{ObjectName}_Impostor` — Ready-to-use impostor GameObject

### Step 5: Use Impostor

The generated impostor GameObject can be used directly or as the last LOD level:

```csharp
// Example: Setup LOD with impostor
LODGroup lodGroup = gameObject.AddComponent<LODGroup>();

LOD[] lods = new LOD[3];
lods[0] = new LOD(0.5f, highDetailRenderers);
lods[1] = new LOD(0.2f, mediumDetailRenderers);
lods[2] = new LOD(0.01f, impostorRenderers);  // Impostor as last LOD

lodGroup.SetLODs(lods);
```

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
