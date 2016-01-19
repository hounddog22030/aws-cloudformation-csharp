default['PsTools']['zip_filename'] = 'pstools.zip'
default['PsTools']['zip_url'] = "https://download.sysinternals.com/files/#{default[:PsTools][:zip_filename]}"
default['PsTools']['zip_path'] = "#{Chef::Config['file_cache_path']}/#{default[:PsTools][:zip_filename]}"
default['PsTools']['dir'] = "#{Chef::Config['file_cache_path']}/pstools"
default['PsTools']['exe_path'] = "#{default[:PsTools][:dir]}/psexec.exe"	
