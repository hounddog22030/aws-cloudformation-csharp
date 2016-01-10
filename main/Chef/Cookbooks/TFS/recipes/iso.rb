#
# Cookbook Name:: Team_Foundation_Server_STD_x64
# Recipe:: default
#
# Copyright (c) 2014 Ryan Irujo, All Rights Reserved.

# Declaring Variables
iso_name = "tfs2015.1_server_enu.iso"
iso_url           = "https://s3.amazonaws.com/gtbb/software/#{iso_name}"
iso_path          = "#{Chef::Config['file_cache_path']}/#{iso_name}"
volume_name = "TFS2015.1_SERVER_ENU"

tfsconfigure_exe_file = "C:/Program Files/Microsoft Team Foundation Server 12.0/Tools/TFSConfig.exe"

s3_file iso_path do
	remote_path "/software/#{iso_name}"
	bucket "gtbb"
	aws_access_key_id "#{node[:s3_file][:key]}"
	aws_secret_access_key "#{node[:s3_file][:secret]}"
	s3_url "https://s3.amazonaws.com/gtbb"
	decrypted_file_checksum "a8cbd23feac3da7925634560cdec78b7-17"
	action :create
end

# Mounting the Team Foundation Server SP1 Standard ISO.
powershell_script 'Mount Team Foundation Server STD ISO' do
	code  <<-EOH
		Mount-DiskImage -ImagePath "#{iso_path}"
        if ($? -eq $True)
		{
			echo "Team Foundation Server STD ISO was mounted Successfully." > "#{Chef::Config['file_cache_path']}/Team_Foundation_Server_STD_ISO_Mounted_Successfully.txt"
			exit 0;
		}
		
		if ($? -eq $False)
        {
			echo "The Team Foundation Server STD ISO Failed was unable to be mounted." > "#{Chef::Config['file_cache_path']}/Team_Foundation_Server_STD_ISO_Mount_Failed.txt"
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
		cd $TFS_Server_ISO_Drive_Letter/
		$Install_TFS = ./tfs_express.exe /install /quiet
		$Install_TFS > "#{Chef::Config['file_cache_path']}/Team_Foundation_Server_STD_Install_Results.txt"
		EOH
	guard_interpreter :powershell_script
	not_if  { File.exists?(tfsconfigure_exe_file) }
end
