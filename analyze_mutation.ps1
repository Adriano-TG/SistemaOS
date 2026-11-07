$json = Get-Content 'StrykerOutput/2026-11-02.19-50-16/reports/mutation-report.json' | ConvertFrom-Json
$files = $json.files
foreach ($prop in $files.psobject.properties) {
    $name = $prop.Name
    $mutants = $prop.Value.mutants
    if ($null -ne $mutants) {
        $total = $mutants.Count
        $killed = ($mutants | Where-Object {$_.status -eq 'Killed'}).Count
        $survived = ($mutants | Where-Object {$_.status -eq 'Survived'}).Count
        $noCoverage = ($mutants | Where-Object {$_.status -eq 'NoCoverage'}).Count
        $compileError = ($mutants | Where-Object {$_.status -eq 'CompileError'}).Count
        $ignored = ($mutants | Where-Object {$_.status -eq 'Ignored'}).Count
        Write-Host "$name"
        Write-Host "  Total: $total, Killed: $killed, Survived: $survived, NoCoverage: $noCoverage, CompileError: $compileError, Ignored: $ignored"
    }
}