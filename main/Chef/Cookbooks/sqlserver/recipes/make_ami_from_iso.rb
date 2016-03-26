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

ec2helper_iso 'MountEc2Drives' do
	volume_name "#{node[:sqlserver][:iso_volume_name]}"
	iso_filename "en_sql_server_2014_standard_edition_with_service_pack_1_x64_dvd_6669998.iso"
	iso_path "c:/cfn/files"
	drive_letter_filepath "#{node[:sqlserver][:setup_drive_letter_filename]}"
	action :mount
	not_if { File.exist?("#{node[:sqlserver][:setup_drive_letter_filename]}") }
end

ruby_block "SetSetupDriveLetter" do
	block do
	content = File.read("#{node[:sqlserver][:setup_drive_letter_filename]}")
	content = content[0,1]

		puts "SetSetupDriveLetter"
		puts content
		puts "SetSetupDriveLetter"
		node.default['sqlserver']['setup_drive_letter'] = content
	end
	action :run
end

execute 'Install SQL' do
	timeout 7200
	command lazy { "#{node[:sqlserver][:setup_drive_letter]}:/setup.exe /q /SQLSYSADMINACCOUNTS=#{node[:sqlserver][:SQLSYSADMINACCOUNTS]} /ACTION=Install /FEATURES=#{node[:sqlserver][:FEATURES]} /INSTANCENAME=#{node[:sqlserver][:INSTANCENAME]} /IAcceptSQLServerLicenseTerms=#{node[:sqlserver][:IAcceptSQLServerLicenseTerms]} /SQLSVCACCOUNT=\"#{node[:sqlserver][:SQLSVCACCOUNT]}\" /AGTSVCACCOUNT=\"#{node[:sqlserver][:AGTSVCACCOUNT]}\" /INDICATEPROGRESS /UpdateEnabled=False /SECURITYMODE=#{node[:sqlserver][:SECURITYMODE]} /SAPWD=#{node[:sqlserver][:SAPWD]} /TCPENABLED=#{node[:sqlserver][:TCPENABLED]} /ADDCURRENTUSERASSQLADMIN=#{node[:sqlserver][:ADDCURRENTUSERASSQLADMIN]} /SQLUSERDBDIR=\"#{node[:sqlserver][:SQLUSERDBDIR]}\" /SQLUSERDBLOGDIR=\"#{node[:sqlserver][:SQLUSERDBLOGDIR]}\" /INSTALLSQLDATADIR=\"#{node[:sqlserver][:INSTALLSQLDATADIR]}\"" }
	not_if { File.exist?("#{sentinel_file}") }
	action :run
end
