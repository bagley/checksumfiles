#!/usr/bin/perl
#checksums files (sha1) in a folder, and saves the hashes to .FolderContents.sha1

# central db (sqlite or mysql) that holds

use strict;
use warnings;

use File::Next;
use Digest::SHA1;
use File::Basename;

my $last_edit="November 2013";
my $version="0.3";

# for future use of SHA (1, 256, 512, etc)
# my $sha_alg="sha1";

my $sha_file = ".FolderContents.sha1";

my $ignore_file_pattern='/Thumbs\.db$|/desktop\.ini$|/\.git/';

# do you want to save hashes in UPPERCASE?
# note that they can be read in any case.
my $use_uppercase=1;

#debug on(1) or off(0)
my $debug=0;

#############################################

#folders to check
our @folder_list=();

#check files
our (@changed_files,@not_found);

#update files
our (@changed_name,@added_files);

#clean files
our (@removed,@files_exists,@errors);

#############################################

##########################
#Changelog
# Sep 13, 2012
#  Conversion to perl
# Jan 5, 2008
#  All sums now in uppercase, and even lower case will work
# Dec 21, 2007
#  Initial work
##########################


#find files, enter into sha1, save data
#only file name, not path

# 8adsf88asdf8asd8f8dfs8  file.avi

# save as unicode CR+LF
# or as xml file
# <file>3124324134134213  file.avi</file>

# have a size limit for included files. or use sha512 on files smaller
# than a certain size.

# if a new file is found, make comparisons with other hashes 
# to see if it has already been done. this would only be good for 
# files that have been copied.
#   method:
#       get all the hash files of the directories around this one
#       cat $(find /.. | grep .FolderContents.sha1) > temp file
#       grep (sha1 sum) (temp file)
#       prompt "Are these two files the same?"
# 
# *Have a main file (as well as the foldercontents file) that has all the hashes in it, 
# so it can be reference on finding a new file. Files in this main file will have 
# full path names.
#     Method:
#         -on finding a new file, first check the foldercontents (if any)
#          to see if it was just renamed.
#            >if one is found, check if it exists
#            >Prompt user if these two are the same.
#              if they are, and the old one doesn't exist, remove the old one. done
#              otherwise continue
#         -Then reference the main file of all sums
#            >if one is found, and we prompt user if these two files are the same.
#              if it is, check the existance of the old one and remove if not there. done
#              otherwise continue
#         -Then we prompt user if file is new
#            >if it is not new, we have a bad file
#            
# DOUBLE FILES WILL BE FOUND
#   Setup files sometimes will include the same file many times. So after running,
#   go through all the double entries, and prompt the user if they are the same file.
#   if they are, include them on the same line
#   if not, then we need to add something to the second line so it won't be taken as the first.
#   
# ISSUE: Backups will be the same. Don't want to be prompted with this.
#          

#args -r (-u --update)|(-c --check) <folder1> [folder2] [ folder3]...

if ( $#ARGV > -1 and ( $ARGV[0] eq "--version" or $ARGV[0] eq "-v" ) ) {
  print "\nVersion $version";
  print "Internally says last edited $last_edit\n\n";
#  print "While file time stamp says  $(ls -l ~/bin/checksumfiles | cut -f6-8 -d' ')"\n
  exit;
}

sub give_some_help {
    print "checks File Integrity with sha1 checksums
Arguments: $(basename $0) <options> <folder> 
  Main Options 
   -c --check             check sums for all files in folder (priority)
   -u --update            Make new checksums for new files in folder
   -r --recursive         check recursively (default is only current folder)
  Other Options
   --debug                Output debugging info";

    if ( $#ARGV >= 0 ) {
		print $ARGV[0]."\n";
	}
    clean_up();
}

sub bugout {
    print $_."\n" if ($debug);
}



sub print_errors {
	print $_;
	push @errors, $_;
}




our $sha1 = Digest::SHA1->new;
sub checksum {
  my $_f=shift;
  if ( not -f $_f ) {
    print_errors("File has disappeared: $_f\n");
    return undef;
  }
  if ( not -w $_f ) {
    print_errors("Permission denied: $_f\n");
    return undef;
  }
  if ( open(FILE,$_f) ) {
    binmode(FILE);
    Digest::SHA1->new;
    $sha1->addfile(*FILE) || return undef;
    close(FILE);
    return uc($sha1->hexdigest);
  }
  else {
    print_errors("Error opening file: $_f\n");
    return undef;
  }
}

# given a hash line, it returns: (hash,file)
sub format_hash_line {
	
	my $_line=@_;
	chomp $_line;
	my ($_f,$_h);
	
	$_line =~ s/^([0-9,a-f]+)\s+([^\s].+)$/$_h=$1,$_f=$2/i;
	
	return ($_h,$_f);
	
}





# given (file name, current_sha_file) looks in sha file for file
# and returns
#   0 -> does not have checksum
#   1 -> file has checksum
sub has_checksum {
	my ($_filename,$_shafile)=@_;
	# lock file
	my $_return=0;
	if ( not open(SHA,$_shafile) ) {
		print_errors("Failed to open $_shafile\n");
		return 0;
	}
	my @num=grep(/[0-9a-f]i+\s$_filename/, <SHA>);
	if ($#num > -1) {
		$_return=1;
	}
	close(SHA);
}




# given (file name, hash, current sha file) adds hash to sha file
# format: hash  file-name
# return 0 for good, 1 for failure
sub add_checksum {
	my ($_file,$_hash,$_shafile) = @_;
	my $_return=0;
	# lockfile
	
	# open file and get contents
	if ( not open(SHA,$_shafile) ) {
		print_errors("Failed to open $_shafile\n");
		return 1;
	}
	my @files=<SHA>;
	chomp @files;
	close SHA;
	
	# is the file already in the mix? ie has a different name?
	my @hash_present=grep(/$_hash+\s.+/, @files);
	my @new_list;
	if ( $#hash_present > -1 ) {
		@new_list=grep(!/$_hash+\s.+/, @files);
		@files=@new_list;
		print "$_file was renamed to $hash_present[0]\n";
	}
	
	# add new file into mix
	push @files, uc($_hash)."  $_file";
	
	# sort
	@files=sort_hash_lines(@files);
	
	# save
	if ( not open(SHA,'>',$_shafile) ) {
		print_errors("Failed to open $_shafile: $!\n");
		return 1;
	}
	for (@files) {
		print $_ . "\n";
	}
	close SHA;
	system("sync");
}




# given a sha file, cleans out non-existant files
sub clean_checksums {
	my ($_shafile) = @_;
	my $_dir=dirname($_shafile);
	
	# if file is empty, remove it and return
	# prob not going to be used much. is checked below
	#if ( not -s $_shafile ) {
	#	unlink $_shafile;
	#	return 0;
	#}
	
	# lockfile
	
	
	# open file and get contents
	if ( not open(SHA,$_shafile) ) {
		print_errors("Failed to open $_shafile: $!\n");
		return 1;
	}
	my @files=();
	@files=<SHA>;
	chomp @files;
	close SHA;
	
	# list dir and see that all files remain
	my $_rewrite_file=0;
	my @files_exist = ();
	if ( not opendir (DIR, $_dir) ) {
		print_errors("Unable to open dir $_dir: $!\n");
		return 1;
	}	
	# get listing
	my @file_list = grep { !/^\./ && -f "$_dir/$_" } readdir(DIR);
	# if directory is empty, remove and return
	if ( $#file_list < -1 ) {
		unlink $_shafile || print_errors("Unable to remove $_shafile");
		close DIR;
		return 0;
	}
	
	# compare with current file list
	foreach my $_line ( @files ) {
		next if $_line =~ not m/^([0-9,a-f,A-F]+)\s+(.+)$/;
		my ($_h,$_f) = split(/\s+/,$_line,1);
		if ( -f $_dir."/".$_f ) {
			push @files_exist, uc($_h)."  $_dir/$_f";
		}
		else {
			$_rewrite_file=1;
		}
	}
	
	# if list is empty, remove it and return
	if ( $#files_exist == -1 ) {
		unlink $_shafile || print_errors("Unable to remove $_shafile");
		return 0;
	}
	
	# only rewrite if needed
	if ( $_rewrite_file == 1 ) {
		# sort
		@files_exist=sort_hash_lines(@files_exist);
		
		# write
		if (not open(SHA,'>',$_shafile)) {
			print_errors("Failed to open $_shafile: $!\n");
			return 1;
		}
		foreach (@files_exist) {
			print SHA $_."\n";
		}
		close SHA;
		system("sync");
		@files_exist=();
		@files=();
	}
}

sub sort_hash_lines {
	my @_list;
	return map { $_->[0] }
		sort { lc($a->[1]) cmp lc($b->[1]) 
		} map { [$_, /[0-9,a-f]+\s+(.+)/, $_] } @_list;
}


sub clean_up {
	$SIG{INT} = "";
	$SIG{TERM} = "";
	$SIG{HUP} = "";
    exit;
}


### end functions ###


give_some_help "No Options given" unless ( $#ARGV > -1 );
my $update=0;
my $check=0;
my $recursive=0;
foreach (@ARGV) {
  if ( $_ eq '-r' or $_ eq '--recursive' ) {
      $recursive=1 ;
  }
  elsif ( $_ eq '-u' or $_ eq '--update' ) {
	  $update=1;
  }
  elsif ( $_ eq '-c' or $_ eq '--check' ) {    
      $check=1 ;
  }
  elsif ( $_ eq '-v' or $_ eq '--debug' ) {
    $debug=1 ;
  }
  else {
	  if ( -d "$_" or -f "$_" ) {
        push @folder_list, $_;
      }
	  else {
		  give_some_help "Unknown option or folder/file not found: $_";
	  }
  }
}

# was a mode given
if ( $update == 0 and $check == 0 ) {
	give_some_help "So, should I --update, or --check?";
}

# output of args (debug)
bugout "Args: recursive:$recursive - update:$update check:$check";
bugout "Selected folders and files:";
foreach (@folder_list) {
	bugout "$_";
}

#ORDER OF ACTIONS: check, update, clean

$SIG{INT} =  "clean_up";
$SIG{TERM} = "clean_up";
$SIG{HUP} =  "clean_up";


# check method - check that all files are correct
#   get sha1 file list (.FolderContents.sha1)
#   on each sha1 file, with each file:
#     does file exist
#       yes 
#         check and compare the file
#           matches - skip
#           no match - file name saved to bad_files ($BAD_FILES)
#       no
#         record name to not_found file ($NOT_FOUND)


# get files

# check file hashes, by only looking at sha1sum_save_files 
if ( $check == 1 ) {
	
	my $sha_file_paths = File::Next::files( { 
		sort_file => 1, 
		follow_symlinks => 0,
		file_filter => sub { /$sha_file$/ }
		}, @folder_list );
	
	while ( defined ( my $sha_file_path = $sha_file_paths->() ) ) {
    	
		# is it there, does it have data, and can we read it?
		if ( not -s $sha_file_path ) {
			print_errors("Sha file is empty $sha_file_path");
			next;
		}
		if ( not -r $sha_file_path) {
			print_errors("Unable to read $sha_file_path");
			next;
		}
		
		if ( not open (CHKFILE, $sha_file_path) ) {
			print_errors("Failed to open hash file $sha_file_path\n");
			next;
		}
		# read in line by line, and check
		my @hashes = <CHKFILE>;
		chomp @hashes;
		close CHKFILE;
		
		# get dir
		my $dir=dirname($sha_file_path);
		
		# sort list
		@hashes=sort_hash_lines(@hashes);
		foreach my $line (@hashes) {
			
			# skip if it's a space or empty
			next if $line =~ /^\s*$/;
			
			# format is "HASH  /file"
			my ($hash,$file)=split(/\s+/,$line,1);
			
			# is the file there?
			if ( not -f $file ) {
				push @not_found, "$dir/$file\n";
			}
			
			# get hash of file
			my $returned_hash=checksum("$dir/$file");
			if (not defined $returned_hash ) {
				# was it not there, or could not read, or what?
				print_errors("Failed to checksum $file\n");
				next;
			}
			if ( $returned_hash ne $hash ) {
				# add to corrupt/changed files
				push @changed_files, 
				"$dir/$file\n".
            	"  Stored:   $hash\n".
            	"  Checked:  $returned_hash\n";
				print "File has changed: $file\n";
				next;
			}
			# otherwise, next one
		}
	}
	
	# output results
	if ( $#not_found > -1 ) {
		print "\n-------  Could not find these files  -------\n";
		foreach ( @not_found ) {
			print $_ . "\n";
		}
		print "--------------------------------------------\n";
	}
	
	if ( $#errors ) {
		print " *********** The Following ERRORS occured ***********\n";
		foreach (@errors) {
			print $_;
		}
		print "\n\n";
	}
	
	if ( $#changed_files ) {
		print "<<<<<<<<<<   These files have been Changed or *Corrupted*   >>>>>>>>>>>>\n";
		foreach (@changed_files) {
			print $_ . "\n";
		}
		print "<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>\n\n";
	}
	else {
		print "All files are in good condition"
	}
}
			
				


################################

# update method - look for entries of renamed files, and new files
#  add new files and see if any have been renamed
#    listing all files,
#      is file in its FolderContents.sha1 (be exact)
#        yes
#          skip
#        no
#          does sha1sum match any sha1s
#            yes
#              -then the file name was changed.
#               change it to the right name and number
#               record file name to changed_name file ($CHANGED_NAME)
#            no 
#              -add it and record file name to added_files file ($ADDED_FILES)
#  clean FolderContents.sha1
#      for each file in the FolderContents.sha1,
#        does file exist?
#          yes
#            keep
#          no
#            remove and save file name to removed file ($REMOVED)

elsif  ( $update == 1 ) {
	# add new files and fix renamed ones
	bugout "Updating";
	
	my $list = File::Next::everything( { 
		sort_file => 1,
		follow_symlinks => 0,
		file_filter => sub { !/$ignore_file_pattern/ }
		}, @folder_list );
	
	my $skip_dir=0;
	
	while ( defined ( my $fullpath = $list->() ) ) {
		
		if ( -d $fullpath and $recursive eq 1 ) {
			
			print 'Entering -> '.$fullpath."\n";
			$skip_dir=0;
			
		}
		
		elsif ( -f $fullpath and $fullpath =~ m|.*/$sha_file$| ) {
			
			my $file_name=basename($fullpath);
			
			my $dir_name=dirname($fullpath);
			
			my $current_sha_file=$dir_name . "/" . $sha_file;
			
			# if no checksum file, get checksum
			if ( not -f $current_sha_file or has_checksum($fullpath, $current_sha_file) == 0 ) {
				print "Generating sum for $file_name\n";
				my $hash=checksum($fullpath);
				# add to sha_file
				if ( not add_checksum($file_name,$hash,$current_sha_file) ) {
					print_errors("Failed add hash $hash for $file_name in $current_sha_file\n");
				}
				else {
					# add to new files list
					push @added_files, $fullpath;
				}
				next;
			}
			
			# is sha_file writeable?
			if ( not -w $current_sha_file ) {
				if ( $skip_dir == 0 ) {
					print_errors("Cannot write to sha1 file: $current_sha_file\n");
					$skip_dir=1;
				}
				next;
			}
			
			# skip if already has checksum
			next if ( has_checksum($file_name,$current_sha_file) == 1 );
			
			# should never get here
			print_errors("Error: missed file $fullpath\n");
			
		}
		
	}
	
	# run through and clean all sha files
	# (I could have just kept a list of which to clean, based
	# on which ones I modified, however, if a user stops an update
	# half way through and begins again, the skipped ones would not
	# be cleaned until a file was modified in their directories
	
	print "Cleaning up. This may take some time.\n";
	my $sha_list = File::Next::from_file( { 
		sort_file => 1,
		follow_symlinks => 0,
		file_filter => sub { !/$ignore_file_pattern/ }
		}, $sha_file );
	while ( defined ( my $sha_fullpath = $sha_list->() ) ) {
		
		clean_checksums($sha_fullpath);	
		
	}
	
	# Output results
	if ( $#changed_name ) {
		print "-----   The names of these files were changed   -----\n";
		foreach (@changed_name) {
			print $_;
		}
		print "\n\n";
	}
	
	if ( $#added_files ) {
		print "-----      These files were added     -----\n";
		foreach (@added_files) {
			print $_;
		}
		print "\n\n";
	}
	
	if ( $#removed ) {
		print "-----   These files were removed (most likely they were recently deleted)   -----\n";
		foreach (@removed) {
			print $_;
		}
		print "\n\n";
	}
	
	if ( $#errors ) {
		print " ***** The Following ERRORS occured *****\n";
		foreach (@errors) {
			print $_;
		}
		print "\n\n";
	}
	
	print "All done\n";
	
}


clean_up();
