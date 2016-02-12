#
# Cookbook Name:: vs
# Recipe:: default
#
# Copyright (C) 2016 YOUR_NAME
#
# All rights reserved - Do Not Redistribute
#


directory 'c:/users/default/AppData' do
	recursive true
	action :delete
end
