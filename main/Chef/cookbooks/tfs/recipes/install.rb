#
# Cookbook Name:: Team_Foundation_Server_STD_x64
# Recipe:: default
#
# Copyright (c) 2014 Ryan Irujo, All Rights Reserved.

# Declaring Variables
volume_name = "TFS2015.1_EXPRESS_ENU"

ec2helper_mount 'MountEc2Drives' do
	volume_name "TFS2015.1_EXPRESS_ENU"
	drive_letter "#{node[:tfs][:setup_drive_letter]}"
	not_if { File.exist?("#{node[:tfs][:setup_exe_path]}") }
	notifies :run, 'execute[Install TFS]', :immediately
	action :mount
end

# Installing Team Foundation Server Standard.
execute 'Install TFS' do
	command { "#{node[:tfs][:setup_exe_path]} /quiet" }
	timeout 21600
	returns [0,3010]
	not_if { File.exist?("#{node[:tfs][:config_exe_path]}") }
	action :nothing
end
