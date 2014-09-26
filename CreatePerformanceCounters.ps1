
function CreateCounter($counterName)
{
	$objCCD1 = New-Object System.Diagnostics.CounterCreationData
	$objCCD1.CounterName = $counterName
	$objCCD1.CounterType = "NumberOfItems64"
	Write-Host "Created performance counter $counterName."
	$objCCD1
}

$categoryName = "DbSync"
$categoryExists = [System.Diagnostics.PerformanceCounterCategory]::Exists($categoryName)

If (-Not $categoryExists)
{
	$categoryType = [System.Diagnostics.PerformanceCounterCategoryType]::SingleInstance
	$objCCDC = New-Object System.Diagnostics.CounterCreationDataCollection
	Write-Host "Creating performance counters for category $categoryName."
	
	$counter = CreateCounter("RowsInserted")
    $objCCDC.Add($counter) | Out-Null
	
	$counter = CreateCounter("RowsUpdated")
    $objCCDC.Add($counter) | Out-Null
	
	$counter = CreateCounter("RowsDeleted")
    $objCCDC.Add($counter) | Out-Null
	
	$counter = CreateCounter("RowsErrored")
    $objCCDC.Add($counter) | Out-Null
	
	[System.Diagnostics.PerformanceCounterCategory]::Create($categoryName, "", $categoryType, $objCCDC) | Out-Null	
}

if ($categoryExists) 
{
	Write-Host "It already exists."
}
