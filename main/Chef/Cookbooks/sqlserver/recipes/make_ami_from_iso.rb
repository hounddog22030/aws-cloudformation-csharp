#
# Cookbook Name:: sqlserver
# Recipe:: default
#
# Copyright (C) 2016 YOUR_NAME
#
# All rights reserved - Do Not Redistribute
#

include_recipe 'ec2helper'
include_recipe 'msdotnet35'

sentinel_file = "C:/Program Files/Microsoft SQL Server/MSSQL12.#{node[:sqlserver][:INSTANCENAME]}/MSSQL/Binn/sqlservr.exe"


drive_letter = "S:"
exe_path = "#{drive_letter}/setup.exe"

ec2helper_mountiso 'MountEc2Drives' do
	volume_name "SQLEXPRADV_x64_ENU"
	drive_letter "#{drive_letter}"
	iso_url "https://s3.amazonaws.com/gtbb/software/"
	iso_filename "en_sql_server_2014_standard_edition_with_service_pack_1_x64_dvd_6669998.iso"
	iso_path "#{Chef::Config['file_cache_path']}"
	notifies :run, 'execute[Install SQL]', :immediately
	not_if { File.exist?("#{exe_path}") }
	action :download
end

execute 'Install SQL' do
	timeout 7200
	command "#{exe_path} /q /SQLSYSADMINACCOUNTS=#{node[:sqlserver][:SQLSYSADMINACCOUNTS]} /ACTION=Install /FEATURES=#{node[:sqlserver][:FEATURES]} /INSTANCENAME=#{node[:sqlserver][:INSTANCENAME]} /IAcceptSQLServerLicenseTerms=#{node[:sqlserver][:IAcceptSQLServerLicenseTerms]} /SQLSVCACCOUNT=\"#{node[:sqlserver][:SQLSVCACCOUNT]}\" /AGTSVCACCOUNT=\"#{node[:sqlserver][:AGTSVCACCOUNT]}\" /INDICATEPROGRESS /UpdateEnabled=False /SECURITYMODE=#{node[:sqlserver][:SECURITYMODE]} /SAPWD=#{node[:sqlserver][:SAPWD]} /TCPENABLED=#{node[:sqlserver][:TCPENABLED]} /ADDCURRENTUSERASSQLADMIN=#{node[:sqlserver][:ADDCURRENTUSERASSQLADMIN]} /SQLUSERDBDIR=\"#{node[:sqlserver][:SQLUSERDBDIR]}\" /SQLUSERDBLOGDIR=\"#{node[:sqlserver][:SQLUSERDBLOGDIR]}\" /INSTALLSQLDATADIR=\"#{node[:sqlserver][:INSTALLSQLDATADIR]}\""
	not_if { File.exist?("#{sentinel_file}") }
	action :nothing
end
