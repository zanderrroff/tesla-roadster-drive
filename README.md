# Tesla Roadster Free Drive

A noncommercial browser driving game forked from [pmndrs/racing-game](https://github.com/pmndrs/racing-game). Retains the original desert map, heightfield and Cannon raycast-vehicle approach, adapted for the supplied Tesla Roadster model and touch controls.

## Play

[Open the game](https://tesla-roadster-drive.blushwren10.chatgpt.site)

Free driving, ramps, two follow cameras, pause, reset, sound and graphics settings. No account or multiplayer backend. Touch controls support simultaneous steering and acceleration; turn your phone landscape.

Desktop: W/↑ throttle, S/↓/B brake, A/D or ←/→ steer, R selects D/R, Space toggles the handbrake, Backspace resets, C changes camera, Esc pauses. Stop before changing direction.

Touch: rotate the wheel on the left (or switch to arrows), select D/N/R, hold the accelerator or brake on the right. Tap the handbrake to latch/release. Drag the free screen area to orbit; pinch to zoom. Mouse dragging and wheel zoom work too. The camera-return button restores the rear view.

v1.1 adds reflective clear-coat paint, environment lighting, smooth terrain normals, surface grain, instanced ground details, contact shadows, and adaptive render resolution. High quality adds dynamic shadows and the more detailed vehicle. The original stylized desert map is preserved.

## Development

Node 22 or newer:

```sh
npm ci
npm run dev
npm run build
node scripts/check-model.mjs
node --experimental-strip-types scripts/check-controls.mjs
```

Deploy the generated `dist` directory to a static HTTPS host. No secrets or environment variables are required to play. `.openai/hosting.json` identifies the Sites deployment.

## Vehicle pipeline

The user's archive contains `source/tesla_roadster_2_0.zip`, which contains `scene.gltf`, `scene.bin` and `license.txt`. Extract those files into `.model-source/` locally, then run:

```sh
node scripts/prepare-model.mjs .model-source/scene.gltf
```

This normalizes axes, separates four wheel assemblies, preserves body geometry and generates two levels of detail plus wheel geometry metadata. Mobile: 133,574 triangles, 3.15 MB. High: 344,098 triangles, 7.17 MB. Open `/scripts/model-viewer.html` in the development server to compare both with the original. Source archive is not committed; generated assets are ready to use.

The supplied model is an earlier Roadster design. This project does not claim to reproduce the October 2026 reveal or real vehicle performance.

## Licenses

Game code retains the upstream MIT license. **The Tesla model is separately licensed CC BY-NC 4.0 and may not be used commercially.** Attribution and modifications are listed in [public/CREDITS.txt](public/CREDITS.txt). The map is CC BY 4.0. No advertising, payments or analytics are included.
