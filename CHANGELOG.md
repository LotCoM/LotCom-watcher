# `1.0.0`
### Initial Feature Release
- Observation and processing of new Scanner output is now available. This is a default function of the Watcher program.
- Scan Event validation is build into the Watcher. 
  - Validation ensures that scanner output follows process flow and does not accept duplicate scans.
- Messages are sent to the Scanners when they produce invalid output.

## `1.0.1`
#### Optimizations, dependencies, bugfixes
- Scan processing, Database reading, and network messaging improved in both safety and speed.
- An effort to move to a CRUD API inspired design has begun.
- Several classes have been refactored to support changes made to **LotCom Libraries**.
- Failed network communications are now handled and do not crash the program.