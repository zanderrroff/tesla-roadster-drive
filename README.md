# Tesla Roadster Free Drive

A noncommercial browser driving game forked from [pmndrs/racing-game](https://github.com/pmndrs/racing-game). Uses the upstream Cannon raycast-vehicle approach with the supplied Tesla Roadster and touch controls. v1.2 replaces the mismatched desert heightfield with an original open playground and shared visual/physics dimensions.

## Play

[Open the game](https://tesla-roadster-drive.blushwren10.chatgpt.site)

Free driving, a large drift pad, slalom checkpoints, ramps, drift scoring, jump records, minimap, orbit cameras, pause, reset, sound and graphics settings. No account or multiplayer backend. Touch controls support simultaneous steering and acceleration; turn your phone landscape.

Desktop: W/↑ throttle, S/↓/B brake, A/D or ←/→ steer, R selects D/R, Space toggles the handbrake, Backspace resets, C changes camera, Esc pauses. Stop before changing direction.

Touch: rotate the wheel on the left (or switch to arrows), select D/N/R, hold the accelerator or brake on the right. Tap the handbrake to latch/release. Drag the free screen area to orbit; pinch to zoom. Mouse dragging and wheel zoom work too. The camera-return button restores the rear view.

v1.1 adds reflective clear-coat paint, environment lighting, smooth terrain normals, surface grain, instanced ground details, contact shadows, and adaptive render resolution. High quality adds dynamic shadows and the more detailed vehicle. The original map remains in source assets for provenance; v1.2 uses a purpose-built flat playground.

## Development

Node 22 or newer:

```sh
npm ci
npm run dev
npm run build
node scripts/check-model.mjs
node --experimental-strip-types scripts/check-controls.mjs
node --experimental-strip-types scripts/check-physics.mjs
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

## v1.2 handling fixes

The visible ground and collision surface share y=0, with a thick continuous box underneath. Ramp render and collision transforms share one configuration. Wheel rotation follows road speed instead of a nearly-zero custom sliding speed. Service/parking brake impulses are scaled for the 120 Hz physics step to prevent braking flips; rear grip allows controlled oversteer. Text selection, dragging and long-press callouts are disabled throughout the game. High quality keeps render density at least 1; automatic mode can lower it to favor smoothness.
