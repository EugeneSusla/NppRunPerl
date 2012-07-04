#use feature 'unicode_strings';
#use utf8;

require $ARGV[0];

print "Opening file: ".$ARGV[1]."\n";
open FILE, "<".$ARGV[1] or die $!;
print "File successfully opened\n";


$outputFileName = substr($ARGV[0], 0, rindex($ARGV[0], '\\') + 1)."runperl_output.txt";
print "Output file: ".$outputFileName."\n";
$result = "";
while (<FILE>) { 
	$result = $result.process($_); 
}
close(FILE);

print $result."\n";

open FILE2, ">".$outputFileName or die $!;
print FILE2 $result;
close(FILE2);
