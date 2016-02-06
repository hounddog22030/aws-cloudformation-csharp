param(
  [string]$accountName,
  [string]$password

)

[reflection.assembly]::LoadWithPartialName("Microsoft.SqlServer.SqlWmiManagement") | Out-Null
   $mc = new-object Microsoft.SQLServer.Management.SMO.WMI.ManagedComputer localhost

   $service = $mc.Services["MSSQLSERVER"]
   $service.SetServiceAccount($accountName,$password)
   $service.Alter()

NET STOP MSSQLSERVER
NET START MSSQLSERVER