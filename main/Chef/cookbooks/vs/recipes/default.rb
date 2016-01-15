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

include_recipe 'ec2helper'

admin_xml = "#{Chef::Config['file_cache_path']}\\AdminDeployment.xml"

cookbook_file "AdminDeployment.xml" do
	path "#{admin_xml}"
	action :create_if_missing	
end

ec2helper_mount 'MountEc2Drives' do
	volume_name "VS2015_PRO_ENU"
	drive_letter "q"
	notifies :run, 'execute[Install Visual Studio]', :immediately
	action :mount
end

# Installing Team Foundation Server Standard.
execute 'Install Visual Studio' do
	command lazy { "#{node[:vs][:exepath]}:/vs_professional.exe /quiet /ADMINFILE #{admin_xml}" }
	timeout 21600
	returns [0,3010]
	not_if { File.exist?("C:/Program Files (x86)/Microsoft Visual Studio 14.0/Common7/IDE/DevEnv.exe") }
	action :nothing
end

#include_recipe 'Resharper'
#include_recipe 'AWS4VisualStudio'