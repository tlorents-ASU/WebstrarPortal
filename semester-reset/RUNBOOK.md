# Semester Reset Runbook — Sites 5-67

Archive Spring 2026 (term 2264) student work, wipe sites 5-67, load new roster.
**Golden rule: archive + verify BEFORE any delete.**

| # | Where | Command | What it does |
|---|-------|---------|--------------|
| 1 | Local | `./1-archive-sites.ps1` | Copy live sites 5-67 → `Archive/2264/` in S3 (copy, not move) |
| 2 | Local | `./2-verify-archive.ps1` | Per-site count: archive == live. **Stop if any MISMATCH.** |
| 3 | EB deploy | drop `clear-sites.config` in `.ebextensions\`, deploy | Empties `C:\WebstrarDeploy\website{5..67}` on the instance |
| 3b| EB deploy | **delete** `clear-sites.config`, redeploy | So the wipe never runs again |
| 4 | Local | `./4-delete-s3-live.ps1` | Delete live S3 keys 5-67 (archive untouched) |
| 5 | Local | `python 5-clear-old-students.py` | Remove old asurite→site 5-67 rows from DynamoDB |
| 6 | Local | `python ..\load-students.py <new.csv>` | Load new roster into DynamoDB |

## Notes
- Steps 1,2,4,5,6 run on your machine (AWS creds + boto3 already set up).
- Step 3 runs on the EC2 instance via the deploy you already do — no RDP needed.
- Hourly S3 backup is upload-only; it never deletes or re-creates the keys you remove, so no race. Safe to leave running.
- Archive lives at `s3://webstrar-student-backups/Archive/2264/` — keep ~5 months min.
- Untouched: sites 1-3 (instructor), 99/100 (Janaka), 199/200/201 (demo/TA).

## Verify after
- Browse a few sites 5-67 in instructor dashboard → should show empty.
- Spot-check `Archive/2264/website5/` in S3 console → old files present.
- New student logs in → lands on assigned empty site.
