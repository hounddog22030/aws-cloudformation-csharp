#
# Cookbook Name:: vs
# Recipe:: default
#
# Copyright (C) 2016 YOUR_NAME
#
# All rights reserved - Do Not Redistribute
#

include_recipe 'ec2helper'

sentinel_file = "C:/Program Files (x86)/Microsoft Visual Studio 14.0/Common7/IDE/devenv.exe"
admin_xml = "#{Chef::Config['file_cache_path']}/AdminDeployment.xml"
drive_letter = "Q:"
exe_path = "#{drive_letter}/vs_professional.exe"
dirToDelete = 'c:/users/default/AppData'

cookbook_file "AdminDeployment.xml" do
	path "#{admin_xml}"
	action :create_if_missing	
end

ec2helper_mount 'MountEc2Drives' do
	volume_name "VS2015_PRO_ENU"
	drive_letter "#{drive_letter}"
	not_if { File.exist?(exe_path) }
	notifies :run, 'execute[Install Visual Studio]', :immediately
	action :mount
end

execute 'Install Visual Studio' do
	command lazy { "start /high #{exe_path} /quiet /ADMINFILE #{admin_xml}" }
	timeout 43200
	returns [0,3010]
	not_if { File.exist?(sentinel_file) }
	action :nothing
	notifies :delete, "directory[#{dirToDelete}]", :immediately
end


directory "#{dirToDelete}" do
	recursive true
	action :nothing
	notifies :request_reboot, 'reboot[app_requires_reboot]', :immediately
	only_if { ::File.directory?(dirToDelete) }
end

reboot 'app_requires_reboot' do
  reason 'Need to reboot when the run completes successfully.'
  action :nothing
end


