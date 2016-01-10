#
# Cookbook Name:: Team_Foundation_Server_STD_x64
# Recipe:: default
#
# Copyright (c) 2014 Ryan Irujo, All Rights Reserved.

# Declaring Variables
iso_url           = "https://s3.amazonaws.com/gtbb/software/vs2013.4_tfs_exp_enu.iso"
iso_path          = "#{Chef::Config['file_cache_path']}\\vs2013.4_tfs_exp_enu.iso"
TFS_svc_act       = "NT AUTHORITY\\NETWORK SERVICE"
TFS_svc_pass      = ""
TFS_sysadmins     = "gtbb\\Domain Admins"
TFS_agent_svc_act = "NT Service\\TFSSERVERAGENT"
volume_name = "VS2013_4_TFS_EXP_ENU"
tfsconfigure_exe_file = "C:\\Program Files\\Microsoft Team Foundation Server 12.0\\Tools\\TFSConfig.exe"

psexec_path = "c:\\tools\\pstools\\psexec.exe"

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
			echo "Team Foundation Server STD ISO was mounted Successfully." > "#{Chef::Config['file_cache_path']}\\Team_Foundation_Server_STD_ISO_Mounted_Successfully.txt"
			exit 0;
		}
		
		if ($? -eq $False)
        {
			echo "The Team Foundation Server STD ISO Failed was unable to be mounted." > "#{Chef::Config['file_cache_path']}\\Team_Foundation_Server_STD_ISO_Mount_Failed.txt"
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
		$Install_TFS > "#{Chef::Config['file_cache_path']}\\Team_Foundation_Server_STD_Install_Results.txt"
		EOH
	guard_interpreter :powershell_script
	not_if  { File.exists?(tfsconfigure_exe_file) }
end
