default['PsTools']['zip_filename'] = 'pstools.zip'
default['PsTools']['zip_url'] = "https://s3.amazonaws.com/gtbb/software/#{default[:PsTools][:zip_filename]}"
default['PsTools']['dir'] = "c:/pstools"
default['PsTools']['exe_path'] = "#{default[:PsTools][:dir]}/psexec.exe"	
