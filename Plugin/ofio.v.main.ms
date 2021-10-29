-- This file is a part of OPEN FORMATS I/O project, (C) Alexander Blade, MIXAZZZ
-- dev-c.com, openiv.com

-- Port for GTAV by Cfx.re (https://cfx.re)
-- Description: script that starts every other OF I/O component
-- Last updated: 02 Sep 2021

try (
	if (maxVersion())[1] < 8000 then throw "In order to use OF I/O you need 3DSMax8 or higher"

	if HeapSize < 256000000 then
		messagebox "MAXScript memory heap size is less than 256MB\nRead OFIO.chm for details !" title:"Warning !"

	fileIn "ofio.v.version.ms"
	fileIn "ofio.v.utils.ms"
	fileIn "ofio.v.log.ms"
	fileIn "ofio.v.anim.ms"
	fileIn "ofio.v.gui.ms"
) catch
(
	messagebox (getcurrentexception() as String) title:"OPEN FORMATS I/O EXCEPTION WHILE INITIALIZATION"
)
