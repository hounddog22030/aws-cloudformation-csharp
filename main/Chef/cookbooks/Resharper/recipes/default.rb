#
# Cookbook Name:: Resharper
# Recipe:: default
#
# Copyright (c) 2015 The Authors, All Rights Reserved.



msi = "#{Chef::Config['file_cache_path']}\\ReSharperSetup.8.2.3000.5195.msi"

remote_file msi do
  source "http://download.jetbrains.com/resharper/ReSharperSetup.8.2.3000.5195.msi"
end

windows_package 'Resharper' do
  source msi
  action :install
end