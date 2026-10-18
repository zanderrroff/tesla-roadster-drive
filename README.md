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

## v1.3: smoother presentation and cabin

Visual chassis/wheels are separated from worker physics updates and damped independently of frame rate. Camera follows the same filtered anchor; pause freezes all visual poses. Suspension damping was retuned. C cycles chase, overview and cockpit views. A lightweight original cabin adds seats, dashboard, live speed display, steering wheel and a seated generic driver. The interior is an artistic reconstruction, not a licensed Tesla interior scan. Soft shadows use a simplified caster in automatic mode and the detailed car in high mode, with texel-aligned tracking.

## v1.4 — Motor Coast

Seven selectable, independently licensed vehicles: Tesla Roadster, BMW M3 E30, Mercedes-Benz AMG GT, Mercedes-Benz 190E Evolution, Nissan Skyline GT-R C110, Porsche 911 Carrera 4S and Ferrari 458 Italia. Fleet models load only when selected, have separate wheel assemblies and per-model dimensions, and add approximately 6 MB of compressed GLB assets in total. Source attribution and derivative licenses are retained in public/models/fleet/LICENSES.md. The Porsche derivative stays CC BY-SA 4.0; the Tesla asset stays noncommercial.

The former primitive character is replaced by Microsoft Rocketbox Male Adult 01 (MIT), posed sitting with original skin/clothing textures. The lightweight reconstructed cabin adds perforated upholstery, trim, a navigation display and a steering wheel. Imported interiors are retained where available; fallback cabins are original interpretations, not manufacturer scans.

Desktop: WASD/arrows, Space held for handbrake, S brakes then reverses after stopping; W brakes a reversing car then drives forward. C cycles cameras, H horn, G garage, Escape pause, Backspace reset. Touch: arrow buttons (default) or steering wheel, independent gas and brake/reverse pedals, hold handbrake, swipe orbit, pinch zoom. Manual D/N/R is optional and turns automatic reverse off; AUTO re-enables it. Touch controls are hidden on large fine-pointer desktops.

Three graphics presets, sound volume, camera sensitivity, stability assistance and two lighting choices are persisted locally. The coastal scenery adds ocean, palms, mountains, a distant skyline and paddock structures outside the driving apron. A real CC0 HDR environment replaces night reflections.

Audio uses attributed engine/skid/impact/ambient recordings, electric-motor synthesis, speed-dependent wind and a horn. The generic combustion engine sample is not a recording of each named vehicle.

Validation for this release: TypeScript/Vite build, fleet geometry and Cannon regression checks, markup rendering and offline asset renders. Browser/WebGL interaction and physical-device performance were not verified because the browser tool was unavailable. No GTA asset/code is used, and GTA-level graphics or universally stable FPS are not claimed.


## Native iPhone port (Unity 6)

The native Unity 6 / URP / Metal / IL2CPP project is in [unity/MotorCoast](unity/MotorCoast). See [build instructions and current status](unity/README.md). Native C++ checks and 14 Unity tests pass; the project imports, renders on Metal, and exports a real iPhone Xcode project. The signed IPA and Apple processing status are tracked separately in unity/VERIFICATION.txt. The existing website remains the browser version.
