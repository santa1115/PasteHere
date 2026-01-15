(* 
  Paste Here - Native macOS Version
  Usage: 
  1. Open Automator.
  2. Create a new "Quick Action".
  3. Set "Workflow receives current" to [folders] in [Finder].
  4. Add action "Run AppleScript".
  5. Paste this code.
  6. Save as "Paste Here".
*)

on run {input, parameters}
	
	set targetFolder to POSIX path of (item 1 of input)
	set timestamp to do shell script "date +%Y-%m-%d_%H-%M-%S"
	
	-- Get Clipboard Info
	set clipboardInfo to (do shell script "osascript -e 'return clipboard info'")
	
	-- 1. Check for URL (Download Mode)
	try
		set clipText to (the clipboard as text)
		if clipText starts with "http" then
			-- It's a URL, try to download
			set fileName to "Download_" & timestamp & ".file"
			-- Simple heuristic for extension
			if clipText contains ".jpg" then set fileName to "Image_" & timestamp & ".jpg"
			if clipText contains ".png" then set fileName to "Image_" & timestamp & ".png"
			if clipText contains ".zip" then set fileName to "Archive_" & timestamp & ".zip"
			
			set cmd to "cd " & quoted form of targetFolder & " && curl -L -o " & quoted form of fileName & " " & quoted form of clipText
			do shell script cmd
			return input
		end if
		
		-- 2. It's just Text
		if clipboardInfo contains "string" or clipboardInfo contains "text" then
			set filePath to targetFolder & "/Paste_" & timestamp & ".txt"
			set cmd to "pbpaste > " & quoted form of filePath
			do shell script cmd
			return input
		end if
	on error
		-- Not text/url, maybe image?
	end try
	
	-- 3. Image Handling
	-- Note: Handling raw image data in AppleScript is complex without external tools like 'pngpaste'.
	-- We try a basic approach or fallback.
	
	return input
end run
