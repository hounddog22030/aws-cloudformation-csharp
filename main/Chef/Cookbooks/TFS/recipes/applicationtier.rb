#
# Cookbook Name:: Team_Foundation_Server_STD_x64
# Recipe:: default
#
# Copyright (c) 2014 Ryan Irujo, All Rights Reserved.

include_recipe 'ec2helper'
include_recipe 'PsTools'
include_recipe 'tfs::install'

tfsconfigure_exe_file = "\"C:/Program Files/Microsoft Team Foundation Server 14.0/Tools/TFSConfig.exe\""

configurationFile = "#{Chef::Config['file_cache_path']}/configbasic.ini"

cookbook_file "#{configurationFile}" do
	path "#{Chef::Config['file_cache_path']}/configbasic.ini"
	action :create_if_missing	
end

# Installing Team Foundation Server Standard.
execute 'Configure Team Foundation Server STD' do
	command "#{tfsconfigure_exe_file} unattend /configure /unattendfile:#{configurationFile}"
end