Amendment after reading the restored `judge-guides/judge.md`:

**Decision unchanged:** p001, candidate K, round 02, benchmark v1 remains useful provisional UI evidence with invalid benchmark coverage. No scores or lock. The original eight native captures remain the evidence; no new visual evidence was supplied.

| Stable ID | Severity | Clarification and acceptance check |
|---|---|---|
| **EV-001** | Validation blocker | Required state combinations remain incomplete. Supply normal/selected/disabled comparisons and populated inventory evidence at the benchmark sizes. Holdout absence separately prevents final validation. |
| **UI-001** | **Major** | `K03-fishing-1080.jpg`, 1920×1080: simultaneous reeling and “按E提竿” instructions break the distinction between fishing phases. Update the bottom banner with the active phase. Acceptance: each fishing phase presents one consistent current action across both panels. Affected domain: fishing feedback. |
| **UI-002** | Minor | `K08-gallery-1080.jpg`, 1920×1080: A位 lacks the selected border/marker required by the stated grammar. Apply that treatment to the active slot. Acceptance: switching A/B/C visibly moves the selected treatment and updates the text. Affected domain: gallery selection. |

UI-001’s **major** classification is now explicit under the restored rubric. Both UI defects appear locally repairable. They do not prevent useful provisional delivery; UI-001 independently prevents lock.

There is no prior independent verdict for continuity comparison. Production reproducibility and functional behavior remain unproven by these screenshots.
