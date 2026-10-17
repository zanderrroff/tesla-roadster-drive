# Release validation

Validated on 17 October 2026 on the local Mac in the Codex Chromium-based browser.

- TypeScript and production Vite build pass.
- Both GLB files pass geometry/index validation and have separate chassis and four wheel assemblies; mobile version: 133,574 triangles.
- Original and optimized models compared in front, rear and side views. More polygons retained on the body to preserve curvature.
- Keyboard acceleration, braking, reverse, reset, pause/resume and camera switch checked in browser.
- Both graphics settings load; console contained no application errors during checks.
- Layout checked at 844 × 390; desktop layout checked at the initial larger viewport.
- Final 133,574-triangle mobile asset measured approximately 23–25 FPS in sampled browser frames at 844 × 390 on this Mac during development. An intermediate lower-detail asset measured about 40 FPS. These are not physical-phone benchmarks; the 30 FPS target is not guaranteed.
- Portrait touch-device emulation displays the rotate-phone overlay and pause state.
- Sites deployment succeeded and public access was enabled.

Limits: physical iOS/Android devices and WebKit were not available for verification. The in-app browser does not support CDP touch injection, so actual simultaneous multitouch remains unverified. The implementation uses independent pointer IDs, pointer capture and cancellation handling. Ramp collision is retained from the upstream physics setup; full jump traversal has not been independently measured.
