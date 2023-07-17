set path=%~dp0
set key1="HKCR\SystemFileAssociations\.png\Shell\Edit with UPaint"
set key2="HKCR\SystemFileAssociations\.jpg\Shell\Edit with UPaint"
set key3="HKCR\SystemFileAssociations\.jpeg\Shell\Edit with UPaint"
reg delete %key1% /f
reg delete %key2% /f
reg delete %key3% /f