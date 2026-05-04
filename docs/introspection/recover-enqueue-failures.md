# Recover reported enqueue failures when starting processing jobs

## Summary

**Context:** Starting project processing creates an estimate-relevance job and sends a preprocessing task to the queue.

**Problem:** The start path logs `HasError` from the queue send result but does not fail, roll back, or mark the job retryable.

**Impact:** A project can show a processing job below completion while no preprocessing task exists to advance it, and repeat process clicks can be suppressed by the in-progress check.

**Recommendation:** Make job creation and enqueue outcome consistent: either fail the start request before committing a job or persist a clear retryable/failed-start state.

## Evidence

- Related Introspection issue: `019df549-1f71-70b8-a5c1-3ceb8146b291`
- [Job start](https://github.com/cyharyanto/llassist/blob/main/llassist.ApiService/Services/ProjectProcessingService.cs#L29-L49) starts job creation, sends a preprocessing task, and only logs the returned error flag.
- [Process route](https://github.com/cyharyanto/llassist/blob/main/llassist.ApiService/Controllers/ProjectController.cs#L131-L149) avoids enqueueing new work when the latest job exists and progress is under 100 percent.

## What I Found

The local code already exposes an enqueue error signal through `enqueueResult.HasError`, but the service treats that signal as informational. There is no compensating state transition or retry path in the start workflow.

## Options

| Option | What changes | Pros | Cons |
| ------ | ------------ | ---- | ---- |
| A | Await job creation only after a successful enqueue and throw on `HasError`. | Smallest change; no stuck job record. | A successful enqueue followed by job-write failure still needs handling. |
| B | Create the job first with an explicit status, then enqueue and mark started/failed-start. | Clear recovery state and UI behavior. | Requires status/schema changes. |
| C | Keep the current schema but allow the process route to retry jobs with zero progress and no processed articles. | Avoids schema changes. | Can duplicate work without stronger idempotency checks. |

## Recommended Plan

1. Prefer option B if schema changes are acceptable; otherwise implement option A as a short-term guard.
2. Treat `HasError` as a failed start, not a log-only event.
3. Add tests that simulate reported enqueue failure and assert no unrecoverable in-progress job remains.
4. Make the process route surface a retryable failure or safely enqueue replacement work.

## Acceptance Criteria

- [ ] Issue is linked to this PR.
- [ ] Reported enqueue errors cannot leave a job permanently in progress without work.
- [ ] Users can retry a failed start without manual database repair.
- [ ] Tests cover the reported enqueue-error branch.
