# Parse repaired model output before persisting screening data

## Summary

**Context:** LLAssist asks the model for structured article semantics and relevance decisions, then persists the parsed objects as screening output.

**Problem:** When deserialization fails, the repair prompt is invoked but its response is never parsed; the code returns a default object instead.

**Impact:** Recoverable model output can become empty semantics or default relevance decisions that look like valid processed results.

**Recommendation:** Parse and validate the repair response, then return an explicit failure when both original and repaired output cannot be deserialized.

## Evidence

- Related Introspection issue: `019df549-1ea6-7456-a4bd-05ebeb12e46e`
- [Response handling](https://github.com/cyharyanto/llassist/blob/main/llassist.ApiService/Services/NLPService.cs#L44-L80) assigns the repair response but returns `new T()` without deserializing it.
- [Extraction](https://github.com/cyharyanto/llassist/blob/main/llassist.ApiService/Services/NLPService.cs#L83-L111) and [relevance estimation](https://github.com/cyharyanto/llassist/blob/main/llassist.ApiService/Services/NLPService.cs#L114-L160) both use the same response handler.

## What I Found

The repair branch is reachable for any non-deserializable model response. After the repair model call, control exits the catch block and immediately logs that repair failed, even though no second deserialize attempt happened.

## Options

| Option | What changes | Pros | Cons |
| ------ | ------------ | ---- | ---- |
| A | Deserialize the repaired response and throw a typed exception if it still fails. | Smallest reliable fix; avoids silent defaults. | Callers must handle visible failures. |
| B | Return a result wrapper with success/error plus the parsed object. | Makes failure explicit without exceptions. | Larger interface change across worker and console callers. |

## Recommended Plan

1. Implement option A first: retry deserialization once after repair.
2. Add tests that cover valid JSON, repairable invalid JSON, and unrecoverable output.
3. Ensure callers surface unrecoverable parse failures instead of persisting default objects.

## Acceptance Criteria

- [ ] Issue is linked to this PR.
- [ ] Repaired output is deserialized before any default object can be returned.
- [ ] Unrecoverable structured-output failures fail visibly.
- [ ] Unit tests cover the repair branch.
