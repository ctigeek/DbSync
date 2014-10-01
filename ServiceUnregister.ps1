$serviceName = "DbSync"
$description = "Synchronizes data for Control Panel from MySql to Sql Server"
$exePath = "D:\DbSyncService\DbSyncService.exe"


$existingService = Get-WmiObject -Class Win32_Service -Filter "Name='$serviceName'"

if ($existingService) 
{
  "'$serviceName' exists already. Stopping."
  Stop-Service $serviceName
  "Waiting 3 seconds to allow existing service to stop."
  Start-Sleep -s 3

  $existingService.Delete()
  "Waiting 5 seconds to allow service to be uninstalled."
  Start-Sleep -s 5  
}

"Completed."