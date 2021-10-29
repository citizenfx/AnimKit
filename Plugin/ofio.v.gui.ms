-- This file is a part of OPEN FORMATS I/O project, (C) Alexander Blade, MIXAZZZ
-- dev-c.com, openiv.com

-- Port for GTAV by Cfx.re (https://cfx.re)
-- Description: rollout in the Utilities panel
-- Last updated: 19 Oct 2021

global ofgui

-- if main gui rollout already exists then it must be deleted
if ofgui != undefined then
(
	-- in order to delete anything from Utilities panel the panel must be active itself
	ofutils.openUtilities()
	try (removeRollout ofgui) catch ()
)

-- create new rollout
rollout ofgui "OPEN FORMATS I/O (V)"
(
	group " \\ ANIMATION \\ ONIM "
	(
		button btn_V_ONIM_Import "Import Single" width:120 align:#center
		button btn_V_ONIM_Export_Skel "Export Skel" width:120 align:#center
		label margin_V_ONIM "" height:0
	)

	on btn_V_ONIM_Import pressed do
	(
		local obj
		try (
			if selection.count == 1 and (classof selection[1] == EGIMS_V_Model_Bone)
				then obj = selection[1]
				else obj = medit.GetCurMtl()

			if (obj == undefined) or ((classof obj != Standard) and (classof obj != Dummy) and (classof selection[1] != EGIMS_V_Model_Bone)) then
				throw ("SELECTION ERROR\n\n" + "In order to import animation you must select root bone for skel anim or\nopen target material in Material Editor for uv anim\n")
		) catch
		(
			ofutils.showException()
			return OK
		)

		local input_onim_file_name = getOpenFileName caption:"Select GTA *.onim file" types:"gta open formats animation (*.onim)|*onim"
		if (input_onim_file_name != undefined) and (getFileSize input_onim_file_name) > 0 then
		(
			with undo off
			(
				with redraw off
				(
					local onim = ofio_iv_anim()
					ofioLog.start()

					try (
						local start_time = timeStamp()
						ofioLog.print "reading input file"
						onim.read(input_onim_file_name)
						ofioLog.print "building animation"
						onim.build obj
						ofioLog.print ("animation import succeed, elapsed " + ( ( (timeStamp() - start_time) / 1000.0 ) as String ) + " seconds")
						gc()
					) catch
					(
						ofutils.showException()
						ofioLog.print "IMPORT ERROR !"
					)

					ofioLog.finish()
					onim = undefined
				)
			)
		)
	)

	on btn_V_ONIM_Export_Skel pressed do
	(
		local fname
		try (
			--if (Selection.Count != 1) or (getUserProp Selection[1] "id" != 0) or (ofutils.lowercase(getUserProp Selection[1] "type") != "bone") then
			--	throw "SELECTION ERROR\n\nOnly root bone of the skel must be selected in order to export skel bone animation\n"
			--if (Selection[1].parent == undefined) or (ofutils.lowercase(getUserProp Selection[1].parent "type") != "drawable") then
			--	throw "SELECTION ERROR\n\nRoot bone must have parent drawable\n"
			fname = ofutils.removeExt(Selection[1].parent.name) + ".onim"
		) catch
		(
			ofutils.showException()
			return OK
		)
		local pathOut = getSavePath caption:("Select directory for exporting '" + fname + "'.")
		if pathOut == undefined then return OK

		with undo off
		(
			with redraw off
			(
				ofioLog.start()
				try (
					local start_time = timeStamp()
					local onim = ofio_iv_anim()
					ofioLog.print ("dumping " + Selection[1].name)
					onim.dump Selection[1]
					ofioLog.print ("writing " + fname)
					onim.write(pathOut + "\\" + fname)
					ofioLog.print ("Skel onim export succeed, elapsed " + ( ( (timeStamp() - start_time) / 1000.0 ) as String ) + " seconds")
					gc()
				) catch
				(
					ofutils.showException()
					ofioLog.print "EXPORT ERROR !"
				)
				ofioLog.finish()
			)
		)
	)

	group " \\ ANIMKIT "
	(
		button btn_V_ANIMKIT_QuickImport "Quick Import" width:120 align:#center
		button btn_V_ANIMKIT_QuickExport "Quick Export" width:120 align:#center
		label margin_V_ANIMKIT "" height:0
	)

	on btn_V_ANIMKIT_QuickImport pressed do
	(
		local obj
		try (
			if selection.count == 1 and (classof selection[1] == EGIMS_V_Model_Bone)
				then obj = selection[1]
				else obj = medit.GetCurMtl()

			if (obj == undefined) or ((classof obj != Standard) and (classof obj != Dummy) and (classof selection[1] != EGIMS_V_Model_Bone)) then
				throw ("SELECTION ERROR\n\n" + "In order to import animation you must select root bone for skel anim or\nopen target material in Material Editor for uv anim\n")
		) catch
		(
			ofutils.showException()
			return OK
		)

		with undo off
		(
			with redraw off
			(
				local onim = ofio_iv_anim()
				ofioLog.start()

				try (
					ofioLog.print ("searching for AnimKit export file")

					-- TODO: is there any better way to find ROAMING?
					local export_file_path = "$temp\\..\\..\\..\\..\\..\\..\\Roaming\\AnimKitFive\\.ANIMKIT_ANIMATION"

					if not doesFileExist(export_file_path) then throw "AnimKit export file wasn't found, have you used LOAD IN PLUGIN button?"
					ofioLog.print ("reading AnimKit export file")

					local export_file_data = openFile export_file_path
					if eof(export_file_data) then
					(
						close export_file_data
						throw "AnimKit onim path is empty, try to use LOAD IN PLUGIN again"
					)

					local onim_file_path = readLine(export_file_data)
					if not doesFileExist(onim_file_path) then
					(
						close export_file_data
						throw "AnimKit onim path is invalid, try to use LOAD IN PLUGIN again"
					)

					local start_time = timeStamp()
					ofioLog.print "reading input file"
					onim.read(onim_file_path)
					ofioLog.print "building animation"
					onim.build obj
					ofioLog.print ("animation import succeed, elapsed " + ( ( (timeStamp() - start_time) / 1000.0 ) as String ) + " seconds")
					close export_file_data
					gc()
				) catch
				(
					ofutils.showException()
					ofioLog.print "EXPORT ERROR !"
				)

				ofioLog.finish()
				onim = undefined
			)
		)
	)

	on btn_V_ANIMKIT_QuickExport pressed do
	(
		with undo off
		(
			with redraw off
			(
				ofioLog.start()
				try (
					ofioLog.print ("searching for AnimKit export file")

					-- TODO: is there any better way to find ROAMING?
					local export_file_path = "$temp\\..\\..\\..\\..\\..\\..\\Roaming\\AnimKitFive\\.ANIMKIT_ANIMATION"
					if not doesFileExist(export_file_path) then throw "AnimKit export file wasn't found, have you used LOAD IN PLUGIN button?"

					ofioLog.print ("reading AnimKit export file")

					local export_file_data = openFile export_file_path
					if eof(export_file_data) then
					(
						close export_file_data
						throw "AnimKit onim path is empty, try to use LOAD IN PLUGIN again"
					)

					local onim_file_path = readLine(export_file_data)

					local start_time = timeStamp()
					local onim = ofio_iv_anim()
					ofioLog.print ("dumping " + Selection[1].name)
					onim.dump Selection[1]
					ofioLog.print ("writing " + onim_file_path)
					onim.write(onim_file_path)
					ofioLog.print ("Skel onim export succeed, elapsed " + ( ( (timeStamp() - start_time) / 1000.0 ) as String ) + " seconds")
					close export_file_data
					gc()
				) catch
				(
					ofutils.showException()
					ofioLog.print "EXPORT ERROR !"
				)
				ofioLog.finish()
			)
		)
	)

	label info1 "-      OFIO GTA V:      -" align:#center
	label info2 "-     https://cfx.re     -" align:#center
	label info3 "-       03 Oct 2021     -" align:#center
	label info4 "-------------------------" align:#center
	label info5 "-      OFIO GTA IV:    -" align:#center
	label info6 "-    Alexander Blade    -" align:#center
	label info7 "-         MIXAZZZ         -" align:#center
	label info8 "-    http://dev-c.com   -" align:#center
)
addRollout ofgui
