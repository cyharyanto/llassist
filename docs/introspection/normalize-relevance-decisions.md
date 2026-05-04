# Normalize relevance decisions against one threshold policy

## Summary

**Context:** LLAssist estimates each article's relevance and contribution to a research question, then uses those fields to decide whether an article is a must-read.

**Problem:** The prompt states that relevance and contribution are true only above 0.7, but the workflow accepts the model's boolean fields directly without validating them against the scores.

**Impact:** A syntactically valid model response can mark an article read or skip contrary to the system's stated threshold.

**Recommendation:** Centralize relevance validation so scores are range-checked and booleans are derived from, or explicitly checked against, the configured threshold before persistence.

## Evidence

- Related Introspection issue: `019df549-1f0c-756e-8429-caa8c70d748b`
- [The relevance prompt](https://github.com/cyharyanto/llassist/blob/main/llassist.ApiService/Services/NLPService.cs#L118-L160) defines score ranges and a 0.7 decision threshold.
- [Article update logic](https://github.com/cyharyanto/llassist/blob/main/llassist.ApiService/Services/EstimateRelevanceService.cs#L193-L197) sets `MustRead` from the model booleans directly.

## What I Found

The score and boolean fields are separate model outputs. No code clamps invalid scores, rejects out-of-range values, or checks that `IsRelevant` and `IsContributing` match the threshold described to the model.

## Options

| Option | What changes | Pros | Cons |
| ------ | ------------ | ---- | ---- |
| A | Treat scores as authoritative and derive booleans deterministically from a single threshold. | Simple and consistent with the prompt. | Changes behavior when the model intended an exception. |
| B | Treat booleans as authoritative but reject/log contradictions with scores. | Preserves model discretion. | Keeps ambiguous decision ownership. |
| C | Store raw model fields and a normalized decision separately. | Best auditability. | Requires schema/view changes. |

## Recommended Plan

1. Start with option A unless product owners need model-discretion exceptions.
2. Add a small validator/normalizer for `Relevance` objects before persistence.
3. Add unit tests for contradictory booleans, boundary scores, and out-of-range scores.
4. Use the normalized fields when setting `MustRead` and writing `ArticleRelevance` rows.

## Acceptance Criteria

- [ ] Issue is linked to this PR.
- [ ] One threshold policy owns the read/skip decision.
- [ ] Invalid or contradictory model outputs are normalized or rejected before persistence.
- [ ] Tests cover score/boolean contradictions.
