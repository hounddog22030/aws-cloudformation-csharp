#
# Cookbook Name:: Team_Foundation_Server_STD_x64
# Recipe:: default
#
# Copyright (c) 2014 Ryan Irujo, All Rights Reserved.
require 'win32/service'
include_recipe 'tfs::applicationtier'
include_recipe 'iis'

iis_config "\"Team Foundation Server/tfs\" -section:basicAuthentication /enabled:true" do
  action :set
end