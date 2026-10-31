---
name: qa-code-review
description: Review Unity autotest PRs and testability of changes for Echoes of Ascent. Use for QA review requests or the QA review playbook.
---

# QA Code Review

## Method

1. Gather diff (tests + any production seams)
2. Map claimed coverage → asserts
3. Check EditMode/PlayMode choice
4. Check isolation/determinism/factory usage
5. Check categories vs `unity_qa.yml`
6. Check failure messages usefulness

## Output

```
Verdict: APPROVE | REQUEST CHANGES | BLOCK
Summary: ...
Findings:
- [Px] ...
Coverage map:
- ...
Flake risks:
- ...
CI alignment:
- ...
```

BLOCK for: deleted coverage without replacement, `DemoAI` misuse on product tests, non-deterministic “sometimes pass” merges.
