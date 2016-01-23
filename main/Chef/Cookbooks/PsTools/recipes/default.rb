#
# Cookbook Name:: PsTools
# Recipe:: default
#
# Copyright (c) 2015 The Authors, All Rights Reserved.
# Declaring Variables


directory "#{node[:PsTools][:dir]}" do
	action :create
end

windows_zipfile "#{node[:PsTools][:dir]}" do
	source "#{node[:PsTools][:zip_url]}"
	not_if { File.exist?("#{node[:PsTools][:exe_path]}") }
  action :unzip
end
