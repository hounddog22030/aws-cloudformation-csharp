#
# Cookbook Name:: yadayada_iis
# Recipe:: default
#
# Copyright (C) 2016 YOUR_NAME
#
# All rights reserved - Do Not Redistribute
#
include_recipe 'iis'
include_recipe 'iis::mod_aspnet45'
include_recipe 'iis::mod_auth_windows'
include_recipe 'iis::mod_compress_dynamic'
include_recipe 'iis::mod_logging'
include_recipe 'iis::mod_security'
include_recipe 'iis::mod_tracing'
