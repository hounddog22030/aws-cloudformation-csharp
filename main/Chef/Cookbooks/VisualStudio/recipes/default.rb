#
# Cookbook Name:: VisualStudio
# Recipe:: default
#
# Copyright (c) 2015 The Authors, All Rights Reserved.

include_recipe 'VisualStudio::iso'


#include_recipe 'iis'
#include_recipe 'iis::mod_aspnet45'
#include_recipe 'iis::mod_auth_windows'
#include_recipe 'iis::mod_compress_dynamic'
#include_recipe 'iis::mod_logging'
#include_recipe 'iis::mod_security'
#include_recipe 'iis::mod_tracing'

#include_recipe 'SQL2014'

admin_xml = "#{Chef::Config['file_cache_path']}\\AdminDeployment.xml"

cookbook_file "AdminDeployment.xml" do
	path "#{admin_xml}"
	action :create_if_missing	
end

installation = "f://vs_professional.exe"

#remote_file installation do
#  source "https://s3.amazonaws.com/gtbb/software/vs_professional.exe"
#end

# Installing Team Foundation Server Standard.
execute 'Install Visual Studio' do
	command "#{installation} /quiet /ADMINFILE #{admin_xml}"
	timeout 21600
	returns [0,3010]
	not_if { File.exist?("C:/Program Files (x86)/Microsoft Visual Studio 14.0/Common7/IDE/DevEnv.exe") }
end

include_recipe 'Resharper'
include_recipe 'AWS4VisualStudio'