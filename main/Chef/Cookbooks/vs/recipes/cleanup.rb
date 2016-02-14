#
# Cookbook Name:: vs
# Recipe:: default
#
# Copyright (C) 2016 YOUR_NAME
#
# All rights reserved - Do Not Redistribute
#


dirToDelete = 'c:/users/default/AppData'

directory "#{dirToDelete}" do
	recursive true
	action :delete
	only_if { ::File.directory?(dirToDelete) }
end
