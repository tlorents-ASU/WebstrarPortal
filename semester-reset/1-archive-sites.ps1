# Step 1 — Archive live sites 5-67 to Archive/2264/ (S3 server-side copy)
# Run on LOCAL machine. AWS creds already configured.
# COPY (not move) — live stays intact until you verify in Step 2.

$bucket = "webstrar-student-backups"
$term   = "2264"          # Spring 2026 (semester being archived)
$first  = 5
$last   = 67

Write-Host "Archiving sites $first-$last to Archive/$term/ ..." -ForegroundColor Cyan

foreach ($w in $first..$last) {
    $src = "s3://$bucket/WebstrarDeploy/website$w/"
    $dst = "s3://$bucket/Archive/$term/website$w/"
    Write-Host "  website$w ..."
    aws s3 cp $src $dst --recursive --only-show-errors
}

Write-Host "Archive copy complete. Run 2-verify-archive.ps1 next." -ForegroundColor Green
