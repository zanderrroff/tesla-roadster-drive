# Release validation

Validated on 17 October 2026 on the local Mac in the Codex Chromium-based browser.

- TypeScript and production Vite build pass.
- Both GLB files pass geometry/index validation and have separate chassis and four wheel assemblies; mobile version: 133,574 triangles.
- Original and optimized models compared in front, rear and side views. More polygons retained on the body to preserve curvature.
- Keyboard acceleration, braking, reverse, reset, pause/resume and camera switch checked in browser.
- Both graphics settings load; console contained no application errors during checks.
- Layout checked at 844 × 390; desktop layout checked at the initial larger viewport.
- An intermediate 91,691-triangle mobile model measured about 40 FPS at 844 × 390 on this Mac. This is not a physical-phone benchmark or a measurement of the final 133,574-triangle asset.

Limits: physical iOS/Android devices and WebKit were not available for verification. The in-app browser does not support CDP touch injection, so actual simultaneous multitouch remains unverified. The implementation uses independent pointer IDs, pointer capture and cancellation handling. Ramp collision is retained from the upstream physics setup; full jump traversal has not been independently measured.
