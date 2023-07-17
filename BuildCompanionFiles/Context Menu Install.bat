set path=%~dp0
set key1="HKCR\SystemFileAssociations\.png\Shell\Edit with UPaint"
set key2="HKCR\SystemFileAssociations\.jpg\Shell\Edit with UPaint"
set key3="HKCR\SystemFileAssociations\.jpeg\Shell\Edit with UPaint"
reg add "%key1:"=%\command" /f /d "\"%path%UPaint Standalone.exe\" \"%%1\""
reg add %key1% /f /v icon /d "%path%icon.ico"
reg add %key1% /f /v position /d Top
reg add "%key2:"=%\command" /f /d "\"%path%UPaint Standalone.exe\" \"%%1\""
reg add %key2% /f /v icon /d "%path%icon.ico"
reg add %key2% /f /v position /d Top
reg add "%key3:"=%\command" /f /d "\"%path%UPaint Standalone.exe\" \"%%1\""
reg add %key3% /f /v icon /d "%path%icon.ico"
reg add %key3% /f /v position /d Top