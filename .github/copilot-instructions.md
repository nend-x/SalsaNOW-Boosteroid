# Copilot Instructions

## Project Guidelines
- User prefers Win32/PInvoke-based solutions such as using `CloseHandle` when asking for Windows handle-management changes.
- Use the shared `NativeMethods` class for Win32/PInvoke declarations instead of declaring native imports directly in feature classes like `BackgroundTasks`.
- Keep handle-closing logic contained in a single function when possible for compact implementations.