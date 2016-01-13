#
# Cookbook Name:: SQL_Server_2012_STD_x64
# Recipe:: default
#
# Copyright (c) 2014 Ryan Irujo, All Rights Reserved.

include_recipe "DotNet35"

# Declaring Variables
iso_url           = "https://s3.amazonaws.com/gtbb/software/SQLServer2014-x64-ENU.iso"
iso_path          = "#{Chef::Config['file_cache_path']}\\SQLServer2014-x64-ENU.iso"
sql_svc_act       = "NT AUTHORITY\\NETWORK SERVICE"
sql_svc_pass      = ""
sql_sysadmins     = "#{ENV['USERDOMAIN']}\\Domain Admins"
sql_agent_svc_act = "NT Service\\SQLSERVERAGENT"

# Download the SQL Server 2012 Standard ISO from a Web Share.
powershell_script 'Download SQL Server 2012 STD ISO' do
	code <<-EOH
		$Client = New-Object System.Net.WebClient
		$Client.DownloadFile( "#{iso_url}", "#{iso_path}")
		EOH
	guard_interpreter :powershell_script
	not_if { File.exists?(iso_path)}
end

# Mounting the SQL Server 2012 SP1 Standard ISO.
powershell_script 'Mount SQL Server 2012 STD ISO' do
	code  <<-EOH
		Mount-DiskImage -ImagePath "#{iso_path}"
        if ($? -eq $True)
		{
			echo "SQL Server 2012 STD ISO was mounted Successfully." > "#{Chef::Config['file_cache_path']}\\SQL_Server_2012_STD_ISO_Mounted_Successfully.txt"
			exit 0;
		}
		
		if ($? -eq $False)
        {
			echo "The SQL Server 2012 STD ISO Failed was unable to be mounted." > "#{Chef::Config['file_cache_path']}\\SQL_Server_2012_STD_ISO_Mount_Failed.txt"
			exit 2;
        }
		EOH
	guard_interpreter :powershell_script
	not_if '($SQL_Server_ISO_Drive_Letter = (gwmi -Class Win32_LogicalDisk | Where-Object {$_.VolumeName -eq "SQLServer"}).VolumeName -eq "SQLServer")'
end

# Installing SQL Server 2012 Standard.
powershell_script 'Install SQL Server 2012 STD' do
	code <<-EOH
		echo "installing..."
		$SQL_Server_ISO_Drive_Letter = (gwmi -Class Win32_LogicalDisk | Where-Object {$_.VolumeName -eq "SQL2014_ENU_x64"}).DeviceID
		cd $SQL_Server_ISO_Drive_Letter\\
		$Install_SQL = ./Setup.exe /q /ACTION=Install /FEATURES=SQLEngine,FullText,SSMS /INSTANCENAME=MSSQLSERVER /SQLSVCACCOUNT="#{sql_svc_act}" /SQLSYSADMINACCOUNTS="#{sql_sysadmins}" /AGTSVCACCOUNT="#{sql_agent_svc_act}" /IACCEPTSQLSERVERLICENSETERMS /INDICATEPROGRESS /UpdateEnabled=False /SECURITYMODE=SQL /SAPWD=JUhsd82.!#
		$Install_SQL > "#{Chef::Config['file_cache_path']}\\SQL_Server_2012_STD_Install_Results.txt"
		EOH
	guard_interpreter :powershell_script
	not_if '((gwmi -class win32_service | Where-Object {$_.Name -eq "MSSQLSERVER"}).Name -eq "MSSQLSERVER")'
end

# Open Firewall To Domain
powershell_script 'Firewall' do
	code <<-EOH
		New-NetFirewallRule -DisplayName "SQL Server" -Direction Inbound -Protocol TCP -LocalPort 1433 -Action allow -Profile Domain
		EOH
	guard_interpreter :powershell_script
end

# Dismounting the SQL Server 2012 STD ISO.
powershell_script 'Delete SQL Server 2012 STD ISO' do
	code <<-EOH
		Dismount-DiskImage -ImagePath "#{iso_path}"
		EOH
	guard_interpreter :powershell_script
	only_if { File.exists?(iso_path)}
end


# Removing the SQL Server 2012 STD ISO from the Directory.
powershell_script 'Delete SQL Server 2012 STD ISO' do
	code <<-EOH
		[System.IO.File]::Delete("#{iso_path}")
		EOH
	guard_interpreter :powershell_script
	only_if { File.exists?(iso_path)}
end