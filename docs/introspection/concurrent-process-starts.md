# Prevent duplicate active screening jobs

## Summary

**Context:** LLAssist starts article screening from the project process endpoint, creates an estimate-relevance job, and enqueues background work for the project's articles.

**Problem:** The process endpoint checks progress and then starts a new job without an atomic active-job guard, so two concurrent requests can both create unfinished jobs for the same project.

**Impact:** A double-click, retrying client, or concurrent API call can duplicate model work, increase cost, and make the latest-job progress/download view hide another in-flight job.

**Recommendation:** Add the smallest active-job gate that makes process-start idempotent for an unfinished project job before enqueueing new work.

## Evidence

- Related Introspection issue: `019e210a-b6a9-77ff-837a-1ab7dcd350ea`
- [Process endpoint](https://github.com/cyharyanto/llassist/blob/main/llassist.ApiService/Controllers/ProjectController.cs#L130-L152) reads current progress before calling the start path.
- [Start path](https://github.com/cyharyanto/llassist/blob/main/llassist.ApiService/Services/ProjectProcessingService.cs#L30-L49) creates a new job and queue message without owning an atomic check/create boundary.
- [Job configuration](https://github.com/cyharyanto/llassist/blob/main/llassist.ApiService/Repositories/Configurations/EstimateRelevanceJobConfiguration.cs) indexes project ID but does not enforce a single unfinished job per project.

## What I Found

The in-progress check and job creation are separated across controller and service calls. If two requests pass the check before either created job is visible as in progress, both can enqueue screening work for the same project. `GetJobProgress` then chooses the latest job, which can obscure another in-flight job from the progress and download path.

## Options

| Option | What changes | Pros | Cons |
| ------ | ------------ | ---- | ---- |
| A | Move the active-job check and job creation into one transactional service method. | Keeps the guard near the state transition; straightforward to test. | Still needs careful handling around enqueue failure. |
| B | Add a database-enforced uniqueness constraint for unfinished jobs per project. | Strongest protection against races across processes. | Requires schema change and a way to represent finished/failed job state. |
| C | Make the start endpoint idempotent by returning the existing unfinished job instead of creating another. | Good user experience for retries and double-clicks. | Needs API response changes if callers need job identity. |

## Recommended Plan

1. Implement option A as the first step: centralize process-start in `ProjectProcessingService` and perform the unfinished-job check and job creation in one transaction.
2. If the service can run multiple API instances, pair option A with option B once job status is explicit enough to define "unfinished" safely.
3. Return the existing unfinished job/progress for retry requests instead of silently starting duplicate work.
4. Add tests with two simulated process-start attempts for the same project and verify only one active job/queue message is created.

## Acceptance Criteria

- [ ] Issue is linked to this PR.
- [ ] Concurrent or repeated starts for the same unfinished project job do not create duplicate active jobs.
- [ ] The process endpoint returns a clear existing-job/progress response when work is already in progress.
- [ ] Enqueue failure handling remains consistent with the existing linked queue-start issue.
- [ ] Process-start tests cover the duplicate-start race or its transactional guard.
