-- This file is a part of OPEN FORMATS I/O project, (C) Alexander Blade, MIXAZZZ
-- alexander.sannybuilder.com, openiv.com

-- Port for GTAV by Cfx.re (https://cfx.re)
-- Description: log gui and functions
-- Last updated: 02 Sep 2021

global GUI_ofioLog_canClose
global GUI_ofioLog
global ofioLog

rollout GUI_ofioLog "Open Formats I/O (GTA V) Log" width:419 height:286
(
	listBox lbLog "" pos:[-1,-1] width:420 height:19 enabled:true
	progressBar pbProgess pos:[10,260] width:290 height:20
	button btnClose "CLOSE" pos:[310,258] height:23 width:100
	on GUI_ofioLog okToClose do return GUI_ofioLog_canClose
	on btnClose pressed do
	(
		destroyDialog GUI_ofioLog
		GUI_ofioLog_canClose = false
	)
)

struct ofioLog (
	fn reset = (
		GUI_ofioLog_lastUpdateVal = 0
		GUI_ofioLog_canClose = true
		try ( destroyDialog GUI_ofioLog ) catch ( )
		GUI_ofioLog_canClose = false
	),

	fn start = (
		ofioLog.reset()
		createDialog GUI_ofioLog
		GUI_ofioLog.btnClose.enabled = false
	),

	fn print text = (
		try (
			t = text as String
			format (t + "\n")
			items = GUI_ofioLog.lbLog.items
			append items t
			GUI_ofioLog.lbLog.items = items
			GUI_ofioLog.lbLog.selection = items.count
		) catch
		(
		)
	),

	fn progress value = (
		try ( GUI_ofioLog.pbProgess.value = value as Integer ) catch ( )
	),

	fn finish = (
		try (
			GUI_ofioLog.btnClose.enabled = true
			GUI_ofioLog_canClose = true
			ofioLog.progress 100
		) catch
		(
			ofioLog.reset()
		)
	)
)
