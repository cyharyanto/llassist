# Record per-article NLP failures during review jobs

## Summary

**Context:** Each execution task extracts semantics and estimates relevance for one article, and successful relevance inserts advance job progress.

**Problem:** NLP exceptions are rethrown, while progress only reflects successful relevance rows; there is no durable failed-article state.

**Impact:** One persistently failing article can leave the overall review job incomplete with no clear way for users to distinguish failed work from still-running work.

**Recommendation:** Persist per-article execution status and allow jobs to finish as completed-with-failures, with a retry path for failed articles.

## Evidence

- Related Introspection issue: `019df549-1fd3-71ee-a91e-35012a06c020`
- [Extraction and relevance wrappers](https://github.com/cyharyanto/llassist/blob/main/llassist.ApiService/Services/EstimateRelevanceService.cs#L138-L181) catch NLP exceptions and rethrow.
- [Progress calculation](https://github.com/cyharyanto/llassist/blob/main/llassist.ApiService/Services/ProjectProcessingService.cs#L64-L81) counts only articles with relevance rows for the latest job.
- [Existing test coverage](https://github.com/cyharyanto/llassist/blob/main/llassist.Tests/EstimateRelevanceServiceTests.cs#L277-L309) expects extraction errors to throw.

## What I Found

The worker has no durable status for `failed`, `skipped`, or `retryable` article execution. A failed article therefore looks the same as work that has not completed yet when progress is read.

## Options

| Option | What changes | Pros | Cons |
| ------ | ------------ | ---- | ---- |
| A | Add per-article job execution status rows with success/failure/error fields. | Clean recovery and UI state. | Requires schema and mapper changes. |
| B | Store a failed `ArticleRelevance` placeholder row with error fields. | Reuses progress mechanics. | Mixes decision data with execution status. |
| C | Keep failures transient but add bounded retry/dead-letter behavior in the queue. | Smaller product surface change. | Users still cannot see article-level failure in LLAssist state. |

## Recommended Plan

1. Prefer option A: separate execution status from relevance decisions.
2. Mark article executions as failed after the chosen retry policy is exhausted.
3. Let job progress include terminal success and failure states, and expose completed-with-failures status to the API/UI.
4. Add retry-failed-article or retry-failed-job behavior once statuses are durable.

## Acceptance Criteria

- [ ] Issue is linked to this PR.
- [ ] A persistent NLP failure for one article does not leave the whole job ambiguously incomplete.
- [ ] Users or callers can see which articles failed and retry them.
- [ ] Tests cover failed article status and completed-with-failures job behavior.
