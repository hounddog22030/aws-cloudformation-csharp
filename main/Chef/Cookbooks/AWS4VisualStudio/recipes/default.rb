#
# Cookbook Name:: AWS4VisualStudio
# Recipe:: default
#
# Copyright (c) 2015 The Authors, All Rights Reserved.

aws4vs = "#{Chef::Config['file_cache_path']}\\AWSToolsAndSDKForNet.msi"

remote_file aws4vs do
  source "http://sdk-for-net.amazonwebservices.com/latest/AWSToolsAndSDKForNet.msi"
end

windows_package 'AWS4VisualStudio' do
  source aws4vs
  action :install
end