# Step 4 — Delete S3 live keys for sites 5-67 (after EC2 cleared in Step 3).
# Run on LOCAL machine. Archive copy in Archive/2264/ is untouched by this.
# Only run AFTER Step 2 verified the archive.

$bucket = "webstrar-student-backups"
$first  = 5
$last   = 67

Write-Host "Deleting live S3 keys for sites $first-$last ..." -ForegroundColor Yellow

foreach ($w in $first..$last) {
    Write-Host "  website$w ..."
    aws s3 rm "s3://$bucket/WebstrarDeploy/website$w/" --recursive --only-show-errors
}

Write-Host "Live S3 keys deleted. Archive/2264/ preserved." -ForegroundColor Green
