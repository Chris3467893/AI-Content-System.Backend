# GitHub Middleware API MVP Specification

Status: Draft 0.3  
Repository: `Chris3467893/AI-Content-System.Backend`  
Target: .NET 8 Web API  
Primary Goal: Provide a controlled middleware layer between AI tools and the GitHub API so repository changes can be executed deterministically, safely, and auditably.

## 1. Goal

Build a minimal but production-oriented internal API that:

- encapsulates GitHub repository operations
- allows controlled file changes
- validates repository state before write operations
- can be safely used by AI systems
- is easy to run locally in Visual Studio
- is structured for later extension

## 2. Scope of MVP

The MVP shall implement the following REST endpoints:

1. `GET /api/repo/file`
2. `POST /api/repo/file`
3. `PUT /api/repo/file`
4. `DELETE /api/repo/file`
5. `POST /api/repo/file/move`

Optional later phases:

- `POST /api/repo/batch`
- Branch + Pull Request strategy
- Diff preview endpoints
- policy extensions

## 3. Architectural Principles

1. No direct GitHub calls by AI clients.
2. Every write operation must go through a defined command contract.
3. Update and delete operations must validate expected SHA.
4. Allowed target paths are controlled by configuration.
5. Every operation is audit-logged with structured telemetry.
6. The solution must be deterministic and testable.
7. The codebase must be ready for later migration from PAT to GitHub App authentication.

## 4. Technology Requirements

- .NET 8 ASP.NET Core Web API
- Visual Studio runnable without manual restructuring
- Serilog for structured logging
- `HttpClient` or Octokit for GitHub API access
- configuration via `appsettings.json` and `appsettings.Development.json`
- XML documentation for public types and members
- clean layering, even if kept lightweight for MVP

## 5. Required Solution Structure

Use a single Web API project for the MVP, but structure it internally.

```text
/src/AIContentSystem.Backend/
  Controllers/
    RepoController.cs
  Models/
    Requests/
    Responses/
    Options/
  Services/
    Interfaces/
    GitHub/
    Policy/
  Domain/
  Logging/
  Program.cs
  appsettings.json
  appsettings.Development.json
```

## 6. Endpoint Contracts

### 6.1 GET /api/repo/file

Query parameters:

- `repo` (required)
- `path` (required)
- `branch` (optional)

Response:

```json
{
  "path": "src/...",
  "content": "...",
  "sha": "abc123"
}
```

### 6.2 POST /api/repo/file

Request body:

```json
{
  "repo": "owner/repo",
  "path": "src/...",
  "content": "...",
  "commitMessage": "Create file",
  "intent": "create initial service implementation"
}
```

Behavior:

- validate allowed path
- create new file in target repository
- reject if file already exists
- log intent, path, repo, result, duration

### 6.3 PUT /api/repo/file

Request body:

```json
{
  "repo": "owner/repo",
  "path": "src/...",
  "content": "...",
  "expectedSha": "abc123",
  "commitMessage": "Update file",
  "intent": "fix DI registration bug"
}
```

Behavior:

- fetch current file metadata
- compare actual SHA with `expectedSha`
- reject with conflict response if mismatch
- update file only on exact SHA match
- log structured telemetry

### 6.4 DELETE /api/repo/file

Request body:

```json
{
  "repo": "owner/repo",
  "path": "src/...",
  "expectedSha": "abc123",
  "commitMessage": "Delete file",
  "intent": "remove obsolete bootstrap file"
}
```

Behavior:

- validate allowed path
- compare current SHA with expected SHA
- delete only if SHA matches
- return structured result

### 6.5 POST /api/repo/file/move

Request body:

```json
{
  "repo": "owner/repo",
  "sourcePath": "src/old.cs",
  "targetPath": "src/new.cs",
  "commitMessage": "Rename file",
  "intent": "rename service to match domain terminology"
}
```

Behavior:

Internal sequence:

1. read source file
2. create target file with same content
3. delete source file

Error handling rules:

- if create fails, abort without deleting source
- if delete fails after create succeeded, attempt rollback by deleting target
- return structured error with step information
- log both primary failure and compensation result

## 7. Policy Layer

Define a read-only policy validator service based on configuration.

### Allowed paths in MVP

- `/src`
- `/tools`
- `/README.md`
- `/docs`

### Forbidden changes

- `.github/workflows/**`
- `.gitignore`
- root-level config files outside explicit allowlist
- secret or pipeline configuration files

### Requirements

- policy config must come from application configuration
- policy must not be modifiable via runtime API
- all write endpoints must validate paths before execution

## 8. Authentication Strategy

MVP/Lab:

- use GitHub Personal Access Token with minimal scope for contents write access

Future production target:

- GitHub App authentication with installation tokens

Implementation requirement:

- abstract authentication and API calling behind a service boundary so PAT can later be replaced without changing controller contracts

## 9. Logging and Telemetry

Use Serilog and structured logging.

Each write operation must log at least:

- operation
- repo
- path
- intent
- result
- duration
- timestamp
- error details if failed

Intent is mandatory in every write request and must be logged as a structured property.

## 10. Service Design

Required interfaces and services:

- `IGitHubContentService`
- `GitHubContentService`
- `IChangePolicyValidator`
- `ChangePolicyValidator`

Suggested responsibilities:

### IGitHubContentService

- get file content and SHA
- create file
- update file with SHA
- delete file with SHA
- move file with compensation logic

### IChangePolicyValidator

- validate whether a target path is allowed
- return reason when blocked

## 11. Models

Create typed request/response DTOs for all endpoints.

Required request models:

- `GetFileRequest` (optional if query-bound directly)
- `CreateFileRequest`
- `UpdateFileRequest`
- `DeleteFileRequest`
- `MoveFileRequest`

Required response models:

- `FileContentResponse`
- `RepoOperationResult`
- `RepoErrorResponse`

## 12. Error Handling

Use consistent HTTP semantics:

- `200 OK` for successful read/update/delete
- `201 Created` for create
- `400 Bad Request` for invalid input
- `403 Forbidden` for blocked policy path
- `404 Not Found` for missing source file
- `409 Conflict` for SHA mismatch
- `500 Internal Server Error` for unexpected failures

Response bodies should be structured and machine-readable.

## 13. Non-Functional Requirements

- compile cleanly in Visual Studio
- no placeholder code without implementation unless explicitly marked
- public code documented with XML comments
- no unnecessary frameworks
- code should be easy to extend with batch operations later

## 14. Delivery Sequence

Implement in this exact order:

1. project scaffold and configuration
2. logging setup with Serilog
3. options/config classes
4. policy validator
5. GitHub content service
6. GET file endpoint
7. POST create file endpoint
8. PUT update file endpoint with SHA check
9. DELETE file endpoint with SHA check
10. MOVE endpoint with compensation logic
11. final README section for local setup

## 15. Definition of Done

The MVP is done when:

- the project runs locally via F5 in Visual Studio
- all five endpoints exist and compile
- configuration-based path policy is enforced
- update and delete validate SHA
- move performs create/delete with compensation behavior
- structured logging is active
- code is documented and readable
- local configuration instructions are included

## 16. Copilot Execution Rules

When implementing this spec, the coding agent must:

- work directly in the repository
- create the full folder structure and files
- prefer complete files over partial snippets
- avoid speculative placeholders
- keep naming stable and conventional
- not add features outside this MVP unless required for compilation
- keep all write operations behind service abstractions
- ensure `intent` is part of every write request model
- ensure `expectedSha` is enforced for update and delete
- not modify unrelated files

## 17. First Implementation Target

Start with a complete initial scaffold of the Web API project and implement the full MVP in one pass if possible. If not possible in one pass, prioritize a compiling scaffold plus the first three endpoints, then continue with update, delete, and move.
