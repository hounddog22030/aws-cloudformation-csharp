default['tfs']['setup_drive_letter'] = "t:"
default['tfs']['setup_exe_filename'] = "setup.exe"
default['tfs']['setup_exe_path'] = "#{default['tfs']['setup_drive_letter']}/tfs_express.exe"
default['tfs']['config_exe_path'] = "C:/Program Files/Microsoft Team Foundation Server 14.0/Tools/TFSConfig.exe"
default['tfs']['application_server_netbios_name'] = "tfsserver1"
default['tfs']['application_server_sqlname'] = "sql4tfs"
default['tfs']['ServiceAccountName'] = "NT AUTHORITY\\NETWORK SERVICE"
default['tfs']['TfsServicePasswordParameterName'] = "invalid"




#build_agent
default['tfs']['build_agent_zipfile_filename'] = "agent.zip"
default['tfs']['build_agent_zipfile_bucket_name'] = "gtbb"
default['tfs']['build_agent_zipfile_bucket_url'] = "https://s3.amazonaws.com/#{default['tfs']['build_agent_zipfile_bucket_name']}"
default['tfs']['build_agent_zipfile_url'] = "https://s3.amazonaws.com/#{default['tfs']['build_agent_zipfile_bucket_name']}/#{default['tfs']['build_agent_zipfile_filename']}"
default['tfs']['build_agent_zipfile_path'] = "#{Chef::Config['file_cache_path']}/#{default['tfs']['build_agent_zipfile_filename']}"
default['tfs']['build_agent_dir'] = "c:/agent"
default['tfs']['build_agent_command_file_path'] = "#{default['tfs']['build_agent_dir']}/ConfigureAgent.cmd"

default['tfs']['sqlexpress4build_private_dns_name'] = "invalid"
default['tfs']['sqlexpress4build_username'] = "invalid"
default['tfs']['sqlexpress4build_password'] = "invalid"