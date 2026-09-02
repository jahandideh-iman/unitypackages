# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-08-30

First release of *Scene Management*.

### Added

- `SceneManager`, wrapping `UnityEngine.SceneManagement.SceneManager.LoadScene` behind `Open(sceneName)`.
- `SceneInitilizer`, an abstract `MonoBehaviour` at `[DefaultExecutionOrder(-100)]` that calls an `Init()` override from `Awake`, so a scene can bootstrap itself before other components run.
