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

## v1.1 — 2026-10-17

- TypeScript and Vite production build passed. Geometry checks still pass for mobile (133,574 triangles) and high (344,098 triangles), four independent wheels each.
- 11 drivetrain regression assertions pass: forward/reverse torque, neutral, brake priority, latched handbrake, pause and direction-change safety.
- Chromium-based Codex browser on Intel Mac: verified D/R selection by tap and keyboard R, throttle, service braking, handbrake latch and throttle inhibition, reset, mouse orbit, camera return and high-quality loading/shadows.
- Responsive layout inspected at 1280×720 and 844×390. Production preview at 844×390 averaged 40.1 requestAnimationFrame FPS over 90 frames near spawn in automatic quality. Development/HMR sessions measured 15–18 FPS; these are not production or phone benchmarks. FPS varies with location, hardware and load.
- Production smoke test recorded no JavaScript errors. Screenshot: local ignored `.qa/v11-mobile.png`.
- Pointer capture supports independent pedals, wheel and camera pointers, release/cancel/lost-capture cleanup; physical multi-touch/pinch, iOS Safari and Android device tests were not available. No claim of physical-phone validation or universally stable 30 FPS.
- Original stylized map retained; smoother terrain shading, material grain and instanced small ground details improve appearance without replacing its geometry with a photorealistic map.
