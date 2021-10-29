-- This file is a part of OPEN FORMATS I/O project, (C) Alexander Blade, MIXAZZZ
-- alexander.sannybuilder.com, openiv.com

-- Port for GTAV by Cfx.re (https://cfx.re)
-- Description: simply functions for common usage
-- Last updated: 16 Sep 2021

global ofutils

struct ofutils (

	fn lowerCase instring = (
		if instring == undefined then return ""
		local upper, lower, outstring
		upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
		lower = "abcdefghijklmnopqrstuvwxyz"
		outstring = copy instring
		for i = 1 to outstring.count do
		(
			j = findString upper outstring[i]
			if (j != undefined) do outstring[i] = lower[j]
		)
		return outstring
	),

	fn extractFileName path = (
		if path.count == 0 then return ""
		local i = path.count, r = ""
		while (i > 0) and (path[i] != "\\") and (path[i] != "/") do
		(
			r = path[i] + r
			i -= 1
		)
		return r
	),

	fn extractFilePath path = (
		if path.count == 0 then return ""
		local i = path.count
		while (i > 0) and (path[i] != "\\") and (path[i] != "/") do
		(
			i -= 1
		)
		local r = substring path 1 i
		return r
	),

	fn removeExt name = (
		if name.count == 0 then return ""
		local i = name.count, dotpos = 0
		while i > 0 do
		(
			if name[i] == "." then
			(
				dotpos = i
				exit
			)
			i -= 1
		)
		local r = name
		if dotpos > 0 then
		(
			r = ""
			for i = 1 to dotpos - 1 do r += name[i]
		)
		return r
	),

	fn round val dp = (
		local a = val * (10 ^ dp)
	 	local b = a as Integer
	 	a = (b as float) / (10 ^ dp)
	 	return a
	),

	fn showException = (
		local e = getcurrentexception()
		if e != undefined then
		(
			local str_to_replace = "-- Runtime error: "
			local pos = findString e str_to_replace
			while (pos != undefined) do
			(
				e = replace e pos str_to_replace.count ""
				pos = findString e str_to_replace
			)
			local s = "EXCEPTION: " + e
			try ( ofioLog.print s ) catch ( )
			messagebox s title:"OPEN FORMATS I/O EXCEPTION"
			try ( gc() ) catch ()
		)
	),

	fn openUtilities = (
		local dummyrollout
		rollout dummyrollout "" ( )
		addRollout dummyrollout
		removeRollout dummyrollout
	)

)
