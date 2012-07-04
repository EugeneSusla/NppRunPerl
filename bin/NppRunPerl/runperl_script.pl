sub process {
	$_[0] =~ m/\[([0-9]+)\]\s(.*)/;
    my $age = $1;
	if ($age eq "") {
		return "[0] ".$_[0];
	} else {
		return "[".($age + 1)."] ".$2."\n";
	}
}
1;
