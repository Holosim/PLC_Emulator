# Test Engineer — memory

## Test harness notes

<!-- Simulation frameworks, hardware-in-the-loop rigs, and how to run
     each product line's test suite locally vs. in this pipeline. -->

- [PlcEmulator .NET scaffolding: build/test verification + SDD reference-graph checklist](harness_dotnet_scaffolding.md) — how to verify issue #5-style scaffolding and later feature branches; #10 and #14 landed on sibling branches (52/52 and 36/36 respectively, different prior baselines) and merged together — combined `main` baseline is 61/61; also covers the shallow-clone-hides-a-merge gotcha

## Platform-specific test considerations

<!-- What "correct" means per platform where it isn't obvious from the
     RTVM alone — e.g. VR frame-timing tolerances, glove input-latency
     budgets, jukebox audio-sync tolerances. -->

## Recurring failure patterns

<!-- Bugs or regressions that have shown up more than once, and what
     actually fixed them, so they're recognized faster next time. -->

## Flaky tests

<!-- Tests known to fail intermittently for reasons unrelated to the
     code under test, and the current best guess why. -->
