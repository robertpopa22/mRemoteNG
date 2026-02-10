Get-ChildItem 'D:\github\mRemoteNG\.project-roadmap\issues-db\upstream\*.json' | ForEach-Object {
    $j = Get-Content $_.FullName -Raw | ConvertFrom-Json
    $labels = ($j.labels -join ',')
    $year = if ($j.created_at -and $j.created_at.Length -ge 4) { $j.created_at.Substring(0,4) } else { 'unknown' }
    $waiting = $j.waiting_for_us
    Write-Output ('{0}|{1}|{2}|{3}|{4}|{5}' -f $j.number, $j.state, $labels, $waiting, $year, $j.title)
}
