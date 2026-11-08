# Session implementation

The implemented session lifecycle is documented in the [single session and login guide](../session/README.md).

Start there for runtime initialization, provider/hook wiring, restoration, login, refresh, logout, platform storage, browser synchronization and verification. The controller now owns its store and exposes one `session` object; `restoreSession()` replaces `bootstrap()` and the intermediate `sessionActions` wrapper is removed.
