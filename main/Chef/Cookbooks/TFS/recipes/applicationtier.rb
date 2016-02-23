#
# Cookbook Name:: Team_Foundation_Server_STD_x64
# Recipe:: default
#
# Copyright (c) 2014 Ryan Irujo, All Rights Reserved.
require 'win32/service'
include_recipe 'iis::mod_auth_basic'
include_recipe 'ec2helper'
include_recipe 'tfs::install'

configurationFile = "#{Chef::Config['file_cache_path']}/configbasic.ini"

template "#{configurationFile}" do
  source 'configbasic.ini.erb'
  variables(	:application_server_sqlname => "#{node[:tfs][:application_server_sqlname]}",
				:ServiceAccountName => "#{node[:tfs][:TfsServiceAccountName]}", 
				:ServiceAccountPassword => "#{node[:tfs][:TfsServicePassword]}" )
end

# Installing Team Foundation Server Standard.
execute 'Configure Team Foundation Server STD' do
	command "\"#{node[:tfs][:config_exe_path]}\" unattend /configure /unattendfile:#{configurationFile}"
	not_if {::Win32::Service.exists?("TFSJobAgent")}
end

execute 'Configure Team Foundation Server STD' do
	command "\"#{node[:tfs][:security_exe_path]}\" /g+ \"Project Collection Build Service Accounts\" n:\"#{node[:domain]}\tfsbuild\" /collection:http://tfs:8080/tfs/YadaYada"
end
