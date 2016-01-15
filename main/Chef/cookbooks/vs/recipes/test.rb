#
# Cookbook Name:: vs
# Recipe:: default
#
# Copyright (C) 2016 YOUR_NAME
#
# All rights reserved - Do Not Redistribute
#

#######################################

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

include_recipe 'MountDrives'

MountDrives_mount 'MountEc2Drives' do
	notifies :write, 'log[log1]', :immediately
	notifies :run, 'ruby_block[get-drive]', :immediately
	action :mount
end

log 'log1' do
	message 'this is my message triggered by a notifies'
	level :error
	action :nothing
end


exe_path = ''

ruby_block 'get-drive' do
  block do
	puts 'BEGIN: get-drive'
	volume_name = "VS2015_PRO_ENU"
	script =<<-EOF
	$vInfo = Get-Volume | Where FileSystemLabel -eq "#{volume_name}"
	return $vInfo.DriveLetter
	EOF

	cmd2 = powershell_out!(script)

	Chef::Log.warn("cmd2:#{cmd2}")

	ENV['RBENV_ROOT'] = cmd2.stdout.chop

	exe_path = "#{cmd2.stdout.chop}:/vs_professional.exe"

	Chef::Log.warn("vs.exe_path:#{exe_path}")

	puts "#{cmd2.stdout.chop}:/vs_professional.exe"
	node.default[:vs][:exepath] = cmd2.stdout.chop
	puts 'END: get-drive'
  end
  action :nothing
  notifies :run, 'execute[custom command]', :immediately
end
#directory 'c:/x/y/z' do
#  action :create
#  notifies :run, 'execute[custom command]', :immediately
#  notifies :write, 'log[log1]', :immediately
#end

execute 'custom command' do
  command lazy { "echo the magic variable exe_path is #{node[:vs][:exepath]}" }
  action :nothing
end