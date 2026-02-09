param(
    [string]$Repo = "mRemoteNG/mRemoteNG",
    [int]$Take = 50,
    [int]$MinDaysSinceUpdate = 0,
    [ValidateSet("updatedAt", "createdAt", "number")]
    [string]$SortBy = "createdAt",
    [int]$MaxRounds = 20,
    [string]$StateFile = "D:\github\mRemoteNG\.project-roadmap\p4_state_processed.txt",
    [string]$BatchScript = "D:\github\mRemoteNG\.project-roadmap\scripts\comment-p4-version-debt-batch.ps1",
    [switch]$Comment
)

if (-not (Test-Path $BatchScript))
{
    throw "Batch script not found: $BatchScript"
}

for ($round = 1; $round -le $MaxRounds; $round++)
{
    Write-Output ("ROUND {0}" -f $round)

    $invokeParams = @{
        Repo               = $Repo
        Take               = $Take
        MinDaysSinceUpdate = $MinDaysSinceUpdate
        SortBy             = $SortBy
        StateFile          = $StateFile
    }

    if ($Comment)
    {
        $invokeParams.Comment = $true
    }

    $output = & $BatchScript @invokeParams
    $lines = @($output)
    foreach ($line in $lines)
    {
        Write-Output $line
    }

    if ($lines.Count -eq 0)
    {
        continue
    }

    if ($lines[0] -like "No version-debt issues found*")
    {
        Write-Output ("DONE after {0} rounds" -f $round)
        break
    }
}
