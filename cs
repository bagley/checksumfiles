#!/usr/bin/perl
#checksums files (sha1) in a folder, and saves the hashes to .FolderContents.sha1

# central db (sqlite or mysql) that holds

use strict;
use warnings;

use File::Next;
use Digest::SHA1;

my $last_edit="Sep 13, 2012";
my $version="0.2";

# for future use of SHA (1, 256, 512, etc)
# my $sha_alg="sha1";

my $sha_file=".FolderContents.sha1";

# do you want to save hashes in UPPERCASE?
# note that they can be read in any case.
my $use_uppercase=1;

#debug on(1) or off(0)
my $debug=0;

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

#find only in folder
# ls ./
#find recursivly
# find ./

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

if ( $#ARGV > -1 and ( $ARGV[0] eq "--version" or $ARGV[0] eq "-v" ) {
  print "\nVersion $version";
  print "Internally says last edited $last_edit\n\n";
#  print "While file time stamp says  $(ls -l ~/bin/checksumfiles | cut -f6-8 -d' ')"\n
  exit;
}

sub give_some_help {
    print cat << _EOF_
checks File Integrity with sha1 checksums
Arguments: $(basename $0) <options> <folder> 
  Main Options 
   -c --check             check sums for all files in folder (priority)
   -u --update            Make new checksums for new files in folder
   -r --recursive         check recursively (default is only current folder)
  Other Options
   --debug                Output debugging info
_EOF_
    if ( $#ARGV >= 0 ) {
		print $#ARGV[0]."\n";
	}
    clean_up();
}

sub bugout {
    print $_."\n" if ($debug);
}

sub output_data {
	if ( $check == 1 ) {
		if ( $#not_found > -1 ) {
			print "\n-------  Could not find these files  -------\n";
			foreach ( "$NOT_FOUND"
  echo "--------------------------------------------"
 fi
 if [ -f "$BAD_FILES" ] ; then
  echo "<<<<<<<<<<   THESE FILES HAVE BEEN ***CORRUPTED***   >>>>>>>>>>>>"
  echo ""
  cat "$BAD_FILES"
  echo "<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>"
 else
  echo "All files are in good condition"
 fi
fi
			
}
		
    
# DONT translate all hash files to unicode CRLF
#  cant read them in linux

# hashes and arrays


# OUTPUT
echo ""
#check files
if [ $check -eq 1 ] ; then 
 if [ -f "$NOT_FOUND" ] ; then
  echo "-------  Could not find these files  -------"
  cat "$NOT_FOUND"
  echo "--------------------------------------------"
 fi
 if [ -f "$BAD_FILES" ] ; then
  echo "<<<<<<<<<<   THESE FILES HAVE BEEN ***CORRUPTED***   >>>>>>>>>>>>"
  echo ""
  cat "$BAD_FILES"
  echo "<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>"
 else
  echo "All files are in good condition"
 fi
fi

#update and clean files
if [ $update -eq 1 ] ; then
 if [ -f "$CHANGED_NAME" ] ; then
  echo "-----   The names of these files were changed   -----"
  cat "$CHANGED_NAME"
  echo ""
 fi
 if [ -f "$ADDED_FILES" ] ; then
  echo "-----      These files were added     -----"
  cat "$ADDED_FILES"
  echo ""
 fi
 if [ -f "$REMOVED" ] ; then
  echo "-----   These files were removed (most likely they were recently deleted)   -----"
  cat "$REMOVED"
  echo ""
 fi
 echo "All done"
fi

#errors
if [ -f "$ERRORS_FILE" ] ; then 
  echo ""
  echo "    The Following ERRORS occured"
  echo ""
  cat "$ERRORS_FILE" 
fi
}

get_upper_case() {
    RETURN=$(echo "$1" | sed 's!a!A!g' | sed 's!b!B!g' | sed 's!c!C!g' | sed 's!d!D!g' | sed 's!e!E!g' | sed 's!f!F!g')
}

sub checksum {
  local $_f=shift;
  if ( not -f $_f ) {
    print "File has disappeared: $_f\n";
    return undef;
  }
  if ( not -w $_f ) {
    print "Permission denied: $_f\n";
    return undef;
  }
  if ( open(FILE,$_f) ) {
    binmode(FILE);
    $sha1->addfile(*FILE) || return undef;
    close(FILE);
    return $sha1->hexdigest;
  }
  else {
    print "Error opening file: $_f\n";
    return undef;
  }
}

clean_up() {
    trap "" SIGHUP SIGTERM SIGINT
    output_data
    rm -drf $TMP_FOLDER
    exit
}


#############################################

#folders to check
my @folder_list=();

#check files
my (@bad_files,@not_found);

#update files
my (@changed_name,@added_files);

#clean files
my (@removed,@files_exists,@errors);

#############################################


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

#was a mode given
if ( $update == 0 and $check == 0 ) {
	give_some_help "So, should I --update, or --check?";
}

# output of args (debug)
bugout "Args: recursive:$recursive - update:$update check:$check"
bugout "Selected folders and files:"
foreach (@folder_list) {
	bugout "$_";
}

#ORDER OF ACTIONS: check, update, clean

trap "clean_up" SIGHUP SIGTERM SIGINT

# get files
my $files = File::Next::files( '/tmp' );

while ( defined ( my $file = $files->() ) ) {
        # do something...
    }


echo "Getting File List..."
if [ $recursive -eq 1 ] ; then
  cat "$FOLDER_LIST" | while read FOLDER ; do
    bugout "Finding all files under $FOLDER"
    find "$FOLDER" 2> /dev/null >> "$FILE_LIST"
  done
  cat "$FILE_LIST" | grep -v ^"find:" | sort -f  > "${FILE_LIST}2"
  mv -f "${FILE_LIST}2" "${FILE_LIST}"
else
  cat $FOLDER_LIST | while read FOLDER ; do
    bugout "Adding $FOLDER and contents"
    echo -e "$FOLDER" >> "$FILE_LIST"
    if [ -d "$FOLDER" ] ; then
      ls -a1 "$FOLDER" | while read FILE ; do
        echo "${FOLDER}/${FILE}" >> "$FILE_LIST"
      done
    fi
  done
fi

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
	my $dirs = File::Next::dirs( {
	sort_file => 1,
	}, @folder_list );

	while ( defined ( my $dir = $dirs->() ) ) {
    	next if ( -d $dir ;
    	
		my $sha_file="$dir/$sha1sum_save_file";
		# is it there, does it have data, and can we read it?
		if ( not -f $sha_file or not -s $sha_file or not -r  $sha_file) {
			next;
		}
		
		if ( not open (CHKFILE, $sha_file) ) {
			print "Failed to open hash file $sha_file\n";
			#add to error notices
			next;
		}
		# read in line by line, and check
		@hashes = <CHKFILE>;
		chomp @hashes;
		foreach (my $line (sort @hashes)) {
			next if $line =~ /^\s*$/;
			# format is "HASH  /file", so just split by first space?
			my ($hash, $file);
			$line =~ s/^(\w+)\s+([^\s].+)$/$hash=$1,$file=$2/i;
			my $return=checksum("$dir/$file");
			if (not defined $return ) {
				#add to errors
				# was it not there, or could not read, or what?
				print "Failed to checksum $file\n";
				next;
			}
			if ( $return ne $hash ) {
				#add to corrupt files
            	# echo "${FILE} (${DIR}/${FILE})" >> $BAD_FILES
            	# echo "Stored:   $SHA1SUM_STORED" >> $BAD_FILES
            	# echo "checked:  $SHA1SUM_check" >> $BAD_FILES
				# print "File has changed: $file\n";
				next;
			}
			# otherwise, next one
		}
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

elif  [ $update -eq 1 ] ; then
  # add new files and fix renamed ones
  bugout "Updating"
  cat "$FILE_LIST" | while read FILE ; do
    if [ -d "$FILE" ] && [ $recursive -eq 1 ] ; then
      echo "Entering -- $FILE"
    elif [ -f "$FILE" ] && [ "$(basename "$FILE")" != "${SHA1SUM_SAVE_FILE}" ] ; then
      FILE_NAME=$(basename "$FILE")
      FILE_NAME_GREP=$(echo "$FILE_NAME" | sed 's!\[!.!g' | sed 's!\]!.!g') 
      DIR=$(dirname "$FILE")
      DIR1=$(basename "$(dirname "$(dirname "$DIR")")")
      DIR2=$(basename "$(dirname "$DIR")")
      DIR3=$(basename "$DIR")
      DIR_SHORT="${DIR1}/${DIR2}/${DIR3}"
      SHA1_FILE="${DIR}/${SHA1SUM_SAVE_FILE}"
      if [ -w "$DIR" ] ; then
          # see if file doesn't have a hash (do nothing if it's already there, or if more than one)
          if ! [ -f "$SHA1_FILE" ] ; then
            # save hash
            echo "Generating sum for \"${FILE_NAME}\""
            FILE_SUM=$(sha1sum "${FILE}" | cut -f1 -d' ')
            get_upper_case "$FILE_SUM"
            FILE_SUM=$RETURN
            echo "$FILE_SUM  ${FILE_NAME}" >> "${SHA1_FILE}"
            echo "${FILE}" >> "$ADDED_FILES"
            echo "  Hash: ${FILE_SUM}" >> "$ADDED_FILES"
          elif [ $(cat "$SHA1_FILE" | grep -c "  ${FILE_NAME_GREP}"$) -eq 0 ] ; then
            # file may have been moved
            # we do $file since it has the full path
            echo "Generating sum for \"${FILE_NAME}\""
            FILE_SUM=$(sha1sum "${FILE}" | cut -f1 -d' ')
            get_upper_case "$FILE_SUM"
            FILE_SUM=$RETURN
            #echo $FILE_SUM
            # check for sum in file
            if [ $(cat "$SHA1_FILE" | grep -c ^"$FILE_SUM") -gt 0 ] ; then
              
              # see if one of the files is missing. if it is, then file was renamed
              
              FOUND_NEW_FILE=0
              grep ^"$FILE_SUM" "$SHA1_FILE" | while read LINE ; do
                FILE_NAME_READ="$(echo "$LINE" | cut -f3- -d' ')"
                FILE_NAME_READ_GREP=$(echo "$FILE_NAME_READ" | sed 's!\[!.!g' | sed 's!\]!.!g') 
                #echo "$FILE_NAME_READ"
                if ! [ -f "${DIR}/${FILE_NAME_READ}" ] ; then
                  #echo "NOT FOUND ${DIR}/${FILE_NAME_READ}"
                  if [ $FOUND_NEW_FILE -eq 0 ] ; then
                    cat "$SHA1_FILE" | grep -v "$FILE_NAME_READ_GREP"$ > "$FILES_EXIST"
                    cp -f "$FILES_EXIST" "${SHA1_FILE}"
                    echo "$FILE_SUM  ${FILE_NAME}" >> "${SHA1_FILE}"
                    echo "<$FILE_NAME_READ> changed to <${FILE_NAME}> in .../$DIR_SHORT" >> "$CHANGED_NAME"
                    FOUND_NEW_FILE=1
                  fi
                fi
              done
              
              #check if we found a file. if not, then it is a new file
              if [ $(cat "$SHA1_FILE" | grep -c "  ${FILE_NAME_GREP}"$) -eq 0 ] ; then
                #file was not added, so add it
                echo "$FILE_SUM  ${FILE_NAME}" >> "${SHA1_FILE}"
                echo "${FILE}" >> "$ADDED_FILES"
                echo "  Hash: ${FILE_SUM}" >> "$ADDED_FILES"
              fi
            #elif [ $(cat "$SHA1_FILE" | grep -c ^"$FILE_SUM") -eq 1 ] ; then
            #  # just one entry, so check if it is the same file
            #  if ! [ "$(cat "$SHA1_FILE" | grep -c ^"$FILE_SUM" | cut -f3- -d' ')" == "$FILE_NAME" ] ; then
            #    echo "$FILE_SUM  ${FILE_NAME}" >> "${SHA1_FILE}"
            #    echo "${FILE}" >> "$ADDED_FILES"
            #    echo "  Hash: ${FILE_SUM}" >> "$ADDED_FILES"
            #  fi
            else
              # otherwise add file
              echo "$FILE_SUM  ${FILE_NAME}" >> "${SHA1_FILE}"
              echo "${FILE}" >> "$ADDED_FILES"
              echo "  Hash: ${FILE_SUM}" >> "$ADDED_FILES"
            fi
          elif [ $(cat "$SHA1_FILE" | grep -c "  ${FILE_NAME_GREP}"$) -gt 1 ] ; then
            # if file is listed more than once, will need to look at it
            echo "Multiple instances[$(cat "$SHA1_FILE" | grep -c "  ${FILE_NAME}"$)]: ${FILE_NAME}" >> "$ERRORS_FILE"
            echo "  In $SHA1_FILE" >> "$ERRORS_FILE"
            echo "" >> "$ERRORS_FILE"
          fi
      else
         if [ -f "$ERRORS_FILE" ] ; then
          if [ $(grep -c "Cannot write to $DIR" "$ERRORS_FILE") -eq 0 ] ; then
            echo "Cannot write to $DIR" >> "$ERRORS_FILE"
          fi
         else
          echo "Cannot write to $DIR" >> "$ERRORS_FILE"
         fi
      fi   
    fi
   done
   # clean sum files
   echo ""
   echo "Cleaning up. This may take some time."
   #bugout "Cleaning"
   cat "$FILE_LIST" | grep "$SHA1SUM_SAVE_FILE"$ | while read SHA1_FILE ; do
    if [ -f "$SHA1_FILE" ] ; then
      rm -f "$FILES_EXIST"
      # parse FolderContents.sha1 file and remove non existant entries
      echo "Cleaning sum file in $(dirname "$SHA1_FILE")"
      cat "$SHA1_FILE" | while read LINE ; do
        DIR="$(dirname "${SHA1_FILE}")"
        FILE_NAME="$(echo "$LINE" | cut -f3- -d' ')"
        HASH=$(echo "$LINE" | cut -f1 -d' ')
        get_upper_case "$HASH"
        HASH=$RETURN
        if [ -f "${DIR}/${FILE_NAME}" ] ; then
          echo "$HASH  $FILE_NAME" >> "$FILES_EXIST"
        else
          echo "${DIR}/${FILE_NAME}" >> "$REMOVED"
          echo "  Hash: $HASH" >> "$REMOVED"
        fi
      done
      if [ -f "$FILES_EXIST" ] ; then
        # sort files in alphabetical order
        sort -f --output="$SHA1_FILE" --key=2 "$FILES_EXIST"
        rm -f "$FILES_EXIST"
      fi     
    fi
  done
fi


clean_up

