#!D:/dev/dotnet/object-server/lib/ironruby/bin/ir.exe
#
# This file was generated by RubyGems.
#
# The application 'json' is installed as part of a gem, and
# this file is here to facilitate running it.
#

require 'rubygems'

version = ">= 0"

if ARGV.first =~ /^_(.*)_$/ and Gem::Version.correct? $1 then
  version = $1
  ARGV.shift
end

gem 'json', version
load Gem.bin_path('json', 'prettify_json.rb', version)
