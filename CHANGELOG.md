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

## `1.0.2`
#### API Integration
- `ScanOutput` objects utilize the API to retrieve data while parsing.
- `NetworkService` now communicates invalid Parts and an expanded set of validation errors.
- `ReaderService` outputs process-ready `ScanOutput` objects and reads from a new location.
- `ScanValidationService` performs unique scan, valid process, valid part, existing previous process scan, and required field validations.
- `Worker.cs` now operates on the last 60 days of Scans instead of the entire database (much faster).

## `1.0.21`
#### Hotfix
- [`feat/1.0.21`](https://github.com/LotCom/LotCom-watcher/pull/36):
  - Integrate with change to `DAL` in **LotCom Libraries** that requires the LotCom apps to provide their own `HttpClient`.
  - Add dependency injection of `HttpClient` and `UserAgent` services.