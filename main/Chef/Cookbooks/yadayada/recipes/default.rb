#
# Cookbook Name:: yadayada
# Recipe:: default
#
# Copyright (C) 2016 YOUR_NAME
#
# All rights reserved - Do Not Redistribute
#

include_recipe 'windows_ad'

domain 'domain' do

	action :join
end