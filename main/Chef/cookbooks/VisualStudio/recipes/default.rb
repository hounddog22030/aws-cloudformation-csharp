#
# Cookbook Name:: VisualStudio
# Recipe:: default
#
# Copyright (c) 2015 The Authors, All Rights Reserved.


#include_recipe 'iis'
#include_recipe 'iis::mod_aspnet45'
#include_recipe 'iis::mod_auth_windows'
#include_recipe 'iis::mod_compress_dynamic'
#include_recipe 'iis::mod_logging'
#include_recipe 'iis::mod_security'
#include_recipe 'iis::mod_tracing'

#include_recipe 'SQL2014'

iso_name = "en_visual_studio_professional_2015_x86_x64_dvd_6846629.iso"
iso_url           = "https://s3.amazonaws.com/gtbb/software/#{iso_name}"
iso_path          = "#{Chef::Config['file_cache_path']}/#{iso_name}"
volume_name = "VS2015_PRO_ENU"
exe_path = "f:/vs_professional.exe"

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
		Set-Sleep -s 30
		EOH
	guard_interpreter :powershell_script
	not_if  { File.exists?(tfsconfigure_exe_file) }
end


admin_xml = "#{Chef::Config['file_cache_path']}\\AdminDeployment.xml"

cookbook_file "AdminDeployment.xml" do
	path "#{admin_xml}"
	action :create_if_missing	
end

# Installing Team Foundation Server Standard.
execute 'Install Visual Studio' do
	command "#{exe_path} /quiet /ADMINFILE #{admin_xml}"
	timeout 21600
	returns [0,3010]
	not_if { File.exist?("C:/Program Files (x86)/Microsoft Visual Studio 14.0/Common7/IDE/DevEnv.exe") }
end

include_recipe 'Resharper'
include_recipe 'AWS4VisualStudio'