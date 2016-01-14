#
# Cookbook Name:: Team_Foundation_Server_STD_x64
# Recipe:: default
#
# Copyright (c) 2014 Ryan Irujo, All Rights Reserved.

# Declaring Variables
iso_name = "en_visual_studio_professional_2015_x86_x64_dvd_6846629.iso"
iso_url           = "https://s3.amazonaws.com/gtbb/software/#{iso_name}"
iso_path          = "#{Chef::Config['file_cache_path']}/#{iso_name}"
volume_name = "VS2015_PRO_ENU"

s3_file iso_path do
	remote_path "/software/#{iso_name}"
	bucket "gtbb"
	aws_access_key_id "#{node[:s3_file][:key]}"
	aws_secret_access_key "#{node[:s3_file][:secret]}"
	s3_url "https://s3.amazonaws.com/gtbb"
	decrypted_file_checksum "7dd6da4564820a481325c9d35ed98cd3-94"
	action :create
end

# Mounting the ISO
powershell_script 'Mount ISO' do
	code  <<-EOH
		Mount-DiskImage -ImagePath "#{iso_path}"
        if ($? -eq $True)
		{
			echo "#{volume_name} was mounted Successfully." > "#{Chef::Config['file_cache_path']}/${volume_name}-Mount.txt"
			exit 0;
		}
		
		if ($? -eq $False)
        {
			echo "#{volume_name} ISO Failed was unable to be mounted." > "#{Chef::Config['file_cache_path']}/#{volume_name}-Failed.txt"
			exit 2;
        }
		EOH
	guard_interpreter :powershell_script
	not_if "($ISO_Drive_Letter = (gwmi -Class Win32_LogicalDisk | Where-Object {$_.VolumeName -eq \"#{volume_name}\"}).VolumeName -eq \"#{volume_name}\")"
end

# Installing Team Foundation Server Standard.
powershell_script 'Install Team Foundation Server STD' do
	code <<-EOH
		echo "installing..."
		$TFS_Server_ISO_Drive_Letter = (gwmi -Class Win32_LogicalDisk | Where-Object {$_.VolumeName -eq "#{volume_name}"}).DeviceID
		cd $TFS_Server_ISO_Drive_Letter/
		$Install_TFS = ./tfs_express.exe /quiet
		$Install_TFS > "#{Chef::Config['file_cache_path']}/Team_Foundation_Server_STD_Install_Results.txt"
		EOH
	guard_interpreter :powershell_script
	not_if  { File.exists?(tfsconfigure_exe_file) }
end
