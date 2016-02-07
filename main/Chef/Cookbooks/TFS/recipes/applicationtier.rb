#
# Cookbook Name:: Team_Foundation_Server_STD_x64
# Recipe:: default
#
# Copyright (c) 2014 Ryan Irujo, All Rights Reserved.

include_recipe 'ec2helper'
include_recipe 'tfs::install'

tfsconfigure_exe_file = "\"C:/Program Files/Microsoft Team Foundation Server 14.0/Tools/TFSConfig.exe\""

configurationFile = "#{Chef::Config['file_cache_path']}/configbasic.ini"

template "#{configurationFile}" do
  source 'configbasic.ini.erb'
  variables(	:application_server_sqlname => "#{node[:tfs][:application_server_sqlname]}",
				:ServiceAccountName => "#{node[:tfs][:ServiceAccountName]}"
end


# Installing Team Foundation Server Standard.
execute 'Configure Team Foundation Server STD' do
	command "#{tfsconfigure_exe_file} unattend /configure /unattendfile:#{configurationFile} /password:#{node[:tfs][:TfsServicePasswordParameterName]}"
end