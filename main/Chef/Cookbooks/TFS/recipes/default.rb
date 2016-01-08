#
# Cookbook Name:: Team_Foundation_Server_STD_x64
# Recipe:: default
#
# Copyright (c) 2014 Ryan Irujo, All Rights Reserved.

#include_recipe "DotNet35"
include_recipe "PsTools"

# Declaring Variables
iso_url           = "https://s3.amazonaws.com/gtbb/software/vs2013.4_tfs_exp_enu.iso"
iso_path          = "C:\\temp\\vs2013.4_tfs_exp_enu.iso"
TFS_svc_act       = "NT AUTHORITY\\NETWORK SERVICE"
TFS_svc_pass      = ""
TFS_sysadmins     = "gtbb\\Domain Admins"
TFS_agent_svc_act = "NT Service\\TFSSERVERAGENT"
volume_name = "VS2013_4_TFS_EXP_ENU"
tfsconfigure_exe_file = "C:\\Program Files\\Microsoft Team Foundation Server 12.0\\Tools\\TFSConfig.exe"

psexec_path = "c:\\temp\\pstools\\psexec.exe"


cookbook_file "configbasic.ini" do
	path "c:/temp/configbasic.ini"
	action :create_if_missing	
end

powershell_script 'Download ISO' do
	code <<-EOH
		ECHO "#{iso_url}"
		$Client = New-Object System.Net.WebClient
		$Client.DownloadFile( "#{iso_url}", "#{iso_path}")
		EOH
	guard_interpreter :powershell_script
	not_if { File.exists?(iso_path)}
end

# Mounting the Team Foundation Server SP1 Standard ISO.
powershell_script 'Mount Team Foundation Server STD ISO' do
	code  <<-EOH
		Mount-DiskImage -ImagePath "#{iso_path}"
        if ($? -eq $True)
		{
			echo "Team Foundation Server STD ISO was mounted Successfully." > C:\\temp\\Team_Foundation_Server_STD_ISO_Mounted_Successfully.txt
			exit 0;
		}
		
		if ($? -eq $False)
        {
			echo "The Team Foundation Server STD ISO Failed was unable to be mounted." > C:\\temp\\Team_Foundation_Server_STD_ISO_Mount_Failed.txt
			exit 2;
        }
		EOH
	guard_interpreter :powershell_script
	not_if "($TFS_Server_ISO_Drive_Letter = (gwmi -Class Win32_LogicalDisk | Where-Object {$_.VolumeName -eq \"#{volume_name}\"}).VolumeName -eq \"#{volume_name}\")"
end

# Installing Team Foundation Server Standard.
powershell_script 'Install Team Foundation Server STD' do
	code <<-EOH
		echo "installing..."
		$TFS_Server_ISO_Drive_Letter = (gwmi -Class Win32_LogicalDisk | Where-Object {$_.VolumeName -eq "#{volume_name}"}).DeviceID
		cd $TFS_Server_ISO_Drive_Letter\\
		$Install_TFS = ./tfs_express.exe /install /quiet
		$Install_TFS > C:\\temp\\Team_Foundation_Server_STD_Install_Results.txt
		EOH
	guard_interpreter :powershell_script
	not_if  { File.exists?(tfsconfigure_exe_file) }
end

# Installing Team Foundation Server Standard.
execute 'Configure Team Foundation Server STD' do
	command "#{psexec_path} -accepteula -h -u gtbb\\johnny -p 3ORMSkQCxbNo \"C:\\Program Files\\Microsoft Team Foundation Server 12.0\\Tools\\TFSConfig.exe\" unattend /configure /unattendfile:c:\\temp\\configbasic.ini"
	guard_interpreter :powershell_script
end

# Open Firewall To Domain
powershell_script 'Firewall' do
	code <<-EOH
		New-NetFirewallRule -DisplayName "TFS Server" -Direction Inbound -Protocol TCP -LocalPort 8080 -Action allow -Profile Domain
		EOH
	guard_interpreter :powershell_script
end

# Dismounting the Team Foundation Server STD ISO.
powershell_script 'Delete Team Foundation Server STD ISO' do
	code <<-EOH
		Dismount-DiskImage -ImagePath "#{iso_path}"
		EOH
	guard_interpreter :powershell_script
	only_if { File.exists?(iso_path)}
end


# Removing the Team Foundation Server STD ISO from the Temp Directory.
powershell_script 'Delete Team Foundation Server STD ISO' do
	code <<-EOH
		[System.IO.File]::Delete("#{iso_path}")
		EOH
	guard_interpreter :powershell_script
	only_if { File.exists?(iso_path)}
end