# Step 2 — Verify archive matches live, per site, BEFORE any delete.
# Run on LOCAL machine. Any MISMATCH line = do NOT proceed; re-run Step 1 for that site.

$bucket = "webstrar-student-backups"
$term   = "2264"
$first  = 5
$last   = 67

$mismatch = 0
$totalLive = 0
$totalArch = 0

foreach ($w in $first..$last) {
    $live = (aws s3 ls "s3://$bucket/WebstrarDeploy/website$w/" --recursive | Measure-Object).Count
    $arch = (aws s3 ls "s3://$bucket/Archive/$term/website$w/" --recursive | Measure-Object).Count
    $totalLive += $live
    $totalArch += $arch
    if ($live -ne $arch) {
        Write-Host ("  MISMATCH  website{0}: live={1} archive={2}" -f $w, $live, $arch) -ForegroundColor Red
        $mismatch++
    }
}

Write-Host ""
Write-Host ("Totals  Live: {0}  Archive: {1}" -f $totalLive, $totalArch)
if ($mismatch -eq 0) {
    Write-Host "All sites match. Safe to clear (Step 3)." -ForegroundColor Green
} else {
    Write-Host "$mismatch site(s) mismatched. FIX before clearing." -ForegroundColor Red
}
