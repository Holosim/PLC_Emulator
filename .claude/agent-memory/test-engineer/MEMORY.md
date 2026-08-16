# Test Engineer — memory

## Test harness notes

<!-- Simulation frameworks, hardware-in-the-loop rigs, and how to run
     each product line's test suite locally vs. in this pipeline. -->

- [PlcEmulator .NET scaffolding: build/test verification + SDD reference-graph checklist](harness_dotnet_scaffolding.md) — how to verify issue #5-style scaffolding and later feature branches; baseline test count updated through issue #14 (36/36, CORE-208 fault-flag pattern); also covers the shallow-clone-hides-a-merge gotcha

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
