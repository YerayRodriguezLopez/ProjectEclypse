# Crystal_URP.shader
## Quartz-style semi-transparent crystal — Meta Quest 3 / Unity 6 URP

---

## Installation

1. Drop `Crystal_URP.shader` anywhere inside your project's `Assets/` folder.
2. Create a new Material and set its shader to **Custom/Crystal_URP**.
3. Assign the material to your crystal mesh.

---

## Inspector Properties

| Property | Purpose |
|---|---|
| **Surface Texture** | RGBA texture — RGB drives scratch/facet detail brightness, **A channel is the opacity mask** (white = fully opaque, black = fully transparent) |
| **Crystal Color** | Main tint of the crystal. The **alpha of this color is unused** — use `Base Opacity` instead |
| **Base Opacity** | Overall transparency. ~0.45–0.6 gives a convincing quartz look |
| **Fresnel Edge Power** | Sharpness of the edge brightening. Higher = tighter rim, lower = softer glow spreading inward |
| **Fresnel Edge Boost** | Intensity of the edge brightening. Raise for backlit or glowing crystals |
| **Specular Color** | Color of the specular highlight. Keep white for realistic quartz |
| **Smoothness** | Surface polish. 0.85–0.95 = polished faceted quartz |
| **Inner Glow Intensity** | Fake subsurface scatter / refraction. Subtle (0.2–0.5) adds depth without a grab pass |
| **Inner Glow Color** | Color of the inner glow — usually a lighter or warmer version of Crystal Color |

---

## Texture Authoring Guidelines

The RGBA Surface Texture is your main tool for selling the quartz look.

**RGB channels** — paint surface scratches, facet lines, micro-chipping, and dust.
- Dark values = scratched or dirty areas
- White/neutral = clean polished surface

**Alpha channel** — opacity mask.
- Fully white for solid crystal bodies
- Gradient toward edges for chipped or thin looks
- Use to cut custom silhouettes without needing extra geometry

**Recommended resolution:** 512×512. A single-channel detail texture can go down to 256×256 to save VRAM.

**Compression:** ASTC 6×6 (Quest default). Works well for this content type.

---

## Quest 3 Optimization Notes

### What this shader does to stay fast

| Technique | Benefit |
|---|---|
| **Single forward pass, no ShadowCaster** | One draw call per crystal. Transparent shadow casting is expensive and visually wrong for glass/crystal — omitted intentionally |
| **`HLSLINCLUDE` shared block** | Helper functions declared once; no code duplication if you add passes later |
| **`CBUFFER_START(UnityPerMaterial)`** | SRP Batcher compatible — all uniforms in one constant buffer, matches URP batching requirements |
| **`#pragma shader_feature_local`** | Strips unused shadow keyword variants at build time → smaller shader cache, faster load |
| **`#pragma skip_variants`** | Drops lightmap and additional-lights variants — not needed for Quest crystal objects |
| **Blinn-Phong specular** | ~3 ALU ops vs GGX's ~15+. Indistinguishable at Quest 3 resolution and FOV |
| **Schlick Fresnel approximation** | One `dot` + one `pow` — no branching, Adreno/Mali friendly |
| **`half` precision throughout** | Adreno/Mali execute `half` at full rate; reduces register pressure across the board |
| **`#pragma target 3.5`** | Matches Quest 3's actual GPU capability floor — enables more efficient half-precision paths than 3.0 |
| **`UNITY_VERTEX_OUTPUT_STEREO`** | Single-pass stereo rendering enabled — mandatory for Quest performance |
| **No fog** | `multi_compile_fog` stripped entirely — fog breaks stereo depth cues, causes discomfort, and generates 3 wasted shader variants (FOG_LINEAR/EXP/EXP2) even when disabled in project settings |
| **`Cull Off`** | Both faces rendered, so hollow or open meshes work without a second material |

### Recommended Quest render settings
- **MSAA**: 4x (Quest 3 resolves MSAA in tile memory at no bandwidth cost)
- **Foveated Rendering**: Enable Fixed Foveated Rendering in OVRManager
- **Max Additional Lights**: 0–1 for scenes with crystals
- **Render Scale**: 1.0; use FSR if you need headroom

### Batching
- Enable **GPU Instancing** on the material — crystals with the same material batch automatically.
- If individual crystals need different colors, use `MaterialPropertyBlock` per instance rather than creating separate materials.

---

## Variants / Color Ideas

| Crystal Type | Crystal Color (RGB) | Inner Glow Color | Base Opacity |
|---|---|---|---|
| Clear Quartz | `(0.95, 0.98, 1.0)` | `(0.8, 0.9, 1.0)` | 0.50 |
| Rose Quartz | `(1.0, 0.75, 0.78)` | `(1.0, 0.85, 0.88)` | 0.55 |
| Amethyst | `(0.65, 0.4, 0.85)` | `(0.8, 0.6, 1.0)` | 0.60 |
| Citrine | `(1.0, 0.80, 0.25)` | `(1.0, 0.95, 0.5)` | 0.55 |
| Aquamarine | `(0.4, 0.85, 0.9)` | `(0.6, 0.95, 1.0)` | 0.45 |

---

## Known Limitations

- **No fog support** — fog is intentionally omitted. It conflicts with stereo depth perception in VR and wastes shader variants. If you need distance-based fading, drive `_Opacity` from C# based on distance to the camera instead.
- **No refraction** — true refraction requires a grab pass or opaque texture copy, which is expensive on tile GPUs. The Inner Glow parameter approximates it visually at a fraction of the cost.
- **No caustics** — bake caustic patterns into the Surface Texture's RGB channels for a static look, or use a projected decal for a dynamic one.
- **Transparency sorting** — like all alpha-blended objects, overlapping crystals may sort incorrectly. Use the **Render Queue** offset on the material (e.g. 3000, 3001, 3002) to control draw priority between layers.
- **No ShadowCaster pass** — if you need contact shadows beneath crystals, bake them or use a blob shadow decal projector instead.
